using System.Collections.Generic;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.InteractionTechniques;
using VRIL.SDKBase;

namespace VRIL.Manager
{
    public delegate void ControllerActionEventHandler(object sender, VRIL_ControllerActionEventArgs e);

    /// <summary>
    /// Class for registering techniques to a controller
    /// </summary>
    [System.Serializable]
    public class VRIL_RegisteredController
    {
        public GameObject Controller;
        public List<VRIL_InteractionTechniqueBase> Techniques;
    }

    /// <summary>
    /// Manager that manages controller input and reroutes it to the techniques that registered for a specific controller
    /// </summary>
    public class VRIL_Manager : MonoBehaviour
    {
        //[Header("Registered Controller and Techniques")]
        public List<VRIL_RegisteredController> RegisteredControllers = new List<VRIL_RegisteredController>();
        
        //[Header("SDK Scripts")]
        public VRIL_LoadedSdkNames LoadedSdkName;
        public VRIL_SDKBase OpenVR_Script;
        public VRIL_SDKBase Oculus_Script;
        public VRIL_SDKBase CustomSDK_Script;

        /// <summary>
        /// Should be called when a Controller Interaction happened (Button pressed for example)
        /// <para>Notifies all registered techniques for a specific controller</para>
        /// </summary>
        /// <param name="e">VRIL_ControllerActionEventArgs</param>
        /// <see cref="VRIL_ControllerActionEventArgs"/>
        public void OnControllerAction(VRIL_ControllerActionEventArgs e)
        {
            if (e == null)
            {
                Debug.LogWarning($"<b>{nameof(VRIL_Manager)}:</b>\n {nameof(VRIL_ControllerActionEventArgs)} was null at {nameof(OnControllerAction)}");
                return;
            }

            if (e.ControllerIndex > RegisteredControllers.Count - 1)
            {
                Debug.LogWarning($"<b>{nameof(VRIL_Manager)}:</b>\n {nameof(VRIL_ControllerActionEventArgs)} ControllerIndex was greater than registered controller number at {nameof(OnControllerAction)}");
                return;
            }

            foreach (VRIL_InteractionTechniqueBase technique in RegisteredControllers[e.ControllerIndex].Techniques)
            {
                if (technique != null)
                {
                    technique.ControllerAction(this, e);
                }
            }
        }

        /// <summary>
        /// Returns the requested buttonState
        /// </summary>
        /// <param name="button">Button that should be checked</param>
        /// <param name="controllerIndex">Controller that should be checked for the button state</param>
        /// <returns>VRIL_ButtonInteractionType</returns>
        public VRIL_ButtonInteractionType GetButtonState(VRIL_ButtonType button, GameObject controller)
        {
            switch (LoadedSdkName)
            {
                case VRIL_LoadedSdkNames.None:
                    return VRIL_ButtonInteractionType.None;

                case VRIL_LoadedSdkNames.OpenVR:
                    if (OpenVR_Script != null)
                    {
                        return OpenVR_Script.GetButtonState(button, controller);
                    }
                    break;

                case VRIL_LoadedSdkNames.Oculus:
                    if (Oculus_Script != null)
                    {
                        return Oculus_Script.GetButtonState(button, controller);
                    }
                    break;

                case VRIL_LoadedSdkNames.CustomSDK:
                    if (CustomSDK_Script != null)
                    {
                        return CustomSDK_Script.GetButtonState(button, controller);
                    }
                    break;
            }

            // if no script is attached
            return VRIL_ButtonInteractionType.None;
        }

        /// <summary>
        /// Returns all registered techniques for that interaction technique
        /// </summary>
        /// <returns>List<VRIL_RegisteredController></returns>
        public List<GameObject> GetRegisteredControllers(VRIL_InteractionTechniqueBase technique)
        {
            List<GameObject> controllers = new List<GameObject>();
            foreach (VRIL_RegisteredController registeredController in RegisteredControllers)
            {
                if (registeredController.Techniques.Contains(technique))
                    controllers.Add(registeredController.Controller);
            }
            return controllers;
        }
    }
}