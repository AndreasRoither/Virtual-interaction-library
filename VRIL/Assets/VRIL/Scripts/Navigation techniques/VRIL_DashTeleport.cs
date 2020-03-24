using System.Collections;
using UnityEngine;
using VRIL.ControllerActionEventArgs;

namespace VRIL.NavigationTechniques
{
    /// <summary>
    /// Implementation of a teleport technique with dash mode
    /// </summary>
    public class VRIL_DashTeleport : VRIL_TeleportBase
    {
        // *************************************
        // public properties
        // *************************************

        [Header("Dash Settings")] [Tooltip("Define velocity of dash movement")]
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
            // if a valid position is selected, travel is triggered
            if (PositionSelected && !DelayToNextTravel)
            {
                // allow no other input while travelling to target position
                Manager.InputLocked = true;
                TravelMode = true;
                SaveDistancesToViewpoint();
                PlayAudio();
                StartCoroutine(DashMovement());
                DelayToNextTravel = true;
            }

            if (HitEntity)
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
                if (Vector3.Distance(Viewpoint.transform.position, TargetPosition) <= Velocity * Time.deltaTime)
                {
                    Viewpoint.transform.position = TargetPosition;
                }
                else
                {
                    Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position, TargetPosition,
                        Velocity * Time.deltaTime);
                }

                TransferSelectedObjects();

                // check if the positions are approximately equal
                if (Vector3.Distance(Viewpoint.transform.position, TargetPosition) <= TOLERANCE_POINT_REACHED)
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