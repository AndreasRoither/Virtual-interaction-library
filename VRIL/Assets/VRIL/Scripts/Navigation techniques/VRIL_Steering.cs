using System.Collections;
using UnityEditor;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;

namespace VRIL.NavigationTechniques
{
    /// <summary>
    /// Implementation of steering technique
    /// </summary>
    public class VRIL_Steering : VRIL_NavigationTechniqueBase
    {


        // *************************************
        // public properties
        // *************************************

        [Header("Steering Settings")]
        [Tooltip("Enable axes for navigation (for flying mode enable all axes for navigation)")]
        //public bool Flying = false;
        public bool EnableNavigationX = true;
        public bool EnableNavigationY = false;
        public bool EnableNavigationZ = true;

        [Tooltip("Viewpoint velocity")]
        public float Velocity = 2.0f;
        [Tooltip("Select steering type")]
        public SteeringTechnique Technique;
        [HideInInspector]
        [Tooltip("Select steering mode")]
        public SteeringMode Mode;


        // *************************************
        // private and protected members
        // *************************************

        private GameObject SteeringObject;
        protected bool IsActivated = false;
        private GameObject Camera;

        public void Awake()
        {
            Initialize();
            if(Mode == SteeringMode.CrosshairsMode || Technique == SteeringTechnique.GazeDirected)
            {
                if(Viewpoint.GetComponent<Camera>())
                {
                    Camera = Viewpoint;
                }
                else if(Viewpoint.GetComponentInChildren<Camera>())
                {
                    Camera cam = Viewpoint.GetComponentInChildren<Camera>();
                    Camera = cam.gameObject;
                }
                else
                {
                    Debug.LogWarning("Could not set camera object. No camera found in viewpoint object! Use hand-directed steering or check camera first.");
                }
            }
            SteeringObject = Camera != null ? Camera : RegisteredControllers[0];
        }

        /// <summary>
        /// When the technique is activated
        /// </summary>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (RegisteredControllers.Count > 0)
            {
                // differentiate between ButtonStates
                if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Released)
                {
                    IsActivated = false;
                }
                else if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed)
                {
                    if (!IsActivated)
                    {
                        OnTravel(e);
                    }
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_Steering)}:</b>\n No controller registered");
            }
        }

        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            PlayAudio();
            IsActivated = true;
            StartCoroutine(Steering(e));
        }

        protected IEnumerator Steering(VRIL_ControllerActionEventArgs e)
        {
            while (IsActivated)
            {
                Vector3 direction = (Mode == SteeringMode.CrosshairsMode ? (RegisteredControllers[0].transform.position - Camera.transform.position) : SteeringObject.transform.forward).normalized;
                

                Vector3 newPosition = Vector3.zero;
                if(EnableNavigationX)
                {
                    newPosition.x = direction.x;
                }
                if (EnableNavigationY)
                {
                    newPosition.y = direction.y;
                }
                if (EnableNavigationZ)
                {
                    newPosition.z = direction.z;
                }
                Vector3 CalculatedPosition = Viewpoint.transform.position + (newPosition * Velocity * Time.deltaTime);
                TargetPosition = CalculatedPosition;
                SaveDistancesToViewpoint();
                Viewpoint.transform.position = TargetPosition;
                TransferSelectedObjects();
                yield return null;
            }
            StopAudio();
        }

        public enum SteeringTechnique
        {
            HandDirected,
            GazeDirected,
        }

        public enum SteeringMode
        {
            PointingMode,
            CrosshairsMode
        }
    }

    [CustomEditor(typeof(VRIL_Steering))]
    public class SteeringEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var steering = target as VRIL_Steering;

            DrawDefaultInspector();
            if (steering.Technique == VRIL_Steering.SteeringTechnique.HandDirected)
            {
                steering.Mode = (VRIL_Steering.SteeringMode)EditorGUILayout.EnumPopup("Mode: ", steering.Mode);
            }
        }
    }
}
