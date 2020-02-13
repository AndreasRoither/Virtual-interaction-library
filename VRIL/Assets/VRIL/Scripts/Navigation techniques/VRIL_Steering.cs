using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.InteractionTechniques;

namespace VRIL.NavigationTechniques
{
    public class VRIL_Steering : VRIL_NavigationTechniqueBase
    {
        protected bool IsActivated = false;

        [Header("Properties for steering technique (Default is hand directed steering)")]
        [Tooltip("Flying mode includes y-coordinate too")]
        public bool FlyingMode = false;
        [Tooltip("Default velocity")]
        public float Velocity = 2.0f;

        [Header("Properties for gaze directed or crosshairs mode")]
        public bool GazeDirected = false;
        public bool CrosshairsMode = false;
        [Tooltip("Camera object needed in case of gaze directed steering or crosshairs mode")]
        public GameObject Camera;

        private GameObject DirectionObject;

        public void Awake()
        {
            base.Initialize();
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
                Vector3 forward = (CrosshairsMode ? (RegisteredControllers[0].transform.position - Camera.transform.position) : DirectionObject.transform.forward).normalized;

                // in flying mode include y axis too
                if (FlyingMode)
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
    }
}
