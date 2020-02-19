using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.NavigationTechniques;

namespace Assets.VRIL.Scripts.Navigation_techniques
{
    /// <summary>
    /// Implementation for grab the air technique
    /// </summary>
    public class VRIL_GrabTheAir : VRIL_NavigationTechniqueBase
    {


        // *************************************
        // public properties
        // *************************************

        [Header("Grab the Air Settings")]
        [Tooltip("The parent object of the whole world")]
        public GameObject World;
        [Tooltip("The movement gets multiplicated with this factor")]
        public float MovementMultiplicator = 1.0f;
        [Tooltip("Include changes of y-axis")]
        public bool Flying = false;


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
            Vector3 diff = World.transform.position - RegisteredControllers[0].transform.position;
            PrevPosition = RegisteredControllers[0].transform.position;
            while (IsActivated)
            {
                Vector3 newDiff = (RegisteredControllers[0].transform.position - PrevPosition) * MovementMultiplicator;
                World.transform.position += (Flying ? newDiff : new Vector3(newDiff.x, 0, newDiff.z));
                PrevPosition = RegisteredControllers[0].transform.position;
                yield return null;
            }
            StopAudio();
        }
    }
}
