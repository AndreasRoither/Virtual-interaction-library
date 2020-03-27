using UnityEditor;
using UnityEngine;
using VRIL.Interactable;

namespace VRIL.CustomInspector
{
    [CustomEditor(typeof(VRIL_Interactable))]
    [CanEditMultipleObjects]
    public class InteractableObject_Inspector : Editor
    {
        private SerializedProperty OneAudioSource;

        /// <summary>
        /// Called when InspectorGUI is drawn or interacted with
        /// </summary>
        public override void OnInspectorGUI()
        {
            // would show all the public variables from interact
            //base.OnInspectorGUI();

            int baseIndentLevel = EditorGUI.indentLevel;
            OneAudioSource = serializedObject.FindProperty("General_OneAudioSource");

            ShowGeneralArea();
            EditorGUILayout.Space();
            ShowSelectionOptions();
            EditorGUILayout.Space();
            ShowInteractionOptions();
            EditorGUILayout.Space();
            ShowReleaseOptions();

            EditorGUI.indentLevel = baseIndentLevel;

            // MOST Important line!!
            // Unity won't serialize/deserialize upon play/stop and all values changed will be los
            // old way: EditorUtility.SetDirty(target);
            // https://docs.unity3d.com/Manual/editor-CustomEditors.html
            // https://docs.unity3d.com/ScriptReference/SerializedObject.html
            serializedObject.ApplyModifiedProperties();
        }

        public void ShowGeneralArea()
        {
            SerializedProperty General_AudioSource = serializedObject.FindProperty("General_AudioSource");

            EditorGUILayout.LabelField("General Interactable Object Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                if (OneAudioSource.boolValue =
                    EditorGUILayout.Toggle("Use only one Audio Source", OneAudioSource.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(General_AudioSource, new GUIContent("Audio Source"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Shows all properties in the slection category
        /// </summary>
        /// <param name="interact">The target</param>
        public void ShowSelectionOptions()
        {
            SerializedProperty Selection_Selectable = serializedObject.FindProperty("Selection_Selectable");
            SerializedProperty selection_controller_swappable =
                serializedObject.FindProperty("Selection_ControllerSwappable");
            SerializedProperty selection_script = serializedObject.FindProperty("Selection_Script");
            SerializedProperty Selection_FeedbackScript = serializedObject.FindProperty("Selection_FeedbackScript");
            SerializedProperty Selection_Feedback = serializedObject.FindProperty("Selection_Feedback");
            SerializedProperty Selection_AudioClip = serializedObject.FindProperty("Selection_AudioClip");
            SerializedProperty Selection_AudioSource = serializedObject.FindProperty("Selection_AudioSource");
            SerializedProperty Selection_MoveToAttachmentType =
                serializedObject.FindProperty("Selection_MoveToAttachmentType");
            SerializedProperty Selection_MoveToAttachmentTypeSpeed =
                serializedObject.FindProperty("Selection_MoveToAttachmentTypeSpeed");
            SerializedProperty Selection_ObjectAttachment = serializedObject.FindProperty("Selection_ObjectAttachment");
            SerializedProperty Selection_OffSet = serializedObject.FindProperty("Selection_OffSet");


            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(Selection_Selectable, new GUIContent("Selectable"));
                EditorGUILayout.PropertyField(selection_controller_swappable,
                    new GUIContent("Controller Swap enabled"));
                EditorGUILayout.PropertyField(selection_script, new GUIContent("Selection Script"));
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Feedback", EditorStyles.boldLabel);
                if (Selection_Feedback.boolValue =
                    EditorGUILayout.Toggle("Use Selection Feedback", Selection_Feedback.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(Selection_AudioClip, new GUIContent("Audio Clip"));
                    if (!OneAudioSource.boolValue)
                        EditorGUILayout.PropertyField(Selection_AudioSource, new GUIContent("Audio Source"));
                    EditorGUILayout.PropertyField(Selection_FeedbackScript, new GUIContent("Feedback Script"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Attachment", EditorStyles.boldLabel);
                if (Selection_MoveToAttachmentType.boolValue = EditorGUILayout.Toggle("Object Attachment Type",
                    Selection_MoveToAttachmentType.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(Selection_ObjectAttachment, new GUIContent("Attachment Type"));
                    EditorGUILayout.PropertyField(Selection_MoveToAttachmentTypeSpeed, new GUIContent("Movementspeed"));
                    EditorGUILayout.PropertyField(Selection_OffSet, new GUIContent("Attachment Offset"));

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Shows all properties in the interaction category
        /// </summary>
        /// <param name="interact">The target</param>
        public void ShowInteractionOptions()
        {
            SerializedProperty Interaction_HoldButtonToInteract =
                serializedObject.FindProperty("Interaction_HoldButtonToInteract");
            SerializedProperty Interaction_Script = serializedObject.FindProperty("Interaction_Script");
            SerializedProperty Interaction_FeedbackScript = serializedObject.FindProperty("Interaction_FeedbackScript");
            SerializedProperty Interaction_Manipulatable = serializedObject.FindProperty("Interaction_Manipulatable");
            SerializedProperty Interaction_Manipulation_PositionChangeAble =
                serializedObject.FindProperty("Interaction_Manipulation_PositionChangeAble");
            SerializedProperty Interaction_Manipulation_RotationChangeAble =
                serializedObject.FindProperty("Interaction_Manipulation_RotationChangeAble");
            SerializedProperty Interaction_Feedback = serializedObject.FindProperty("Interaction_Feedback");
            SerializedProperty Interaction_AudioClip = serializedObject.FindProperty("Interaction_AudioClip");
            SerializedProperty Interaction_AudioSource = serializedObject.FindProperty("Interaction_AudioSource");

            EditorGUILayout.LabelField("Manipulation", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(Interaction_HoldButtonToInteract,
                    new GUIContent("Hold Button to Interact"));
                EditorGUILayout.PropertyField(Interaction_Script, new GUIContent("Interaction Script"));
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
                if (Interaction_Manipulatable.boolValue =
                    EditorGUILayout.Toggle("Interactable", Interaction_Manipulatable.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(Interaction_Manipulation_PositionChangeAble,
                        new GUIContent("Position changeable"));
                    EditorGUILayout.PropertyField(Interaction_Manipulation_RotationChangeAble,
                        new GUIContent("Rotation changeable"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Feedback", EditorStyles.boldLabel);
                if (Interaction_Feedback.boolValue =
                    EditorGUILayout.Toggle("Use Interaction Feedback", Interaction_Feedback.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(Interaction_AudioClip, new GUIContent("Audio Clip"));
                    if (!OneAudioSource.boolValue)
                        EditorGUILayout.PropertyField(Interaction_AudioSource, new GUIContent("Audio Source"));
                    EditorGUILayout.PropertyField(Interaction_FeedbackScript, new GUIContent("Feedback Script"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Shows all properties in the relase category
        /// </summary>
        /// <param name="interact">The target</param>
        public void ShowReleaseOptions()
        {
            SerializedProperty Release_Script = serializedObject.FindProperty("Release_Script");
            SerializedProperty Release_FeedbackScript = serializedObject.FindProperty("Release_FeedbackScript");
            SerializedProperty Release_Feedback = serializedObject.FindProperty("Release_Feedback");
            SerializedProperty Release_AudioClip = serializedObject.FindProperty("Release_AudioClip");
            SerializedProperty Release_AudioSource = serializedObject.FindProperty("Release_AudioSource");
            SerializedProperty Release_Releaseable = serializedObject.FindProperty("Release_Releaseable");
            SerializedProperty Release_MoveToReleaseLocationSpeed =
                serializedObject.FindProperty("Release_MoveToReleaseLocationSpeed");
            SerializedProperty Release_ObjectFinalLocation =
                serializedObject.FindProperty("Release_ObjectFinalLocation");

            EditorGUILayout.LabelField("Release", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(Release_Script, new GUIContent("Release Script"));
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Feedback", EditorStyles.boldLabel);
                if (Release_Feedback.boolValue =
                    EditorGUILayout.Toggle("Use Release Feedback", Release_Feedback.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(Release_AudioClip, new GUIContent("Audio Clip"));
                    if (!OneAudioSource.boolValue)
                        EditorGUILayout.PropertyField(Release_AudioSource, new GUIContent("Audio Source"));
                    EditorGUILayout.PropertyField(Release_FeedbackScript, new GUIContent("Feedback Script"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Attachment", EditorStyles.boldLabel);
                if (Release_Releaseable.boolValue = EditorGUILayout.Toggle("Releasable", Release_Releaseable.boolValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(Release_MoveToReleaseLocationSpeed, new GUIContent("Movementspeed"));
                    EditorGUILayout.PropertyField(Release_ObjectFinalLocation, new GUIContent("Release Type"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
        }
    }
}