using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiSet
{
    /// <summary>
    /// Inspects points on a localized map mesh: raycasts from the mouse (or screen-center / camera
    /// forward in AR) onto the mesh and reports the surface hit pose in MapSpace local coordinates.
    /// Useful for reading floor height, authoring hint poses, picking spawn points, or measuring
    /// vertical offsets on the map.
    ///
    /// Setup:
    ///   Assign <see cref="mapSpace"/> (the root transform whose local space matches the
    ///   map coordinate frame) and <see cref="mapMesh"/> (the mesh root to raycast against).
    ///   MeshColliders are added automatically to any descendant that is missing one.
    ///
    /// Cursor visualizer (auto-created at runtime):
    ///   • A flat cyan disc aligned with the surface normal.
    ///   • A green LineRenderer arrow pointing along the normal.
    ///
    /// Readout label (auto-created at runtime):
    ///   A Screen Space Overlay canvas so the readout is never occluded by 3-D geometry.
    ///   The label projects the world hit position to screen coordinates each frame and shows
    ///   the floor height plus the full MapSpace position.
    ///
    /// Play-mode shortcut:
    ///   Press <see cref="copyPoseKey"/> to copy the current pose (X, Y, Z and floor height)
    ///   to the system clipboard.
    ///
    /// Public state available to other scripts:
    ///   <see cref="LocalHitPosition"/>, <see cref="LocalHitNormal"/>, <see cref="IsHitting"/>
    ///
    /// Gizmos (Scene view, Edit and Play mode):
    ///   Cyan sphere = hit position · Green ray = surface normal
    /// </summary>

    public class MapPointInspector : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Root transform whose local space is the map coordinate frame.")]
        public Transform mapSpace;

        [Tooltip("Download MeshMesh (as child of mapSpace) and assign it here")]
        public GameObject mapMesh;

        [Header("Cursor")]
        [Tooltip("World-space radius of the cursor disc and Scene-view gizmo.")]
        [SerializeField] private float cursorScale = 0.1f;

        [Header("Height Label")]
        [Tooltip("Screen-space Y offset (pixels) so the label sits below the cursor.")]
        [SerializeField] private float labelOffsetY = -240f;

        [Tooltip("Font size (points) for the height label.")]
        [SerializeField] private float labelFontSize = 80.0f;

        [Header("Raycast")]
        [Tooltip("True: cast from mouse position. False: cast from screen-center / camera forward (AR).")]
        private bool useMouseRay = true;

        [Tooltip("Layer mask for the raycast.")]
        private LayerMask raycastMask = ~0;

        [Tooltip("Maximum raycast distance.")]
        private float maxRayDistance = 1000f;

        [Header("Copy To Clipboard")]
        [Tooltip("Key that copies the current cursor pose (X, Y, Z and floor height) to the system clipboard while in Play mode.")]
        [SerializeField] private Key copyPoseKey = Key.C;

        [Tooltip("Duration (seconds) the 'Copied!' confirmation stays on the label.")]
        private float copyFeedbackDuration = 1.2f;

        // ── Public read-only state ────────────────────────────────────────────────

        /// <summary>Last hit position expressed in MapSpace local coordinates.</summary>
        public Vector3 LocalHitPosition { get; private set; }

        /// <summary>Last hit normal expressed in MapSpace local coordinates.</summary>
        public Vector3 LocalHitNormal { get; private set; }

        /// <summary>True while the raycast is hitting the map mesh.</summary>
        public bool IsHitting { get; private set; }

        // ── Private fields ────────────────────────────────────────────────────────

        private Camera _mainCam;
        private Transform _cursorRoot;
        private LineRenderer _normalArrow;

        private GameObject _labelCanvasGO;
        private TextMeshProUGUI _labelText;
        private RectTransform _labelRT;

        private float _copyFeedbackUntil = -1f;

        // ── Editor: auto-add colliders on Inspector change ────────────────────────

#if UNITY_EDITOR
        void OnValidate()
        {
            if (mapMesh == null) return;

            int added = 0;
            foreach (MeshFilter mf in mapMesh.GetComponentsInChildren<MeshFilter>(includeInactive: true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;

                Undo.RecordObject(mf.gameObject, "Auto-add MeshCollider");
                MeshCollider col = Undo.AddComponent<MeshCollider>(mf.gameObject);
                col.sharedMesh = mf.sharedMesh;
                EditorUtility.SetDirty(mf.gameObject);
                added++;
            }

            if (added > 0)
                Debug.Log($"[MapPointInspector] Auto-added MeshCollider to {added} object(s) " +
                          $"under '{mapMesh.name}' (Editor).", this);
        }
#endif

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Awake()
        {
            _mainCam = Camera.main;
            AutoAddColliders();
            CreateCursor();
            CreateReadoutLabel();
        }


        // ── Setup helpers ─────────────────────────────────────────────────────────

        void AutoAddColliders()
        {
            if (mapMesh == null) return;

            int added = 0;
            foreach (MeshFilter mf in mapMesh.GetComponentsInChildren<MeshFilter>(includeInactive: true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;

                MeshCollider col = mf.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mf.sharedMesh;
                added++;
            }

            if (added > 0)
                Debug.Log($"[MapPointInspector] Auto-added MeshCollider to {added} object(s) " +
                          $"under '{mapMesh.name}' (Runtime).", this);
        }

        /// <summary>
        /// Builds the runtime cursor: a flat cyan disc aligned to the surface normal
        /// and a green LineRenderer arrow pointing along that normal.
        /// </summary>
        void CreateCursor()
        {
            // Flat cylinder acts as a surface-aligned disc
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "[MapPointInspector_CursorDisc]";
            disc.transform.SetParent(transform, worldPositionStays: false);
            Destroy(disc.GetComponent<Collider>());
            disc.transform.localScale = new Vector3(cursorScale, cursorScale * 0.015f, cursorScale);
            ApplyColor(disc.GetComponent<Renderer>(), new Color(0f, 0.9f, 1f, 0.85f));
            disc.SetActive(false);

            _cursorRoot = disc.transform;

            // Normal arrow
            GameObject arrowGO = new GameObject("[MapPointInspector_NormalArrow]");
            arrowGO.transform.SetParent(transform, worldPositionStays: false);

            _normalArrow = arrowGO.AddComponent<LineRenderer>();
            _normalArrow.positionCount = 2;
            _normalArrow.useWorldSpace = true;
            _normalArrow.startWidth = cursorScale * 0.12f;
            _normalArrow.endWidth = cursorScale * 0.03f;
            _normalArrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _normalArrow.receiveShadows = false;

            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                Material arrowMat = new Material(lineShader);
                if (arrowMat.HasProperty("_BaseColor")) arrowMat.SetColor("_BaseColor", Color.green);
                if (arrowMat.HasProperty("_Color")) arrowMat.SetColor("_Color", Color.green);
                _normalArrow.material = arrowMat;
            }

            _normalArrow.enabled = false;
        }

        /// <summary>
        /// Builds a Screen Space Overlay canvas with the position / height readout label.
        /// SSO renders after the full 3-D scene so the label is never occluded by geometry.
        /// </summary>
        void CreateReadoutLabel()
        {
            _labelCanvasGO = new GameObject("[MapPointInspector_LabelCanvas]");
            Canvas canvas = _labelCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

            CanvasScaler scaler = _labelCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject labelGO = new GameObject("[MapPointInspector_Label]");
            labelGO.transform.SetParent(_labelCanvasGO.transform, worldPositionStays: false);

            _labelText = labelGO.AddComponent<TextMeshProUGUI>();
            _labelText.fontSize = labelFontSize;
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.color = Color.white;
            _labelText.fontStyle = FontStyles.Bold;
            _labelText.textWrappingMode = TextWrappingModes.NoWrap;
            _labelText.overflowMode = TextOverflowModes.Overflow;
            _labelText.text = string.Empty;

            // Drop shadow via TMP underlay so white text stays readable on bright backgrounds.
            Material labelMat = new Material(_labelText.fontMaterial);
            labelMat.EnableKeyword("UNDERLAY_ON");
            labelMat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.75f));
            labelMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            labelMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            labelMat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            labelMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
            _labelText.fontMaterial = labelMat;

            _labelRT = labelGO.GetComponent<RectTransform>();
            _labelRT.sizeDelta = new Vector2(300f, 40f);
            _labelRT.pivot = new Vector2(0.5f, 0.5f);

            _labelCanvasGO.SetActive(false);
        }

        /// <summary>
        /// Applies <paramref name="color"/> to a renderer, compatible with URP and Built-in RP.
        /// Instantiates the material so the shared asset is not modified.
        /// </summary>
        static void ApplyColor(Renderer r, Color color)
        {
            Material mat = new Material(r.sharedMaterial);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            r.material = mat;
        }

        // ── Update ────────────────────────────────────────────────────────────────

        void Update()
        {
            if (_mainCam == null || mapSpace == null) return;

            Ray ray = BuildRay();
            bool didHit = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask);

            // Ignore hits outside the map mesh hierarchy
            if (didHit && mapMesh != null && !hit.collider.transform.IsChildOf(mapMesh.transform))
                didHit = false;

            IsHitting = didHit;

            if (didHit)
            {
                // 1. Convert world hit to MapSpace local coordinates
                LocalHitPosition = mapSpace.InverseTransformPoint(hit.point);
                LocalHitNormal = mapSpace.InverseTransformDirection(hit.normal).normalized;

                // 2. Position and orient the cursor disc
                _cursorRoot.gameObject.SetActive(true);
                _cursorRoot.position = hit.point;
                _cursorRoot.localScale = new Vector3(cursorScale, cursorScale * 0.015f, cursorScale);

                // Align the disc face to the surface normal; keep it tangent toward the camera
                Vector3 tangent = Vector3.Cross(hit.normal, _mainCam.transform.right);
                if (tangent.sqrMagnitude < 1e-4f)
                    tangent = Vector3.Cross(hit.normal, _mainCam.transform.up);
                _cursorRoot.rotation = Quaternion.LookRotation(tangent, hit.normal);

                // 3. Drive the normal arrow
                _normalArrow.enabled = true;
                _normalArrow.startWidth = cursorScale * 0.12f;
                _normalArrow.endWidth = cursorScale * 0.03f;
                _normalArrow.SetPosition(0, hit.point);
                _normalArrow.SetPosition(1, hit.point + hit.normal * cursorScale * 3f);

                // 4. Handle copy-to-clipboard keypress
                if (Keyboard.current != null && Keyboard.current[copyPoseKey].wasPressedThisFrame)
                {
                    CopyCurrentPoseToClipboard();
                }

                // 5. Project the hit point to screen space and update the height label
                Vector3 screenPos = _mainCam.WorldToScreenPoint(hit.point);
                if (screenPos.z > 0f)
                {
                    _labelCanvasGO.SetActive(true);
                    _labelRT.position = new Vector3(screenPos.x, screenPos.y + labelOffsetY, 0f);
                    _labelText.fontSize = labelFontSize;

                    string _height = $"Height (Y) = {LocalHitPosition.y:F2} m";
                    string labelContent = $" {_height}\n\n" +
                    $"Position -  X: {LocalHitPosition.x:F2}," +
                    $"  Y: {LocalHitPosition.y:F2}," +
                    $"  Z: {LocalHitPosition.z:F2}\n";

                    if (Time.unscaledTime < _copyFeedbackUntil)
                        labelContent += $"\n<color=#5BFF7A>Copied to clipboard ✓</color>";
                    else
                        labelContent += $"\n<size=70%>Press [{copyPoseKey}] to copy</size>";

                    _labelText.text = labelContent;
                }
                else
                {
                    _labelCanvasGO.SetActive(false);
                }
            }
            else
            {
                _cursorRoot.gameObject.SetActive(false);
                _normalArrow.enabled = false;
                _labelCanvasGO.SetActive(false);
            }
        }

        // ── Public actions ────────────────────────────────────────────────────────

        /// <summary>
        /// Copies the current MapSpace-local hit position (X, Y, Z) and floor height (Y)
        /// to the system clipboard. No-op when the cursor is not over the mesh.
        /// </summary>
        public void CopyCurrentPoseToClipboard()
        {
            if (!IsHitting) return;

            string clip =
                $"Position (MapSpace): X={LocalHitPosition.x:F4}, " +
                $"Y={LocalHitPosition.y:F4}, Z={LocalHitPosition.z:F4} | " +
                $"Floor Height (Y) = {LocalHitPosition.y:F4} m";

            GUIUtility.systemCopyBuffer = clip;
            _copyFeedbackUntil = Time.unscaledTime + copyFeedbackDuration;

            Debug.Log($"[MapPointInspector] Copied to clipboard → {clip}", this);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        Ray BuildRay()
        {
            if (useMouseRay)
            {
                Vector2 mousePos = Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return _mainCam.ScreenPointToRay(mousePos);
            }

            return new Ray(_mainCam.transform.position, _mainCam.transform.forward);
        }

        // ── Scene-view Gizmos ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!IsHitting || mapSpace == null) return;

            Vector3 worldPos = mapSpace.TransformPoint(LocalHitPosition);
            Vector3 worldNormal = mapSpace.TransformDirection(LocalHitNormal);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(worldPos, cursorScale * 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(worldPos, 3f * cursorScale * worldNormal);
        }
#endif
    }
}
