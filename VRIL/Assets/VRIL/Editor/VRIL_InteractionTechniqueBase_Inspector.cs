using UnityEditor;
using VRIL.InteractionTechniques;
using VRIL.Manager;

namespace VRIL.CustomInspector
{
    [CustomEditor(typeof(VRIL_InteractionTechniqueBase))]
    public class VRIL_InteractionTechniqueBase_Inspector : Editor
    {
        private VRIL_Manager t;
        private SerializedObject GetTarget;
        private SerializedProperty RegisteredControllers;
        private int ListSize;

        private void OnEnable()
        {
            t = (VRIL_Manager)target;
            GetTarget = new SerializedObject(t);

            // Find the List in our script and create a refrence of it
            RegisteredControllers = GetTarget.FindProperty("RegisteredControllers");
        }

        public override void OnInspectorGUI()
        {
            // base is standard on gui
            base.OnInspectorGUI();

            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            //RegisteredControllers = GetTarget.FindProperty("RegisteredControllers");

            //GeneralUI();
            //ControllerAndTechniqueUI();

            // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
            serializedObject.ApplyModifiedProperties();
        }

        public void GeneralUI()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            }
        }
    }
}