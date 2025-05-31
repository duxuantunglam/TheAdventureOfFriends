using System;
using UnityEngine;

[Serializable]
public class PlayerPositionData
{
    public float x;
    public float y;
    public bool facingRight;
    public float velocityX;
    public float velocityY;
    public bool isGrounded;
    public bool isWallDetected;
    public long timestamp;

    public PlayerPositionData()
    {
        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public Vector2 GetPosition()
    {
        return new Vector2(x, y);
    }

    public Vector2 GetVelocity()
    {
        return new Vector2(velocityX, velocityY);
    }
}

[Serializable]
public class PlayerInputData
{
    public float xInput;
    public bool jumpPressed;
    public long timestamp;

    public PlayerInputData()
    {
        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}

[Serializable]
public class PlayerAnimationData
{
    public float xVelocity;
    public float yVelocity;
    public bool isGrounded;
    public bool isWallDetected;
    public bool isKnocked;
    public long timestamp;

    public PlayerAnimationData()
    {
        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}

[Serializable]
public class MultiplayerRoomGameData
{
    public string player1Id;
    public string player2Id;
    public PlayerPositionData player1Position;
    public PlayerPositionData player2Position;
    public PlayerInputData player1Input;
    public PlayerInputData player2Input;
    public PlayerAnimationData player1Animation;
    public PlayerAnimationData player2Animation;
    public string gameStatus;
    public long lastUpdateTime;

    public MultiplayerRoomGameData()
    {
        lastUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        gameStatus = "playing";
    }
}