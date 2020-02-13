using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRIL.InteractionTechniques;
using VRIL.ControllerActionEventArgs;

namespace VRIL.NavigationTechniques
{
    public class VRIL_DashTeleport : VRIL_Teleport
    {
        [Header("Dash Teleport Settings")]
        [Tooltip("Define velocity of dash movement")]
        public float Velocity = 60.0f;

        private bool TravelMode = false;

        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if (PositionSelected && Timer > TimeToWaitForNextTeleport)
            {
                // allow no other input while travelling to target position
                Manager.InputLocked = true;
                TravelMode = true;
                InitDistancesToViewpoint();
                PlayAudio();
                StartCoroutine(DashMovement());
            }
            HitEntity?.SetActive(false);
            IsActivated = false;
        }

        /// <summary>
        /// Performs the dash movement
        /// </summary>
        /// <returns></returns>
        protected IEnumerator DashMovement()
        {
            while (TravelMode)
            {
                if (Vector3.Distance(Viewpoint.transform.position, SelectedPosition) <= Velocity * Time.deltaTime)
                {
                    Viewpoint.transform.position = SelectedPosition;
                }
                else
                {
                    Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position, SelectedPosition, Velocity * Time.deltaTime);
                }
                UpdateObjects();

                // check if the positions are approximately equal
                if (Vector3.Distance(Viewpoint.transform.position, SelectedPosition) < 0.001f)
                {
                    PositionSelected = false;
                    TravelMode = false;
                    Manager.InputLocked = false;
                    Timer = 0.0f;
                }
                yield return null;
            }
        }
    }
}
