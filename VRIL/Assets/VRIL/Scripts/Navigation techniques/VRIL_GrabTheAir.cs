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
    public class VRIL_GrabTheAir : VRIL_NavigationTechniqueBase
    {

        public GameObject World;

        protected bool IsActivated = false;

        public void Awake()
        {
            base.Initialize();
        }

        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (RegisteredControllers.Count > 0)
            {
                // differentiate between ButtonStates
                if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Released)
                {
                    IsActivated = false;
                    World.transform.parent = null;
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
            while (IsActivated)
            {
                World.transform.position = diff + RegisteredControllers[0].transform.position;
                yield return null;
            }
            StopAudio();
        }
    }
}
