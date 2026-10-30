using UnityEngine;
namespace MultiSet
{
    public class MeshUIController : MonoBehaviour
    {
        public GameObject meshSettings;

        private ToggleController m_meshToggle;

        public void OnMeshReady(GameObject mesh)
        {
            meshSettings.SetActive(true);
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
                // includeInactive: true — the toggle sits under the hidden meshSettings panel.
                m_meshToggle = meshSettings.GetComponentInChildren<ToggleController>(true);
                if (m_meshToggle != null)
                {
                    m_meshToggle.OnToggleChanged += OnToggleChanged;
                    m_meshToggle.SetStateImmediately(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (m_meshToggle != null)
                m_meshToggle.OnToggleChanged -= OnToggleChanged;
        }

        // The handlers own visibility so it covers every loaded mesh and survives re-localization.
        void OnToggleChanged(bool isOn)
        {
            if (MapMeshHandler.Instance != null)
                MapMeshHandler.Instance.MeshVisible = isOn;

            if (ObjectMeshHandler.Instance != null)
                ObjectMeshHandler.Instance.MeshVisible = isOn;
        }
    }
}
