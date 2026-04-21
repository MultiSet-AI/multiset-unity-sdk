using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the visual elements of a player's AR card.
/// Attached to a world-space Canvas that is a child of the NetworkARPlayer GameObject.
/// </summary>
public class PlayerCardUI : MonoBehaviour
{
    private TextMeshProUGUI nameText;
    private Image backgroundImage;
    private Image avatarImage;

    private Canvas canvas;

    /// <summary>
    /// Builds the entire card UI hierarchy programmatically under this GameObject.
    /// Call once after instantiation.
    /// </summary>
    public void Build()
    {
        // -- Canvas setup --
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        var canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 100;

        gameObject.AddComponent<GraphicRaycaster>();

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 280);
        rt.localScale = Vector3.one * 0.001f;
        rt.localPosition = new Vector3(0f, 0.15f, 0f);

        // -- Background panel --
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(rt, false);
        backgroundImage = bgGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // -- Top color accent bar --
        var accentGO = new GameObject("AccentBar");
        accentGO.transform.SetParent(bgRT, false);
        var accentImage = accentGO.AddComponent<Image>();
        accentImage.color = Color.cyan;

        var accentRT = accentGO.GetComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0f, 1f);
        accentRT.anchorMax = new Vector2(1f, 1f);
        accentRT.pivot = new Vector2(0.5f, 1f);
        accentRT.sizeDelta = new Vector2(0f, 8f);
        accentRT.anchoredPosition = Vector2.zero;

        // -- Avatar area (centered in upper portion) --
        var avatarGO = new GameObject("Avatar");
        avatarGO.transform.SetParent(bgRT, false);
        avatarImage = avatarGO.AddComponent<Image>();
        avatarImage.color = Color.white;
        avatarImage.preserveAspect = true;

        var avatarRT = avatarGO.GetComponent<RectTransform>();
        avatarRT.anchorMin = new Vector2(0.5f, 1f);
        avatarRT.anchorMax = new Vector2(0.5f, 1f);
        avatarRT.pivot = new Vector2(0.5f, 1f);
        avatarRT.sizeDelta = new Vector2(80f, 80f);
        avatarRT.anchoredPosition = new Vector2(0f, -20f);

        // Generate a simple person silhouette icon
        avatarImage.sprite = CreateAvatarSprite();

        // -- Name text (centered below avatar) --
        var nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(bgRT, false);
        nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = "Player";
        nameText.fontSize = 32;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableAutoSizing = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0f);
        nameRT.anchorMax = new Vector2(1f, 0.55f);
        nameRT.offsetMin = new Vector2(10f, 10f);
        nameRT.offsetMax = new Vector2(-10f, -5f);
    }

    public void Initialize(string playerName, Color cardColor)
    {
        if (nameText != null)
            nameText.text = playerName;

        if (backgroundImage != null)
            backgroundImage.color = new Color(cardColor.r * 0.3f, cardColor.g * 0.3f, cardColor.b * 0.3f, 0.85f);

        // Set accent bar to the full card color
        var accent = transform.Find("Background/AccentBar");
        if (accent != null)
        {
            var img = accent.GetComponent<Image>();
            if (img != null)
                img.color = cardColor;
        }

        // Tint avatar with card color
        if (avatarImage != null)
            avatarImage.color = cardColor;
    }

    public void Billboard(Camera cam)
    {
        if (cam == null) return;

        // Face the camera — LookAt points Z toward camera, then flip so front faces camera
        Vector3 dirToCamera = cam.transform.position - transform.position;
        if (dirToCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-dirToCamera, Vector3.up);
    }

    /// <summary>
    /// Creates a simple person silhouette sprite procedurally.
    /// A circle for head + rounded rectangle for body.
    /// </summary>
    private static Sprite CreateAvatarSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        // Clear to transparent
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        Vector2 center = new Vector2(size / 2f, size / 2f);

        // Head (circle at upper area)
        Vector2 headCenter = new Vector2(size / 2f, size * 0.68f);
        float headRadius = size * 0.16f;

        // Body (ellipse at lower area)
        Vector2 bodyCenter = new Vector2(size / 2f, size * 0.32f);
        float bodyRadiusX = size * 0.28f;
        float bodyRadiusY = size * 0.28f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx, dy;

                // Head check
                dx = x - headCenter.x;
                dy = y - headCenter.y;
                bool inHead = (dx * dx + dy * dy) <= (headRadius * headRadius);

                // Body check (ellipse)
                dx = x - bodyCenter.x;
                dy = y - bodyCenter.y;
                bool inBody = ((dx * dx) / (bodyRadiusX * bodyRadiusX) + (dy * dy) / (bodyRadiusY * bodyRadiusY)) <= 1f;

                // Only upper half of body ellipse (shoulders + torso)
                bool inBodyUpper = inBody && y <= bodyCenter.y + bodyRadiusY * 0.95f;

                if (inHead || inBodyUpper)
                    pixels[y * size + x] = Color.white;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
