using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRIL.InteractionTechniques;
using VRIL.ControllerActionEventArgs;

namespace VRIL.NavigationTechniques
{
    /// <summary>
    /// Implementation for a teleport with dash mode
    /// </summary>
    public class VRIL_DashTeleport : VRIL_TeleportBase
    {

        // *************************************
        // public properties
        // *************************************

        [Header("Dash Settings")]
        [Tooltip("Define velocity of dash movement")]
        public float Velocity = 60.0f;


        // *************************************
        // constants
        // *************************************

        private const float TOLERANCE_POINT_REACHED = 0.001f;


        // *************************************
        // private members
        // *************************************

        private bool TravelMode = false;

        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if (PositionSelected && TravelPauseTimer > SecondsToWaitForNextTeleport)
            {
                // allow no other input while travelling to target position
                Manager.InputLocked = true;
                TravelMode = true;
                InitDistancesToViewpoint();
                PlayAudio();
                StartCoroutine(DashMovement());
            }
            if(HitEntity)
            {
                HitEntity.SetActive(false);
            }
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
                if (Vector3.Distance(Viewpoint.transform.position, SelectedPosition) <= TOLERANCE_POINT_REACHED)
                {
                    PositionSelected = false;
                    TravelMode = false;
                    Manager.InputLocked = false;
                    TravelPauseTimer = 0.0f;
                }
                yield return null;
            }
        }
    }
}
