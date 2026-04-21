using System;

[Serializable]
public class NetworkMessage
{
    public int type;        // 1 = PoseUpdate, 2 = PlayerInfo
    public string senderID;
    public string payload;  // Base64-encoded inner JSON
}

[Serializable]
public class PoseUpdate
{
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float rotationW;
    public bool isLocalized;
}

[Serializable]
public class PlayerInfo
{
    public string playerName;
    public float colorR;
    public float colorG;
    public float colorB;
}
