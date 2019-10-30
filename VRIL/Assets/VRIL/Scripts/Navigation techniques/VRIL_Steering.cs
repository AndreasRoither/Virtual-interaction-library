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

        [Header("Properties for steering technique")]
        [Tooltip("Flying mode includes y-coordinate too")]
        public bool FlyingMode = false;
        [Tooltip("Include button press strength")]
        public bool IncludeButtonPressStrength = false;
        [Tooltip("Default velocity")]
        public float Velocity = 2.0f;

        public void Awake()
        {
            base.Initialize();
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
            IsActivated = true;
            StartCoroutine(PerformTravel(e));
        }

        protected IEnumerator PerformTravel(VRIL_ControllerActionEventArgs e)
        {
            while (IsActivated)
            {
                // in flying mode include y axis too
                if (FlyingMode)
                {
                    SelectedPosition = Viewpoint.transform.position + (RegisteredControllers[0].transform.forward.normalized * Velocity * Time.deltaTime);
                }
                // else y axis is not included
                else
                {
                    Vector3 forwardVec = RegisteredControllers[0].transform.forward.normalized;
                    SelectedPosition = Viewpoint.transform.position + (new Vector3(forwardVec.x, 0, forwardVec.z) * Velocity * Time.deltaTime);
                }
                InitDistancesToViewpoint();
                Viewpoint.transform.position = SelectedPosition;
                UpdateObjects();
                yield return null;
            }
        }
    }
}
