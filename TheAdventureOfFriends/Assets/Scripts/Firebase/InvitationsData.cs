using System;

[Serializable]
public class InvitationData
{
    public string roomId;
    public string inviterId;
    public string invitedId;
    public string inviterName;
    public object timestamp;
    // Có thể thêm thời gian hết hạn lời mời nếu cần
}