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

        [Header("Travel Audio Settings")]
        public AudioClip AudioClip;
        public AudioSource AudioSource;


        // *************************************
        // protected members
        // *************************************

        protected Dictionary<int, Vector3> SelectedObjectDistancesToViewpoint = new Dictionary<int, Vector3>();
        protected bool PositionSelected = false;
        protected Vector3 TargetPosition = new Vector3(0, 0, 0);
        protected bool TravelOnRelease = true;
        protected float DistanceViewpointToGround;
        protected bool MoveSelectedObjects = true;

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
        protected void SaveDistancesToViewpoint()
        {
            if (!MoveSelectedObjects)
            {
                return;
            }

            foreach (VRIL_RegisteredController regController in Manager.RegisteredControllers)
            {

                foreach (VRIL_InteractionTechniqueBase interactionTechnique in regController.InteractionTechniques)
                {
                    foreach (VRIL_Interactable selectedObject in interactionTechnique.GetSelectedObjects())
                    {
                        SelectedObjectDistancesToViewpoint[selectedObject.GetInstanceID()] = selectedObject.transform.position - Viewpoint.transform.position;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the position of all objects related to the user (controllers and selected objects)
        /// </summary>
        protected void TransferSelectedObjects(float? angle = null)
        {
            if(!MoveSelectedObjects)
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
            }
        }

        /// <summary>
        /// Plays an audio clip
        /// </summary>
        protected void PlayAudio()
        {
            if(AudioSource && AudioClip)
            {
                AudioSource.clip = AudioClip;
                AudioSource.Play(0);
            }
        }

        /// <summary>
        /// Stops an audio clip
        /// </summary>
        protected void StopAudio()
        {
            if (AudioSource)
            {
                AudioSource.Stop();
            }  
        }

        public override void Initialize()
        {
            base.Initialize();

            // check if any action mapping is defined for OnTravel
            foreach (VRIL_ActionMapping mapping in Mappings)
            {
                if (mapping.ActionType == VRIL_ActionTypes.OnTravel)
                {
                    TravelOnRelease = false;
                    break;
                }
            }

            // check if any interaction techniques are registered
            foreach (VRIL_RegisteredController regController in Manager.RegisteredControllers)
            {
                if(regController.InteractionTechniques.Count > 0)
                {
                    MoveSelectedObjects = true;
                    break;
                }
            }

            //check distance to ground and save it
            Ray ray = new Ray(Viewpoint.transform.position, Viewpoint.transform.up * -1);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                VRIL_Navigable navigableObject = raycastHit.transform.gameObject.GetComponent<VRIL_Navigable>();
                if(navigableObject != null)
                {
                    DistanceViewpointToGround = Mathf.Abs(Viewpoint.transform.position.y - navigableObject.transform.position.y);
                    return;
                }
            }
            DistanceViewpointToGround = 0;
        }

        /// <summary>
        /// Called when the travel should be performed
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public abstract void OnTravel(VRIL_ControllerActionEventArgs e);

    }
}