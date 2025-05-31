using System.Collections;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;

public class MultiplayerPlayer : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private bool isLocalPlayer = false;
    [SerializeField] private string playerId;
    [SerializeField] private string playerDisplayName;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float doubleJumpForce = 10f;

    [Header("Collision")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float wallCheckDistance = 0.1f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Network Settings")]
    [SerializeField] private float sendRate = 20f; // Gửi data 20 lần/giây
    [SerializeField] private float interpolationTime = 0.1f;

    // Components
    private Rigidbody2D rb;
    private Animator anim;
    private CapsuleCollider2D cd;

    // Movement variables
    private float xInput;
    private bool jumpPressed;
    private bool canDoubleJump;
    private bool isGrounded;
    private bool isWallDetected;
    private bool facingRight = true;
    private int facingDir = 1;

    // Network variables
    private DatabaseReference dbReference;
    private string currentRoomId;
    private float lastSendTime;
    private Vector3 networkPosition;
    private Vector3 networkVelocity;
    private bool networkFacingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        cd = GetComponent<CapsuleCollider2D>();

        // Null checks
        if (rb == null) Debug.LogError("Rigidbody2D not found on MultiplayerPlayer!");
        if (anim == null) Debug.LogError("Animator not found on MultiplayerPlayer!");
        if (cd == null) Debug.LogError("CapsuleCollider2D not found on MultiplayerPlayer!");

        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        if (dbReference == null) Debug.LogError("Firebase Database reference is null!");
    }

    private void Start()
    {
        currentRoomId = PlayerPrefs.GetString("CurrentRoomId", "");
        string currentUserId = PlayerPrefs.GetString("CurrentUserId", "");

        Debug.Log($"MultiplayerPlayer Start - RoomId: {currentRoomId}, UserId: {currentUserId}, PlayerId: {playerId}");

        // Xác định đây có phải local player không
        isLocalPlayer = (playerId == currentUserId);

        if (isLocalPlayer)
        {
            Debug.Log($"Local player initialized: {playerId}");
        }
        else
        {
            Debug.Log($"Remote player initialized: {playerId}");
            // Cho remote player, chúng ta sẽ listen cho position updates
            ListenForPositionUpdates();
        }

        // Set initial position
        networkPosition = transform.position;
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            HandleLocalPlayerUpdate();
        }
        else
        {
            HandleRemotePlayerUpdate();
        }

        HandleCollisions();
        HandleAnimations();
    }

    private void HandleLocalPlayerUpdate()
    {
        HandleInput();
        HandleMovement();
        HandleFlip();

        // Gửi position lên Firebase với tần suất sendRate
        if (Time.time - lastSendTime >= (1f / sendRate))
        {
            SendPositionToFirebase();
            lastSendTime = Time.time;
        }
    }

    private void HandleRemotePlayerUpdate()
    {
        // Interpolate position cho smooth movement
        if (Vector3.Distance(transform.position, networkPosition) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime / interpolationTime);
        }

        // Apply network velocity to rigidbody
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, networkVelocity, Time.deltaTime * 10f);

        // Sync facing direction
        if (facingRight != networkFacingRight)
        {
            Flip();
        }
    }

    private void HandleInput()
    {
        xInput = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
            JumpButton();
        }
    }

    private void HandleMovement()
    {
        if (isWallDetected || !isLocalPlayer)
            return;

        rb.linearVelocity = new Vector2(xInput * moveSpeed, rb.linearVelocity.y);
    }

    private void JumpButton()
    {
        if (!isLocalPlayer) return;

        if (isGrounded)
        {
            Jump();
        }
        else if (canDoubleJump)
        {
            DoubleJump();
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        AudioManager.instance?.PlaySFX(3);
    }

    private void DoubleJump()
    {
        canDoubleJump = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        AudioManager.instance?.PlaySFX(3);
    }

    private void HandleCollisions()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);

        if (isGrounded && rb.linearVelocity.y <= 0)
        {
            canDoubleJump = true;
        }
    }

    private void HandleAnimations()
    {
        anim.SetFloat("xVelocity", rb.linearVelocity.x);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallDetected", isWallDetected);
    }

    private void HandleFlip()
    {
        if (!isLocalPlayer) return;

        if (xInput < 0 && facingRight || xInput > 0 && !facingRight)
            Flip();
    }

    private void Flip()
    {
        facingDir = facingDir * (-1);
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
    }

    private void SendPositionToFirebase()
    {
        if (string.IsNullOrEmpty(currentRoomId) || !isLocalPlayer) return;

        PlayerPositionData positionData = new PlayerPositionData
        {
            x = transform.position.x,
            y = transform.position.y,
            facingRight = facingRight,
            velocityX = rb.linearVelocity.x,
            velocityY = rb.linearVelocity.y,
            isGrounded = isGrounded,
            isWallDetected = isWallDetected
        };

        string playerPositionPath = $"GameRooms/{currentRoomId}/{playerId}/position";
        string json = JsonConvert.SerializeObject(positionData);

        dbReference.Child(playerPositionPath).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to send position: {task.Exception}");
                }
            });
    }

    private void ListenForPositionUpdates()
    {
        if (string.IsNullOrEmpty(currentRoomId)) return;

        string playerPositionPath = $"GameRooms/{currentRoomId}/{playerId}/position";
        dbReference.Child(playerPositionPath).ValueChanged += OnPositionUpdated;
    }

    private void OnPositionUpdated(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError($"Position update error: {args.DatabaseError.Message}");
            return;
        }

        if (!args.Snapshot.Exists) return;

        try
        {
            string json = args.Snapshot.GetRawJsonValue();
            PlayerPositionData positionData = JsonConvert.DeserializeObject<PlayerPositionData>(json);

            if (positionData != null)
            {
                networkPosition = positionData.GetPosition();
                networkVelocity = positionData.GetVelocity();
                networkFacingRight = positionData.facingRight;

                // Update animation states
                anim.SetBool("isGrounded", positionData.isGrounded);
                anim.SetBool("isWallDetected", positionData.isWallDetected);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing position data: {e.Message}");
        }
    }

    public void InitializePlayer(string id, string displayName, bool isLocal)
    {
        playerId = id;
        playerDisplayName = displayName;
        isLocalPlayer = isLocal;

        // Update player name display if needed
        // Có thể thêm UI text để hiển thị tên player
    }

    // Thêm getter methods
    public string GetPlayerId()
    {
        return playerId;
    }

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }

    public string GetPlayerDisplayName()
    {
        return playerDisplayName;
    }

    private void OnDestroy()
    {
        // Cleanup Firebase listeners
        if (dbReference != null && !string.IsNullOrEmpty(currentRoomId))
        {
            string playerPositionPath = $"GameRooms/{currentRoomId}/{playerId}/position";
            dbReference.Child(playerPositionPath).ValueChanged -= OnPositionUpdated;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDir), transform.position.y));
    }
}