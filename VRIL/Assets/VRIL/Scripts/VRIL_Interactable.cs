using System.Collections;
using UnityEngine;
using VRIL.Base;
using VRIL.Callable;

namespace VRIL.Interactable
{
    public enum VRIL_AudioSource
    {
        Selection,
        Interaction,
        Release
    }

    /// <summary>
    /// Interactable Object class, gameobjects that can be selected by an interaction technique
    /// should have this script attached
    /// </summary>
    /// <see cref="VRIL.InteractionTechniques.VRIL_InteractionTechniqueBase"/>
    [System.Serializable]
    public class VRIL_Interactable : MonoBehaviour
    {
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private bool isMoving = false;
        private Transform baseParent;

        private float startTime;
        private float journeyLength;

        private Vector3 newPosition;
        private Quaternion newRotation;
        private float newSpeed;

        private GameObject attachedObject;

        private bool isSelected = false;

        // ++++++++++++++++++++
        // General
        // ++++++++++++++++++++
        public bool General_OneAudioSource;

        public AudioSource General_AudioSource;

        // ++++++++++++++++++++
        // Selection
        // ++++++++++++++++++++
        public bool Selection_Selectable = true;

        public bool Selection_Feedback;
        public bool Selection_ControllerSwappable;
        public bool Selection_MoveToAttachmentType;
        public float Selection_MoveToAttachmentTypeSpeed;

        public ObjectAttachmentTypes Selection_ObjectAttachment;
        public float Selection_OffSet;

        public AudioClip Selection_AudioClip;
        public AudioSource Selection_AudioSource;
        public VRIL_Callable Selection_Script;
        public VRIL_Callable Selection_FeedbackScript;

        // ++++++++++++++++++++
        // Manipulation
        // ++++++++++++++++++++
        public bool Interactable;

        public bool Interaction_HoldButtonToInteract;
        public bool Interaction_Feedback;

        public bool Interaction_Manipulatable;

        public bool Interaction_Manipulation_PositionChangeAble;
        public bool Interaction_Manipulation_RotationChangeAble;

        public ObjectAttachmentTypes Interaction_ObjectAttachment;

        public AudioClip Interaction_AudioClip;
        public AudioSource Interaction_AudioSource;
        public VRIL_Callable Interaction_Script;
        public VRIL_Callable Interaction_FeedbackScript;

        // ++++++++++++++++++++
        // Release
        // ++++++++++++++++++++
        public bool Release_Releaseable = true;

        public bool Release_Feedback;
        public float Release_MoveToReleaseLocationSpeed;

        public ObjectReleaseLocationType Release_ObjectFinalLocation;

        public AudioClip Release_AudioClip;
        public AudioSource Release_AudioSource;
        public VRIL_Callable Release_Script;
        public VRIL_Callable Release_FeedbackScript;

        /// <summary>
        /// Initilaize
        /// </summary>
        public void Start()
        {
            basePosition = this.transform.position;
            baseRotation = this.transform.rotation;
            baseParent = this.transform.parent;
        }

        public void Update()
        {
            if (attachedObject != null)
            {
                Ray ray = new Ray(attachedObject.transform.position, attachedObject.transform.forward);
                newPosition = ray.GetPoint(Selection_OffSet);
                newRotation = this.transform.rotation;
            }
        }

        /// <summary>
        /// Called when the Object is selected
        /// Plays audio
        /// Only use this method if you don't want to move or rotate the gameobject
        /// </summary>
        public void OnSelection()
        {
            if (Selection_Selectable)
            {
                isMoving = false;
                isSelected = true;
                Selection_Script?.OnCall();

                if (Selection_Feedback)
                {
                    PlayAudio(VRIL_AudioSource.Selection);
                    Selection_FeedbackScript?.OnCall();
                }
            }
        }

        /// <summary>
        /// Called when the Object is selected
        /// Plays audio
        /// Attach to transfomr
        /// </summary>
        public void OnSelection(GameObject objecToAttachTo)
        {
            if (Selection_Selectable)
            {
                isMoving = false;
                isSelected = true;
                Selection_Script?.OnCall();

                if (Selection_Feedback)
                {
                    PlayAudio(VRIL_AudioSource.Selection);
                    Selection_FeedbackScript?.OnCall();
                }

                if (Selection_MoveToAttachmentType)
                {
                    attachedObject = objecToAttachTo;
                    this.transform.SetParent(objecToAttachTo.transform);

                    Ray ray = new Ray(objecToAttachTo.transform.position, objecToAttachTo.transform.forward);
                    newPosition = ray.GetPoint(Selection_OffSet);
                    newRotation = this.transform.rotation;

                    this.newSpeed = Selection_MoveToAttachmentTypeSpeed;
                    StartCoroutine(MoveAndRotateTowardsPosition());
                }
            }
        }

        /// <summary>
        /// Should be called when the object is selected
        /// If Selection_MoveToAttachmentType is not set to true, use null values
        /// Uses position and new rotation
        /// </summary>
        /// <param name="newPosition">new position of the object</param>
        /// <param name="newRotation">new rotation of the object</param>
        public void OnSelection(Vector3 newPosition, Quaternion newRotation)
        {
            if (Selection_Selectable)
            {
                isMoving = false;
                isSelected = true;
                Selection_Script?.OnCall();

                if (Selection_Feedback)
                {
                    PlayAudio(VRIL_AudioSource.Selection);
                    Selection_FeedbackScript?.OnCall();
                }

                if (Selection_MoveToAttachmentType)
                {
                    if (Selection_MoveToAttachmentTypeSpeed == 0)
                    {
                        transform.position = newPosition;
                        transform.rotation = newRotation;
                    }
                    else
                    {
                        this.newPosition = newPosition;
                        this.newRotation = newRotation;
                        this.newSpeed = Selection_MoveToAttachmentTypeSpeed;
                        StartCoroutine(MoveAndRotateTowardsPosition());
                    }
                }
            }
        }

        /// <summary>
        /// Should be called when an interaction with the object occurs
        /// </summary>
        /// <param name="pressedButton"></param>
        public void OnInteraction(bool pressedButton)
        {
            if (Interactable)
            {
                if ((Interaction_HoldButtonToInteract == false) || (Interaction_HoldButtonToInteract == true && pressedButton == true))
                {
                    Interaction_Script?.OnCall();

                    if (Interaction_Feedback)
                    {
                        PlayAudio(VRIL_AudioSource.Interaction);
                        Interaction_FeedbackScript?.OnCall();
                    }
                }
            }
        }

        /// <summary>
        /// Should be called when the interaction with the object stopped
        /// </summary>
        public void OnInteractionStop()
        {
            Selection_Script?.OnStop();
            Interaction_Script?.OnStop();

            if (Interaction_Feedback)
            {
                StopAudio(VRIL_AudioSource.Interaction);
                Interaction_FeedbackScript?.OnStop();
            }
        }

        /// <summary>
        /// Called when the object is released
        /// </summary>
        /// <param name="newPosition">Position to move to</param>
        public void OnRelease(Vector3 newPosition)
        {
            if (Release_Releaseable && isSelected)
            {
                this.transform.SetParent(baseParent);

                Release_Script?.OnCall();

                switch (Release_ObjectFinalLocation)
                {
                    case ObjectReleaseLocationType.BaseLocationWithBaseRotation:
                        if (Release_MoveToReleaseLocationSpeed == 0)
                        {
                            this.transform.position = basePosition;
                            this.transform.rotation = baseRotation;
                        }
                        else
                        {
                            this.newPosition = basePosition;
                            this.newRotation = baseRotation;
                            this.newSpeed = Release_MoveToReleaseLocationSpeed;
                            StartCoroutine(MoveAndRotateTowardsPosition());
                        }
                        break;

                    case ObjectReleaseLocationType.BaseLocationWithNewRotation:
                        if (Release_MoveToReleaseLocationSpeed == 0)
                        {
                            this.transform.position = basePosition;
                            baseRotation = this.transform.rotation;
                        }
                        else
                        {
                            this.newPosition = basePosition;
                            this.newRotation = this.transform.rotation;
                            this.newSpeed = Release_MoveToReleaseLocationSpeed;
                            StartCoroutine(MoveAndRotateTowardsPosition());
                        }
                        break;

                    case ObjectReleaseLocationType.CurrentLocation:
                        break;

                    case ObjectReleaseLocationType.NewLocation:
                        this.transform.position = newPosition;
                        basePosition = newPosition;
                        baseRotation = this.transform.rotation;
                        break;
                }

                if (Release_Feedback)
                {
                    PlayAudio(VRIL_AudioSource.Release);
                    Release_FeedbackScript?.OnCall();
                }

                isSelected = false;
                attachedObject = null;
            }
        }

        /// <summary>
        /// Plays an audio clip at an audio source
        /// </summary>
        /// <param name="audioSource"></param>
        public void PlayAudio(VRIL_AudioSource audioSource)
        {
            AudioClip tempAudio = null;
            AudioSource tempSource = null;

            if (General_OneAudioSource)
            {
                tempSource = General_AudioSource;
            }
            else
            {
                switch (audioSource)
                {
                    case VRIL_AudioSource.Selection:
                        tempSource = Selection_AudioSource;
                        break;

                    case VRIL_AudioSource.Interaction:
                        tempSource = Interaction_AudioSource;

                        break;

                    case VRIL_AudioSource.Release:
                        tempSource = Release_AudioSource;
                        break;
                }
            }

            switch (audioSource)
            {
                case VRIL_AudioSource.Selection:
                    tempAudio = Selection_AudioClip;
                    break;

                case VRIL_AudioSource.Interaction:
                    tempAudio = Interaction_AudioClip;
                    break;

                case VRIL_AudioSource.Release:
                    tempAudio = Release_AudioClip;
                    break;
            }

            if (tempSource != null && tempAudio != null)
            {
                tempSource.clip = tempAudio;
                tempSource.Play(0);
            }
        }

        /// <summary>
        /// Stops playing audio on audio sources
        /// </summary>
        /// <param name="audioSource">AudioSource specified</param>
        public void StopAudio(VRIL_AudioSource audioSource)
        {
            switch (audioSource)
            {
                case VRIL_AudioSource.Selection:
                    Selection_AudioSource?.Stop();
                    break;

                case VRIL_AudioSource.Interaction:
                    Interaction_AudioSource?.Stop();
                    break;

                case VRIL_AudioSource.Release:
                    Release_AudioSource?.Stop();
                    break;
            }
        }

        /// <summary>
        /// Moves and rotates the gameobject towards a certain point given the position and rotation
        /// </summary>
        /// <param name="newPosition">The position to move to</param>
        /// <param name="newRotation">The new rotation the gameobject should have</param>
        /// <param name="speed">Movement speed</param>
        /// <returns></returns>
        private IEnumerator MoveAndRotateTowardsPosition()
        {
            // calculate or recalculate if it's already moving since there is probably a new position
            startTime = Time.time;
            journeyLength = Vector3.Distance(this.transform.position, this.newPosition);

            // only one instance of this function running
            if (isMoving)
            {
                // exit if it's still runnning
                yield break;
            }
            isMoving = true;

            // bool in case we want to exit from outside of the function
            while (isMoving)
            {
                // position
                if (this.transform.position != this.newPosition)
                {
                    // Distance moved = time * speed.
                    float distCovered = (Time.time - startTime) * newSpeed;

                    // Fraction of journey completed = current distance divided by total distance.
                    float fracJourney = distCovered / journeyLength;

                    // Set our position as a fraction of the distance between the markers.
                    this.transform.position = Vector3.Lerp(this.transform.position, this.newPosition, fracJourney);
                }

                // rotation
                if (this.transform.rotation != this.newRotation)
                {
                    transform.rotation = Quaternion.Lerp(this.transform.rotation, this.newRotation, (Time.time - startTime) * newSpeed);
                }

                // exit if finished moving
                if (this.transform.position == this.newPosition && this.transform.rotation == this.newRotation) isMoving = false;

                yield return null;
            }
        }
    }
}