using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class InvitationManager : MonoBehaviour
{
    [SerializeField] private UI_NotificationInGame notificationUI;
    [SerializeField] private UI_WaitingRoom waitingRoomUI;

    private DatabaseReference dbReference;
    private string currentUserId;
    private DatabaseReference invitationsRef;

    private void Awake()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        currentUserId = Authentication.CurrentUser?.id;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("InvitationManager: Current user ID is not available");
            return;
        }

        invitationsRef = dbReference.Child("Invitations").Child(currentUserId);
        invitationsRef.ChildAdded += HandleNewInvitation;
    }

    private void OnDestroy()
    {
        if (invitationsRef != null)
        {
            invitationsRef.ChildAdded -= HandleNewInvitation;
        }
    }

    private void HandleNewInvitation(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError($"Error handling invitation: {args.DatabaseError.Message}");
            return;
        }

        if (!args.Snapshot.Exists) return;

        string invitationJson = args.Snapshot.GetRawJsonValue();
        InvitationData invitation = JsonUtility.FromJson<InvitationData>(invitationJson);

        if (invitation == null)
        {
            Debug.LogError("Failed to parse invitation data");
            return;
        }

        ShowInvitationNotification(invitation, args.Snapshot.Key);
    }

    private void ShowInvitationNotification(InvitationData invitation, string invitationId)
    {
        string title = "Invitation";
        string message = $"{invitation.inviterName} invites you to Multiplayer!";

        notificationUI.ShowNotification(
            title,
            message,
            () => AcceptInvitation(invitation, invitationId),
            () => DeclineInvitation(invitationId),
            () => DeclineInvitation(invitationId)
        );
    }

    private async void AcceptInvitation(InvitationData invitation, string invitationId)
    {
        try
        {
            await invitationsRef.Child(invitationId).RemoveValueAsync();

            if (waitingRoomUI != null)
            {
                await waitingRoomUI.ShowRoom(currentUserId, invitation.roomId);
            }
            else
            {
                Debug.LogError("UI_WaitingRoom reference is not set in InvitationManager");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accepting invitation: {e.Message}");
        }
    }

    private async void DeclineInvitation(string invitationId)
    {
        try
        {
            await invitationsRef.Child(invitationId).RemoveValueAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error declining invitation: {e.Message}");
        }
    }
}