using System;
using UnityEngine;
using Valve.VR;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Manager;

namespace VRIL_SteamVR
{
    /// <summary>
    /// Example script that utilizes a few default actions to show how it works
    /// </summary>
    public class VRIL_SteamVR : MonoBehaviour
    {
        public VRIL_Manager manager;

        /* ****************
          Default SteamVR ActionSet (named default)
         *****************/
        [Header("Action Set")]
        public SteamVR_Action_Boolean interactUIAction;
        public SteamVR_Action_Boolean teleportAction;
        public SteamVR_Action_Boolean grabPinchAction;
        public SteamVR_Action_Boolean grabGripAction;

        /* ****************
          ButtonType for VRIL
         *****************/
        [Header("VRIL Button Convert")]
        public VRIL_ButtonType interact_VRIL_Button;
        public VRIL_ButtonType teleport_VRIL_Button;
        public VRIL_ButtonType grabPinch_VRIL_Button;
        public VRIL_ButtonType grabGrip_VRIL_Button;

        /// <summary>
        /// Set up all listeners on enable
        /// </summary>
        public void OnEnable()
        {
            if (interactUIAction == null || teleportAction == null || grabPinchAction == null || grabGripAction == null)
            {
                Debug.LogError($"<b>{nameof(VRIL_SteamVR)}:</b>\n An action has not been mapped!");
                return;
            }

            if (manager == null)
            {
                Debug.LogError($"<b>{nameof(VRIL_SteamVR)}:</b>\n Manager not set");
                return;
            }

            interactUIAction.AddOnStateDownListener(OnInteractUIDown, SteamVR_Input_Sources.Any);
            teleportAction.AddOnStateDownListener(OnTeleportDown, SteamVR_Input_Sources.Any);
            grabPinchAction.AddOnStateDownListener(OnGrabPinchDown, SteamVR_Input_Sources.Any);
            grabGripAction.AddOnStateDownListener(OnGripActionDown, SteamVR_Input_Sources.Any);

            interactUIAction.AddOnStateUpListener(OnInteractUIUp, SteamVR_Input_Sources.Any);
            teleportAction.AddOnStateUpListener(OnTeleportUp, SteamVR_Input_Sources.Any);
            grabPinchAction.AddOnStateUpListener(OnGrabPinchUp, SteamVR_Input_Sources.Any);
            grabGripAction.AddOnStateUpListener(OnGrabGripUp, SteamVR_Input_Sources.Any);
        }

        /// <summary>
        /// Remove all listeners on disable
        /// </summary>
        public void OnDisable()
        {
            interactUIAction?.RemoveOnStateDownListener(OnInteractUIDown, SteamVR_Input_Sources.Any);
            teleportAction?.RemoveOnStateDownListener(OnTeleportDown, SteamVR_Input_Sources.Any);
            grabPinchAction?.RemoveOnStateDownListener(OnGrabPinchDown, SteamVR_Input_Sources.Any);
            grabGripAction?.RemoveOnStateDownListener(OnGripActionDown, SteamVR_Input_Sources.Any);

            interactUIAction?.RemoveOnStateUpListener(OnInteractUIUp, SteamVR_Input_Sources.Any);
            teleportAction?.RemoveOnStateUpListener(OnTeleportUp, SteamVR_Input_Sources.Any);
            grabPinchAction?.RemoveOnStateUpListener(OnGrabPinchUp, SteamVR_Input_Sources.Any);
            grabGripAction?.RemoveOnStateUpListener(OnGrabGripUp, SteamVR_Input_Sources.Any);
        }

        /* ****************
          Button Down 
         *****************/
        private void OnInteractUIDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, interact_VRIL_Button, VRIL_ButtonInteractionType.Pressed);
        }

        private void OnTeleportDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, teleport_VRIL_Button, VRIL_ButtonInteractionType.Pressed);
        }

        private void OnGrabPinchDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, grabPinch_VRIL_Button, VRIL_ButtonInteractionType.Pressed);
        }

        private void OnGripActionDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, grabGrip_VRIL_Button, VRIL_ButtonInteractionType.Pressed);
        }

        /* ****************
          Button Up 
         *****************/
        private void OnInteractUIUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, interact_VRIL_Button, VRIL_ButtonInteractionType.Released);
        }

        private void OnTeleportUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, teleport_VRIL_Button, VRIL_ButtonInteractionType.Released);
        }

        private void OnGrabPinchUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, grabPinch_VRIL_Button, VRIL_ButtonInteractionType.Released);
        }

        private void OnGrabGripUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            NotifyControllerActionEvent(fromSource, grabGrip_VRIL_Button, VRIL_ButtonInteractionType.Released);
        }

        /// <summary>
        /// Notify manager with action event
        /// </summary>
        /// <param name="fromSource">Source input from SteamVR</param>
        /// <param name="vRIL_ButtonType">Specified VRIL ButtonType in Inspector</param>
        /// <param name="vRIL_ButtonInteractionType">Specified VRIL InteractionType in Inspector</param>
        public void NotifyControllerActionEvent(SteamVR_Input_Sources fromSource, VRIL_ButtonType vRIL_ButtonType, VRIL_ButtonInteractionType vRIL_ButtonInteractionType)
        {
            VRIL_ControllerActionEventArgs v = new VRIL_ControllerActionEventArgs
            {
                ButtonType = vRIL_ButtonType,
                ButtonInteractionType = vRIL_ButtonInteractionType
            };

            if (fromSource == SteamVR_Input_Sources.LeftHand)
            {
                v.ControllerIndex = 0;
            }
            else if (fromSource == SteamVR_Input_Sources.LeftHand)
            {
                v.ControllerIndex = 1;
            }

            manager.OnControllerAction(v);
        }
    }
}