#if UNITY_EDITOR
/*
Copyright (c) 2026 MultiSet AI. All rights reserved.
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

        private bool m_isEditingBaseUrl = false;
        private string m_editedBaseUrl = string.Empty;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty baseUrlProp = serializedObject.FindProperty("baseUrl");
            SerializedProperty clientIdProp = serializedObject.FindProperty("clientId");
            SerializedProperty clientSecretProp = serializedObject.FindProperty("clientSecret");

            // === MultiSet API Configuration ===
            EditorGUILayout.LabelField("MultiSet API Configuration", EditorStyles.boldLabel);

            GUIContent baseUrlLabel = new GUIContent(
                "Base URL",
                "Base URL for MultiSet API calls. Leave as the default unless instructed otherwise.");

            if (!m_isEditingBaseUrl)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(baseUrlLabel, baseUrlProp.stringValue);
                }

                if (GUILayout.Button("Update Base URL", GUILayout.Height(22)))
                {
                    m_editedBaseUrl = baseUrlProp.stringValue;
                    m_isEditingBaseUrl = true;
                    GUI.FocusControl(null);
                }
            }
            else
            {
                m_editedBaseUrl = EditorGUILayout.TextField(baseUrlLabel, m_editedBaseUrl);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Continue", GUILayout.Height(22)))
                {
                    string newValue = (m_editedBaseUrl ?? string.Empty).Trim();

                    if (string.IsNullOrEmpty(newValue))
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Base URL",
                            "Base URL cannot be empty.",
                            "OK");
                    }
                    else
                    {
                        bool confirm = EditorUtility.DisplayDialog(
                            "Update Base URL?",
                            "Please make sure the base URL is correct. If the URL is invalid, the MultiSet SDK will not work and all functionality may break.\n\nNew Base URL:\n" + newValue,
                            "Confirm",
                            "Cancel");

                        if (confirm)
                        {
                            baseUrlProp.stringValue = newValue;
                            m_editedBaseUrl = newValue;
                            m_isEditingBaseUrl = false;
                            GUI.FocusControl(null);
                        }
                    }
                }

                if (GUILayout.Button("Cancel", GUILayout.Height(22)))
                {
                    m_editedBaseUrl = baseUrlProp.stringValue;
                    m_isEditingBaseUrl = false;
                    GUI.FocusControl(null);
                }

                EditorGUILayout.EndHorizontal();
            }

            // === MultiSet SDK Credentials ===
            // [Space] and [Header] attributes on clientId in MultiSetConfig.cs draw the section header automatically.
            EditorGUILayout.PropertyField(clientIdProp);
            EditorGUILayout.PropertyField(clientSecretProp);

            serializedObject.ApplyModifiedProperties();

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
                    else
                    {
                        m_verifyMessage = "MultisetSdkManager not found in the scene. Please add it to the scene before verifying credentials.";
                        m_messageType = MessageType.Error;
                        Repaint();
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
