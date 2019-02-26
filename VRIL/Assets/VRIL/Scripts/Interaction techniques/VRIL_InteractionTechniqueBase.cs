namespace VRIL.InteractionTechniques
{
    using System.Collections.Generic;
    using UnityEngine;
    using VRIL.Interactable;
    using VRIL.Manager;
    using VRIL.ControllerActionEventArgs;
    using VRIL.Base;

    /// <summary>
    /// Base class for all VRIL Interaction Techniques
    /// </summary>
    public abstract class VRIL_InteractionTechniqueBase : MonoBehaviour
    {
        [Header("VRIL Manager")]
        public VRIL_Manager Manager;

        [Header("Controller - Action Mapping")]
        public List<VRIL_ActionMapping> Mappings = new List<VRIL_ActionMapping>();
        public List<VRIL_Interactable> SelectableObjects = new List<VRIL_Interactable>();

        protected List<VRIL_Interactable> SelectedObjects = new List<VRIL_Interactable>();
        protected List<GameObject> RegisteredControllers = new List<GameObject>();

        [Header("Controller")]
        [Tooltip("First Controller has Index 0!")]
        public int DominantControllerIndex;

        /// <summary>
        /// Initalizes needed components
        /// <para>Checks if a Manager is applied and gets the necessary ressources</para>
        /// </summary>
        public void Initialize()
        {
            if (Manager != null)
            {
                RegisteredControllers = Manager.GetRegisteredControllers(this);
            }
            else
            {
                Debug.LogWarning($"<b>VRIL:</b>\nManager not assigned at {this.GetType().Name}");
            }
        }

        /// <summary>
        /// Called from VRIL_Manager when a button is pressed
        /// </summary>
        /// <param name="sender">sending class</param>
        /// <param name="e">EventArgs that contains the controller, buttonType, buttonInteractionType</param>
        /// <see cref="VRIL_Manager"/>
        /// <seealso cref="VRIL_ControllerActionEventArgs"/>
        public virtual void ControllerAction(object sender, VRIL_ControllerActionEventArgs e)
        {
            foreach (VRIL_ActionMapping mapping in Mappings)
            {
                if (mapping.ButtonType == e.ButtonType)
                {
                    switch (mapping.ActionType)
                    {
                        case VRIL_ActionTypes.None:
                            break;
                        case VRIL_ActionTypes.OnSelection:
                            OnSelection(e);
                            break;
                        case VRIL_ActionTypes.OnActivation:
                            OnActivation(e);
                            break;
                        case VRIL_ActionTypes.OnInteraction:
                            OnInteraction(e);
                            break;
                        case VRIL_ActionTypes.OnRelease:
                            OnRelease(e);
                            break;
                        case VRIL_ActionTypes.OnStop:
                            OnStop(e);
                            break;
                        case VRIL_ActionTypes.CustomScript:
                            mapping.Script.OnCall();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the interaction technique is activated
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public abstract void OnActivation(VRIL_ControllerActionEventArgs e);

        /// <summary>
        /// Called when the interaction technique should stop
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public abstract void OnStop(VRIL_ControllerActionEventArgs e);

        /// <summary>
        /// Called when the interaction technique should release an object
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public virtual void OnRelease(VRIL_ControllerActionEventArgs e)
        {
            foreach (VRIL_Interactable interactable in SelectableObjects)
            {
                interactable.OnRelease(interactable.transform.position);
            }

            foreach (VRIL_Interactable interactable in SelectedObjects)
            {
                interactable.OnRelease(interactable.transform.position);
            }

            SelectableObjects.Clear();
            SelectedObjects.Clear();
        }

        /// <summary>
        /// Called when the interaction technique should interact with the selected objects
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public virtual void OnInteraction(VRIL_ControllerActionEventArgs e)
        {
            foreach (VRIL_Interactable interactable in SelectableObjects)
            {
                bool pressed = (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed) ? true : false;
                interactable.OnInteraction(pressed);
            }
        }

        /// <summary>
        /// Called when the interaction technique selected an object
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public virtual void OnSelection(VRIL_ControllerActionEventArgs e)
        {
            foreach (VRIL_Interactable interactable in SelectableObjects)
            {
                if (interactable.Selection_ObjectAttachment == ObjectAttachmentTypes.NotAttached)
                {
                    interactable.OnSelection();
                }
                else if (interactable.Selection_ObjectAttachment == ObjectAttachmentTypes.AttachedToHand)
                {
                    interactable.OnSelection(RegisteredControllers[DominantControllerIndex]);
                }
                else
                {
                    interactable.OnSelection(RegisteredControllers[0].transform.position, interactable.transform.rotation);
                }
                
                if (!SelectedObjects.Contains(interactable))
                    SelectedObjects.Add(interactable);
            }

            SelectableObjects.Clear();
        }

        /// <summary>
        /// Check ButtonState
        /// </summary>
        /// <param name="button">Button that should be checked</param>
        /// <param name="controller">Controller that should be checked</param>
        /// <returns></returns>
        public virtual VRIL_ButtonInteractionType GetButtonState(VRIL_ButtonType button, GameObject controller)
        {
            if (Manager != null)
            {
                return Manager.GetButtonState(button, controller);
            }
            else
            {
                return VRIL_ButtonInteractionType.None;
            }
        }
    }
}