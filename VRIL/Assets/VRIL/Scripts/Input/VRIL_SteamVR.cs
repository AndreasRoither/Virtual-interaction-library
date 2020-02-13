using System;
using UnityEngine;
using Valve.VR;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Manager;

namespace VRIL_SteamVR
{

    /////////////////////////////////////
    /// Important!!!!!
    /// In order to use this script you have to generate actions via SteamVR menu! (unless you already created these actions)
    /// Window -> SteamVR Input
    /// There you can auto generate actions, which are used by this script!
    /// 
    /////////////////////////////////////

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

            /*interactUIAction.AddOnStateDownListener(OnInteractUIDown, SteamVR_Input_Sources.Any);
            teleportAction.AddOnStateDownListener(OnTeleportDown, SteamVR_Input_Sources.Any);
            grabPinchAction.AddOnStateDownListener(OnGrabPinchDown, SteamVR_Input_Sources.Any);
            grabGripAction.AddOnStateDownListener(OnGripActionDown, SteamVR_Input_Sources.Any);

            interactUIAction.AddOnStateUpListener(OnInteractUIUp, SteamVR_Input_Sources.Any);
            teleportAction.AddOnStateUpListener(OnTeleportUp, SteamVR_Input_Sources.Any);
            grabPinchAction.AddOnStateUpListener(OnGrabPinchUp, SteamVR_Input_Sources.Any);
            grabGripAction.AddOnStateUpListener(OnGrabGripUp, SteamVR_Input_Sources.Any);*/

            interactUIAction.AddOnStateDownListener(OnInteractUIDown, SteamVR_Input_Sources.LeftHand);
            teleportAction.AddOnStateDownListener(OnTeleportDown, SteamVR_Input_Sources.LeftHand);
            grabPinchAction.AddOnStateDownListener(OnGrabPinchDown, SteamVR_Input_Sources.LeftHand);
            grabGripAction.AddOnStateDownListener(OnGripActionDown, SteamVR_Input_Sources.LeftHand);

            interactUIAction.AddOnStateUpListener(OnInteractUIUp, SteamVR_Input_Sources.LeftHand);
            teleportAction.AddOnStateUpListener(OnTeleportUp, SteamVR_Input_Sources.LeftHand);
            grabPinchAction.AddOnStateUpListener(OnGrabPinchUp, SteamVR_Input_Sources.LeftHand);
            grabGripAction.AddOnStateUpListener(OnGrabGripUp, SteamVR_Input_Sources.LeftHand);

            interactUIAction.AddOnStateDownListener(OnInteractUIDown, SteamVR_Input_Sources.RightHand);
            teleportAction.AddOnStateDownListener(OnTeleportDown, SteamVR_Input_Sources.RightHand);
            grabPinchAction.AddOnStateDownListener(OnGrabPinchDown, SteamVR_Input_Sources.RightHand);
            grabGripAction.AddOnStateDownListener(OnGripActionDown, SteamVR_Input_Sources.RightHand);

            interactUIAction.AddOnStateUpListener(OnInteractUIUp, SteamVR_Input_Sources.RightHand);
            teleportAction.AddOnStateUpListener(OnTeleportUp, SteamVR_Input_Sources.RightHand);
            grabPinchAction.AddOnStateUpListener(OnGrabPinchUp, SteamVR_Input_Sources.RightHand);
            grabGripAction.AddOnStateUpListener(OnGrabGripUp, SteamVR_Input_Sources.RightHand);

        }

        /// <summary>
        /// Remove all listeners on disable
        /// </summary>
        public void OnDisable()
        {
            interactUIAction?.RemoveOnStateDownListener(OnInteractUIDown, SteamVR_Input_Sources.LeftHand);
            teleportAction?.RemoveOnStateDownListener(OnTeleportDown, SteamVR_Input_Sources.LeftHand);
            grabPinchAction?.RemoveOnStateDownListener(OnGrabPinchDown, SteamVR_Input_Sources.LeftHand);
            grabGripAction?.RemoveOnStateDownListener(OnGripActionDown, SteamVR_Input_Sources.LeftHand);

            interactUIAction?.RemoveOnStateUpListener(OnInteractUIUp, SteamVR_Input_Sources.LeftHand);
            teleportAction?.RemoveOnStateUpListener(OnTeleportUp, SteamVR_Input_Sources.LeftHand);
            grabPinchAction?.RemoveOnStateUpListener(OnGrabPinchUp, SteamVR_Input_Sources.LeftHand);
            grabGripAction?.RemoveOnStateUpListener(OnGrabGripUp, SteamVR_Input_Sources.LeftHand);

            interactUIAction?.RemoveOnStateDownListener(OnInteractUIDown, SteamVR_Input_Sources.RightHand);
            teleportAction?.RemoveOnStateDownListener(OnTeleportDown, SteamVR_Input_Sources.RightHand);
            grabPinchAction?.RemoveOnStateDownListener(OnGrabPinchDown, SteamVR_Input_Sources.RightHand);
            grabGripAction?.RemoveOnStateDownListener(OnGripActionDown, SteamVR_Input_Sources.RightHand);

            interactUIAction?.RemoveOnStateUpListener(OnInteractUIUp, SteamVR_Input_Sources.RightHand);
            teleportAction?.RemoveOnStateUpListener(OnTeleportUp, SteamVR_Input_Sources.RightHand);
            grabPinchAction?.RemoveOnStateUpListener(OnGrabPinchUp, SteamVR_Input_Sources.RightHand);
            grabGripAction?.RemoveOnStateUpListener(OnGrabGripUp, SteamVR_Input_Sources.RightHand);

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
            else if (fromSource == SteamVR_Input_Sources.RightHand)
            {
                v.ControllerIndex = 1;
            }

            manager.OnControllerAction(v);
        }
    }
}