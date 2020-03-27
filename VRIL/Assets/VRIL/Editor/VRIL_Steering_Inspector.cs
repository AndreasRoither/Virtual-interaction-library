using UnityEditor;
using UnityEngine;
using VRIL.Manager;
using VRIL.NavigationTechniques;

namespace VRIL.CustomInspector
{
    [CustomEditor(typeof(VRIL_Steering))]
    public class SteeringEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty Technique = serializedObject.FindProperty("Technique");
            DrawDefaultInspector();
            if ((VRIL_Steering.SteeringTechnique)Technique.intValue == VRIL_Steering.SteeringTechnique.HandDirected)
            {
                VRIL_Steering script = (VRIL_Steering)target;
                script.Mode = (VRIL_Steering.SteeringMode)EditorGUILayout.EnumPopup("Mode", script.Mode);
            }
        }
    }
}
