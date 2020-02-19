using System.Collections;
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
        [Tooltip("Select steering mode")]
        public SteeringMode Mode;
        [Tooltip("Flying mode includes y-coordinate in travel too")]
        public bool Flying = false;
        [Tooltip("Viewpoint velocity")]
        public float Velocity = 2.0f;


        // *************************************
        // private and protected members
        // *************************************

        private GameObject DirectionObject;
        protected bool IsActivated = false;
        private GameObject Camera;

        public void Awake()
        {
            Initialize();
            if(Mode == SteeringMode.CrosshairsMode || Mode == SteeringMode.GazeDirected)
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
            DirectionObject = Camera != null ? Camera : RegisteredControllers[0];
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
                Vector3 forward = (Mode == SteeringMode.CrosshairsMode ? (RegisteredControllers[0].transform.position - Camera.transform.position) : DirectionObject.transform.forward).normalized;

                // in flying mode include y axis too
                if (Flying)
                {
                    SelectedPosition = Viewpoint.transform.position + (forward * Velocity * Time.deltaTime);
                }
                // else y axis is not included
                else
                {
                    SelectedPosition = Viewpoint.transform.position + (new Vector3(forward.x, 0, forward.z) * Velocity * Time.deltaTime);
                }
                InitDistancesToViewpoint();
                Viewpoint.transform.position = SelectedPosition;
                UpdateObjects();
                yield return null;
            }
            StopAudio();
        }

        public enum SteeringMode
        {
            HandDirected,
            GazeDirected,
            CrosshairsMode
        }
    }
}
