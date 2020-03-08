using System.Collections;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.NavigationTechniques;

namespace Assets.VRIL.Scripts.Navigation_techniques
{
    /// <summary>
    /// Implementation for the grab the air technique
    /// </summary>
    public class VRIL_GrabTheAir : VRIL_NavigationTechniqueBase
    {
        // *************************************
        // public properties
        // *************************************

        [Header("Grab the Air Settings")] [Tooltip("The parent object of the whole world")]
        public GameObject World;

        [Tooltip("The movement gets multiplicated with this factor")]
        public float MovementScalor = 1.0f;

        [Tooltip("Enable axes for navigation (for flying mode enable all axes for navigation)")]
        public bool EnableNavigationX = true;

        public bool EnableNavigationY = false;
        public bool EnableNavigationZ = true;


        // *************************************
        // private and protected members
        // *************************************

        protected bool IsActivated = false;
        private Vector3 PrevPosition;

        public void Awake()
        {
            Initialize();
        }

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
                Debug.LogError($"<b>{nameof(VRIL_GrabTheAir)}:</b>\n No controller registered");
            }
        }

        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            PlayAudio();
            IsActivated = true;
            StartCoroutine(GrabTheAir(e));
        }

        protected IEnumerator GrabTheAir(VRIL_ControllerActionEventArgs e)
        {
            PrevPosition = RegisteredControllers[0].transform.position;
            while (IsActivated)
            {
                Vector3 newDiff = (RegisteredControllers[0].transform.position - PrevPosition) * MovementScalor;
                Vector3 nextPosition = Vector3.zero;
                if (EnableNavigationX)
                {
                    nextPosition.x = newDiff.x;
                }

                if (EnableNavigationY)
                {
                    nextPosition.y = newDiff.y;
                }

                if (EnableNavigationZ)
                {
                    nextPosition.z = newDiff.z;
                }

                SaveDistancesToViewpoint();
                World.transform.position += nextPosition;
                PrevPosition = RegisteredControllers[0].transform.position;
                TransferSelectedObjects();
                yield return null;
            }

            StopAudio();
        }
    }
}