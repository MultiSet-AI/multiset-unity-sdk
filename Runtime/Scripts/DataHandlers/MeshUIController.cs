using UnityEngine;
namespace MultiSet
{
    public class MeshUIController : MonoBehaviour
    {
        private GameObject currentMesh;

        public GameObject meshSettings;

        private ToggleController m_meshToggle;

        public void OnMeshReady(GameObject mesh)
        {
            currentMesh = mesh;
            meshSettings.SetActive(true);

            // Sync the freshly-loaded mesh's visibility to the current toggle state,
            // otherwise it keeps whatever active state the SDK set (localization-based).
            if (m_meshToggle != null)
                currentMesh.SetActive(m_meshToggle.isOn);

            Debug.Log($"Mesh ready: {mesh.name}");
        }

        public void Start()
        {
            meshSettings.SetActive(false);

            MeshToggleInit();

        }


        private void MeshToggleInit()
        {
            if (meshSettings != null)
            {
                // includeInactive: true — meshSettings may be under the (now hidden) meshSettings panel.
                m_meshToggle = meshSettings.GetComponentInChildren<ToggleController>(true);
                if (m_meshToggle != null)
                {
                    m_meshToggle.OnToggleChanged += OnToggleChanged;
                    // Default the toggle ON so the mesh is shown as soon as it is ready
                    m_meshToggle.SetStateImmediately(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (m_meshToggle != null)
                m_meshToggle.OnToggleChanged -= OnToggleChanged;
        }

        void OnToggleChanged(bool isOn)
        {
            if (currentMesh != null)
                currentMesh.SetActive(isOn);
        }
    }
}