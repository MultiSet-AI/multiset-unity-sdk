using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace MultiSet
{
    public class ToggleController : MonoBehaviour
    {
        public bool isOn;

        public Color onColorBg;
        public Color offColorBg;

        public Image toggleBgImage;
        public RectTransform toggle;

        public GameObject handle;
        public RectTransform handleTransform;

        private float handleSize;
        private float onPosX;
        private float offPosX;

        public float handleOffset;

        public GameObject onIcon;
        public GameObject offIcon;

        public float speed;
        private static float t = 0.0f;

        private bool switching = false;

        // Declare the Action callback event
        public Action<bool> OnToggleChanged;


        private void Awake()
        {
            if (handleTransform == null)
            {
                handleTransform = handle.GetComponent<RectTransform>();
            }
            handleSize = handleTransform.sizeDelta.x;

            float toggleSizeX = toggle.sizeDelta.x;
            onPosX = (toggleSizeX / 2) - (handleSize / 2) - handleOffset;
            offPosX = onPosX * -1;
        }

        private void Start()
        {
            UpdateToggleState();
        }

        public void UpdateToggleState()
        {
            if (isOn)
            {
                toggleBgImage.color = onColorBg;
                handleTransform.localPosition = new Vector3(onPosX, 0f, 0f);
                onIcon.SetActive(true);
                offIcon.SetActive(false);
            }
            else
            {
                toggleBgImage.color = offColorBg;
                handleTransform.localPosition = new Vector3(offPosX, 0f, 0f);
                onIcon.SetActive(false);
                offIcon.SetActive(true);
            }

            OnToggleChanged?.Invoke(isOn);
        }

        public void SetState(bool state)
        {
            switching = true;
            isOn = !state;
        }

        public void SetStateImmediately(bool state)
        {
            try
            {
                isOn = state;
                UpdateToggleState();
            }
            catch (Exception ex)
            {
                Debug.LogError($"ToggleController UI Not reset {ex}");
            }
        }

        private void Update()
        {
            if (switching)
            {
                Toggle(isOn);
            }
        }

        public void Switching()
        {
            switching = true;
        }

        public void Toggle(bool toggleStatus)
        {
            if (!onIcon.activeSelf || !offIcon.activeSelf)
            {
                onIcon.SetActive(true);
                offIcon.SetActive(true);
            }

            if (toggleStatus)
            {
                toggleBgImage.color = SmoothColor(onColorBg, offColorBg);
                onIcon.GetComponent<CanvasGroup>().alpha = 0f;
                offIcon.GetComponent<CanvasGroup>().alpha = 1f;
                handleTransform.localPosition = SmoothMove(handle, onPosX, offPosX);
            }
            else
            {
                toggleBgImage.color = SmoothColor(offColorBg, onColorBg);
                onIcon.GetComponent<CanvasGroup>().alpha = 1f;
                offIcon.GetComponent<CanvasGroup>().alpha = 0f;
                handleTransform.localPosition = SmoothMove(handle, offPosX, onPosX);
            }

        }

        private Vector3 SmoothMove(GameObject toggleHandle, float startPosX, float endPosX)
        {
            Vector3 position = new(Mathf.Lerp(startPosX, endPosX, t += speed * Time.deltaTime), 0f, 0f);
            StopSwitching();
            return position;
        }

        private Color SmoothColor(Color startCol, Color endCol)
        {
            Color resultCol;
            resultCol = Color.Lerp(startCol, endCol, t += speed * Time.deltaTime);
            return resultCol;
        }

        private void StopSwitching()
        {
            if (t > 1.0f)
            {
                switching = false;

                t = 0.0f;

                switch (isOn)
                {
                    case true:
                        isOn = false;
                        OnToggleChanged?.Invoke(isOn);
                        break;
                    case false:
                        isOn = true;
                        OnToggleChanged?.Invoke(isOn);
                        break;
                }
            }
        }
    }
}