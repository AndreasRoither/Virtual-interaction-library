using UnityEditor;
using UnityEngine;
using VRIL.Manager;

namespace VRIL.CustomInspector
{
    [CustomEditor(typeof(VRIL_Manager))]
    public class Manager_Inspector : Editor
    {
        public override void OnInspectorGUI()
        {
            int baseIndentLevel = EditorGUI.indentLevel;

            ShowControllerAndTechniquesSection();
            EditorGUILayout.Space();
            ShowSDKSection();

            EditorGUI.indentLevel = baseIndentLevel;
            serializedObject.ApplyModifiedProperties();
        }

        public void ShowControllerAndTechniquesSection()
        {
            SerializedProperty RegisteredControllers = serializedObject.FindProperty("RegisteredControllers");

            EditorGUILayout.LabelField("Registered Controller and Techniques", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(RegisteredControllers, new GUIContent("Registered Controller"), true);
                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }
        }

        public void ShowSDKSection()
        {
            SerializedProperty LoadedSdkName = serializedObject.FindProperty("LoadedSdkName");
            SerializedProperty OpenVR_Script = serializedObject.FindProperty("OpenVR_Script");
            SerializedProperty Oculus_Script = serializedObject.FindProperty("Oculus_Script");
            SerializedProperty CustomSDK_Script = serializedObject.FindProperty("CustomSDK_Script");

            EditorGUILayout.LabelField("SDK", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.PropertyField(LoadedSdkName, new GUIContent("Loaded SDK"));
                EditorGUILayout.PropertyField(OpenVR_Script, new GUIContent("OpenVR Script"));
                EditorGUILayout.PropertyField(Oculus_Script, new GUIContent("Oculus Script"));
                EditorGUILayout.PropertyField(CustomSDK_Script, new GUIContent("Custom SDK Script"));
                EditorGUILayout.Space();
            }
        }
    }
}