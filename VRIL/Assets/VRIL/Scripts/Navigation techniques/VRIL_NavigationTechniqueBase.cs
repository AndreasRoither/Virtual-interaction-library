namespace VRIL.NavigationTechniques
{
    using System.Collections.Generic;
    using UnityEngine;
    using VRIL.Interactable;
    using VRIL.Manager;
    using VRIL.ControllerActionEventArgs;
    using VRIL.Base;
    using VRIL.TechniqueBase;
    using VRIL.InteractionTechniques;

    /// <summary>
    /// Base class for all VRIL Navigation Techniques
    /// </summary>
    public abstract class VRIL_NavigationTechniqueBase : VRIL_TechniqueBase
    {

        // *************************************
        // public properties
        // *************************************

        [Header("General Technique Settings")]
        public GameObject Viewpoint;
        [Tooltip("Define distance viewpoint to ground")]
        public float DistanceToGround = 1.3f;
        [Tooltip("Allow to travel with selected objects")]
        public bool MoveSelectedObjects = false;
        [Tooltip("Only necessary when controllers are no child objects of viewpoint")]
        public bool MoveControllerSeperately = false;

        [Header("Audio Settings")]
        public AudioClip TravelAudioClip;
        public AudioSource TravelAudioSource;


        // *************************************
        // protected members
        // *************************************

        protected Dictionary<int, Vector3> ControllerDistancesToViewpoint = new Dictionary<int, Vector3>();
        protected Dictionary<int, Vector3> SelectedObjectDistancesToViewpoint = new Dictionary<int, Vector3>();
        protected bool PositionSelected = false;
        protected Vector3 SelectedPosition = new Vector3(0, 0, 0);
        protected bool TravelOnRelease = true;

        /// <summary>
        /// Called from VRIL_Manager when a button is pressed
        /// </summary>
        /// <param name="sender">sending class</param>
        /// <param name="e">EventArgs that contains the controller, buttonType, buttonInteractionType</param>
        /// <see cref="VRIL_Manager"/>
        /// <seealso cref="VRIL_ControllerActionEventArgs"/>
        public override void ControllerAction(object sender, VRIL_ControllerActionEventArgs e)
        {
            bool actionDone = false;
            foreach (VRIL_ActionMapping mapping in Mappings)
            {
                if (mapping.ButtonType == e.ButtonType)
                {
                    switch (mapping.ActionType)
                    {
                        case VRIL_ActionTypes.None:
                            actionDone = true;
                            break;
                        case VRIL_ActionTypes.OnActivation:
                            actionDone = true;
                            OnActivation(e);
                            break;
                        case VRIL_ActionTypes.OnTravel:
                            actionDone = true;
                            OnTravel(e);
                            break;
                        case VRIL_ActionTypes.CustomScript:
                            actionDone = true;
                            mapping.Script.OnCall();
                            break;
                    }
                    if (actionDone)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Init object distances to viewpoint before travel (controller and selected objects move too)
        /// </summary>
        protected void InitDistancesToViewpoint()
        {
            foreach (VRIL_RegisteredController regController in Manager.RegisteredControllers)
            {
                if (MoveSelectedObjects)
                {
                    foreach (VRIL_InteractionTechniqueBase interactionTechnique in regController.InteractionTechniques)
                    {
                        foreach (VRIL_Interactable selectedObject in interactionTechnique.GetSelectedObjects())
                        {
                            SelectedObjectDistancesToViewpoint[selectedObject.GetInstanceID()] = selectedObject.transform.position - Viewpoint.transform.position;
                        }
                    }
                }
                if(MoveControllerSeperately)
                {
                    ControllerDistancesToViewpoint[regController.Controller.GetInstanceID()] = regController.Controller.transform.position - Viewpoint.transform.position;
                }
            }
        }

        /// <summary>
        /// Updates the position of all objects related to the user (controllers and selected objects)
        /// </summary>
        protected void UpdateObjects(float? angle = null)
        {
            if(!MoveSelectedObjects && !MoveControllerSeperately)
            {
                return;
            }
            foreach (VRIL_RegisteredController regController in Manager.RegisteredControllers)
            {
                if (MoveSelectedObjects)
                {
                    foreach (VRIL_InteractionTechniqueBase interactionTechnique in regController.InteractionTechniques)
                    {
                        foreach (VRIL_Interactable selectedObject in interactionTechnique.GetSelectedObjects())
                        {
                            selectedObject.transform.position = SelectedObjectDistancesToViewpoint[selectedObject.GetInstanceID()] + Viewpoint.transform.position;
                            if(angle != null)
                            {
                                selectedObject.transform.RotateAround(Viewpoint.transform.position, Viewpoint.transform.up, angle ?? 0.0f);
                            }
                        }
                    }
                }
                if (MoveControllerSeperately)
                {
                    regController.Controller.transform.position = ControllerDistancesToViewpoint[regController.Controller.GetInstanceID()] + Viewpoint.transform.position;
                    if (angle != null)
                    {
                        regController.Controller.transform.RotateAround(Viewpoint.transform.position, Viewpoint.transform.up, angle ?? 0.0f);
                    }
                }
            }
        }

        /// <summary>
        /// Plays an audio clip
        /// </summary>
        protected void PlayAudio()
        {
            if(TravelAudioSource && TravelAudioClip)
            {
                TravelAudioSource.clip = TravelAudioClip;
                TravelAudioSource.Play(0);
            }
        }

        /// <summary>
        /// Stops an audio clip
        /// </summary>
        protected void StopAudio()
        {
            if (TravelAudioSource)
            {
                TravelAudioSource.Stop();
            }  
        }

        /// <summary>
        /// Checks if a mapping for OnTravel exists (in case nothing exist: button release triggers travel)
        /// </summary>
        protected void CheckInputOnRelease()
        {
            foreach (VRIL_ActionMapping mapping in Mappings)
            {
                if(mapping.ActionType == VRIL_ActionTypes.OnTravel)
                {
                    TravelOnRelease = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Called when the travel should be performed
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public abstract void OnTravel(VRIL_ControllerActionEventArgs e);

    }
}