#if UNITY_EDITOR
/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using UnityEditor;
using UnityEngine;

namespace MultiSet
{
    [CustomEditor(typeof(MultiSetConfig))]
    public class MultiSetConfigEditor : Editor
    {
        private string m_verifyMessage = string.Empty;
        private MessageType m_messageType = MessageType.Info;

        public override void OnInspectorGUI()
        {
            // Draw default fields (clientId, clientSecret)
            DrawDefaultInspector();

            GUILayout.Space(10);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Credentials", EditorStyles.boldLabel);

                if (GUILayout.Button("Verify Credentials", GUILayout.Height(24)))
                {
                    var multisetSdkManager = FindFirstObjectByType<MultisetSdkManager>();

                    var config = (MultiSetConfig)target;
                    m_verifyMessage = "Verifying...";
                    m_messageType = MessageType.Info;

                    if (multisetSdkManager != null)
                    {
                        multisetSdkManager.clientId = config.clientId;
                        multisetSdkManager.clientSecret = config.clientSecret;

                        if (!string.IsNullOrWhiteSpace(multisetSdkManager.clientId) && !string.IsNullOrWhiteSpace(multisetSdkManager.clientSecret))
                        {
                            EventManager<EventData>.StartListening("AuthCallBack", OnAuthCallBack);
                            multisetSdkManager.AuthenticateMultiSetSDK();
                        }
                        else
                        {
                            m_verifyMessage = "Please enter valid credentials in MultiSetConfig!";
                            m_messageType = MessageType.Error;
                            Repaint();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(m_verifyMessage))
                {
                    EditorGUILayout.HelpBox(m_verifyMessage, m_messageType);
                }
            }
        }

        private void OnDestroy()
        {
            EventManager<EventData>.StopListening("AuthCallBack", OnAuthCallBack);
        }

        private void OnAuthCallBack(EventData eventData)
        {
            if (eventData.AuthSuccess)
            {
                m_verifyMessage = "Entered credentials are correct";
                m_messageType = MessageType.Info;
            }
            else
            {
                m_verifyMessage = "Entered credentials are incorrect!";
                m_messageType = MessageType.Error;
            }

            Repaint();
            EventManager<EventData>.StopListening("AuthCallBack", OnAuthCallBack);
        }
    }
}
#endif


