﻿using System.Collections;
using UnityEditor;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;

namespace VRIL.NavigationTechniques
{
    /// <summary>
    /// Implementation of steering based metaphors
    /// This class contains gaze directed steering as well as hand directed steering
    /// </summary>
    [System.Serializable]
    public class VRIL_Steering : VRIL_NavigationTechniqueBase
    {
        // *************************************
        // public properties
        // *************************************

        [Header("Steering Settings")]
        [Tooltip(
            "Enable axes for navigation (for flying mode enable all axes for navigation) and set separate velocities")]
        public bool EnableNavigationX = true;

        public float VelocityX = 2.0f;
        public bool EnableNavigationY = false;
        public float VelocityY = 2.0f;
        public bool EnableNavigationZ = true;
        public float VelocityZ = 2.0f;

        [Tooltip("Select steering type")] 
        public SteeringTechnique Technique;

        [HideInInspector] [Tooltip("Select steering mode")]
        public SteeringMode Mode;


        // *************************************
        // private members
        // *************************************

        private bool IsActivated = false;
        private GameObject SteeringObject;
        private GameObject Camera;

        public void Awake()
        {
            Initialize();

            // gaze directed steering and hand directed steering with pointing mode needs the camera object
            if (Mode == SteeringMode.CrosshairsMode || Technique == SteeringTechnique.GazeDirected)
            {
                if (HasComponent(Viewpoint, out Camera _))
                {
                    Camera = Viewpoint;
                }
                else if (HasComponent(Viewpoint, out Camera cam, true))
                {
                    Camera = cam.gameObject;
                }
                else
                {
                    Debug.LogWarning(
                        "Could not set camera object. No camera found in viewpoint object! Use hand-directed steering or check camera first.");
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
                Vector3 direction = (
                    Mode == SteeringMode.CrosshairsMode ? 
                        (RegisteredControllers[0].transform.position - Camera.transform.position) : SteeringObject.transform.forward
                ).normalized;

                Vector3 newPosition = Vector3.zero;
                if (EnableNavigationX)
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

                Vector3 vectorToNextPositon = newPosition;
                float deltaTime = Time.deltaTime;

                // set movement for each axis separately
                vectorToNextPositon.x *= VelocityX * deltaTime;
                vectorToNextPositon.y *= VelocityY * deltaTime;
                vectorToNextPositon.z *= VelocityZ * deltaTime;

                TargetPosition = Viewpoint.transform.position + vectorToNextPositon;

                // transfer viewpoint
                SaveDistancesToViewpoint();
                Viewpoint.transform.position = TargetPosition;
                TransferSelectedObjects();
                yield return null;
            }

            StopAudio();
        }

        /// <summary>
        /// enum for different steering techniques
        /// </summary>
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
}