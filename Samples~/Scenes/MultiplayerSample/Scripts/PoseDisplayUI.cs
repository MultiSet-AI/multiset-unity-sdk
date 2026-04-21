using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PoseDisplayUI : MonoBehaviour
{
    [Tooltip("TextMeshProUGUI element to display pose info")]
    public TextMeshProUGUI poseText;

    [Tooltip("Optional: MultiplayerManager to show MPC remote players")]
    public MultiplayerManager multiplayerManager;

    [Tooltip("How often to refresh the display (seconds)")]
    public float refreshInterval = 0.1f;

    private float lastRefreshTime;

    void Update()
    {
        if (poseText == null) return;
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            poseText.text = "";
            return;
        }

        if (Time.time - lastRefreshTime < refreshInterval) return;
        lastRefreshTime = Time.time;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>== Player Poses ==</b>");
        sb.AppendLine();

        var players = FindObjectsByType<NetworkARPlayer>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            sb.AppendLine("Waiting for players...");
            poseText.text = sb.ToString();
            return;
        }

        foreach (var player in players)
        {
            if (!player.IsSpawned) continue;

            string displayName = !string.IsNullOrEmpty(player.PlayerName) ? player.PlayerName : $"Player {player.PlayerIndex}";
            string label = player.IsOwner ? $"{displayName} (You)" : $"{displayName} (Remote)";
            string color = player.IsOwner ? "#00FF88" : "#FF8800";

            sb.AppendLine($"<color={color}><b>{label}</b></color>");

            if (!player.IsLocalized)
            {
                sb.AppendLine("  Waiting for localization...");
                sb.AppendLine();
                continue;
            }

            Vector3 pos = player.Position;
            Vector3 rot = player.Rotation.eulerAngles;

            sb.AppendLine($"  Pos:  X={pos.x:F2}  Y={pos.y:F2}  Z={pos.z:F2}");
            sb.AppendLine($"  Rot:  X={rot.x:F1}  Y={rot.y:F1}  Z={rot.z:F1}");
            sb.AppendLine();
        }

        // MPC remote players
        if (multiplayerManager != null && multiplayerManager.IsLocalized)
        {
            foreach (var kvp in multiplayerManager.RemotePlayers)
            {
                var mpcPlayer = kvp.Value;
                string mpcColor = "#FF00FF"; // Magenta for MPC players
                string mpcLabel = $"{mpcPlayer.playerName} (MPC)";

                sb.AppendLine($"<color={mpcColor}><b>{mpcLabel}</b></color>");

                if (!mpcPlayer.isLocalized)
                {
                    sb.AppendLine("  Waiting for localization...");
                    sb.AppendLine();
                    continue;
                }

                Vector3 mpcPos = mpcPlayer.targetPosition;
                Vector3 mpcRot = mpcPlayer.targetRotation.eulerAngles;

                sb.AppendLine($"  Pos:  X={mpcPos.x:F2}  Y={mpcPos.y:F2}  Z={mpcPos.z:F2}");
                sb.AppendLine($"  Rot:  X={mpcRot.x:F1}  Y={mpcRot.y:F1}  Z={mpcRot.z:F1}");
                sb.AppendLine();
            }
        }

        poseText.text = sb.ToString();
    }
}
