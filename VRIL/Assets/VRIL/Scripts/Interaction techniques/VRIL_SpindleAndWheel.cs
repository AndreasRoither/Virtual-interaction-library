using System.Collections;
using UnityEngine;
using VRIL.ControllerActionEventArgs;
using VRIL.Interactable;

namespace VRIL.InteractionTechniques
{
    [RequireComponent(typeof(LineRenderer))]
    public class VRIL_SpindleAndWheel : VRIL_InteractionTechniqueBase
    {
        private LineRenderer LineRenderer;
        private GameObject MainHand;
        private GameObject SecondaryHand;
        private bool selectionModeActivated;
        private bool manipulationModeActivated;
        private float CurrentSphereDiagonal;
        private float CurrentScale;

        [Space]
        [Header("Selection")]
        [Tooltip("SelectionModel should not have a collider!")]
        public GameObject SelectionModel;

        private GameObject SelectionModelInstance;

        [Range(0.0f, 5.0f)]
        public float MaxSphereDiagnoal = 1f;

        [Range(0.0f, 5.0f)]
        public float MinSphereDiagonal = 0.3f;

        [Space]
        [Header("Manipulation")]
        [Tooltip("0 = Infinite Scaling")]
        public float MaxScale;

        [Tooltip("0 = Infinite Scaling")]
        public float MinScale;

        [Tooltip("Distance divided by this factor to scale selection model")]
        public bool Scale = true;

        public float ScaleDivider = 2f;

        [Tooltip("Distance divided by this factor to scale selection model")]
        public float DistanceDivider = 4f;

        [Space]
        [Header("Coroutine")]
        [Range(0.0f, 0.1f)]
        [Tooltip("1 = 1 sec")]
        public float CoroutineWaitTime = 0.03f;

        public void Awake()
        {
            base.Initialize();
            LineRenderer = GetComponent<LineRenderer>();
        }

        public void Start()
        {
            if (SelectionModel?.GetComponent<Collider>())
            {
                Debug.LogError($"<b>{nameof(VRIL_SpindleAndWheel)}:</b>\n The SelectionModel is selectable! Remove any colliders from this gameobject.");
            }

            if (this.RegisteredControllers.Count <= 1)
            {
                Debug.LogError($"<b>{nameof(VRIL_SpindleAndWheel)}:</b>\n This technique requires at least two controllers!");
            }

            if (this.DominantControllerIndex > RegisteredControllers.Count - 1)
            {
                Debug.LogError($"<b>{nameof(VRIL_SpindleAndWheel)}:</b>\n DominantControllerIndex is greater than registered controllers! {this.DominantControllerIndex} > {this.RegisteredControllers.Count} \n Info: index starts at 0!");
            }
        }

        /// <summary>
        /// Draws collider spere in the scene view
        /// </summary>
        public void OnDrawGizmos()
        {
            if (SelectionModelInstance != null && selectionModeActivated)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(SelectionModelInstance.transform.position, CurrentSphereDiagonal / 2);
            }
        }

        /// <summary>
        /// Technique activation
        /// </summary>
        /// <param name="e">ControllerEvent</param>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (this.RegisteredControllers.Count > 1)
            {
                if (!selectionModeActivated && !manipulationModeActivated)
                {
                    LineRenderer.enabled = true;
                    selectionModeActivated = true;
                    StartCoroutine(SpindleAndWheelSelection());
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_SpindleAndWheel)}:</b>\n This technique requires at least two controllers!");
            }
        }

        /// <summary>
        /// Technique selection
        /// </summary>
        /// <param name="e">ControllerEvent</param>
        public override void OnSelection(VRIL_ControllerActionEventArgs e)
        {
            if (!manipulationModeActivated)
                base.OnSelection(e);
        }

        /// <summary>
        /// Technique interaction
        /// </summary>
        /// <param name="e">ControllerEvent</param>
        public override void OnInteraction(VRIL_ControllerActionEventArgs e)
        {
            if (this.RegisteredControllers.Count > 1)
            {
                if (SelectedObjects.Count >= 1 && !manipulationModeActivated)
                {
                    base.OnInteraction(e);
                    selectionModeActivated = false;
                    manipulationModeActivated = true;
                    StartCoroutine(SpindleAndWheelManpulation());
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_SpindleAndWheel)}:</b>\n This technique requires at least two controllers!");
            }
        }

        /// <summary>
        /// Technique stop
        /// </summary>
        /// <param name="e">ControllerEvent</param>
        public override void OnStop(VRIL_ControllerActionEventArgs e)
        {
            selectionModeActivated = false;
            manipulationModeActivated = false;
            LineRenderer.enabled = false;
        }

        /// <summary>
        /// Spindle and Wheel coroutine to manipulate a selected object
        /// </summary>
        /// <returns></returns>
        private IEnumerator SpindleAndWheelManpulation()
        {
            Vector3 pos;
            Vector3 relativePos;
            Vector3 initialScale = SelectedObjects[0].transform.localScale;
            Vector3 previousRelativePos = RegisteredControllers[0].transform.position - SelectedObjects[0].transform.position;
            Quaternion previousRotation = RegisteredControllers[DominantControllerIndex].transform.rotation;
            float previousDistance = 0f;

            while (manipulationModeActivated)
            {
                // *************************************
                // positioning
                // *************************************

                // set line position
                LineRenderer.SetPosition(0, RegisteredControllers[0].transform.position);
                LineRenderer.SetPosition(1, RegisteredControllers[1].transform.position);

                // set position in middle of controllers
                pos = RegisteredControllers[0].transform.position - (RegisteredControllers[0].transform.position - RegisteredControllers[1].transform.position) / 2;
                SelectedObjects[0].transform.position = pos;

                // *************************************
                // scaling
                // *************************************

                if (Scale)
                {
                    // resize based on connectionraydistance
                    float connectionRayDistance = (RegisteredControllers[0].transform.position - RegisteredControllers[1].transform.position).sqrMagnitude;

                    // rescale only on movement or the scaling will never stop
                    if (previousDistance != connectionRayDistance)
                    {
                        float scaleMultiplier = connectionRayDistance / ScaleDivider;
                        Vector3 newObjectScale = initialScale * scaleMultiplier;

                        if ((MinScale == 0 && MaxScale == 0) || ((newObjectScale.x <= MaxScale && newObjectScale.y <= MaxScale && newObjectScale.z <= MaxScale) &&
                            (newObjectScale.x >= MinScale && newObjectScale.y >= MinScale && newObjectScale.z >= MinScale)))
                            SelectedObjects[0].transform.localScale = newObjectScale;

                        previousDistance = connectionRayDistance;
                    }
                }

                // *************************************
                // set rotation for x dominant rotations
                // *************************************

                if (previousRotation != RegisteredControllers[DominantControllerIndex].transform.rotation)
                {
                    Vector3 eulerRotation = new Vector3(SelectedObjects[0].transform.eulerAngles.x, SelectedObjects[0].transform.eulerAngles.y, -RegisteredControllers[DominantControllerIndex].transform.rotation.eulerAngles.z);
                    SelectedObjects[0].transform.rotation = Quaternion.Euler(eulerRotation);

                    previousRotation = RegisteredControllers[DominantControllerIndex].transform.rotation;
                }

                // *************************************w
                // set rotation for yz rotations
                // *************************************
                relativePos = RegisteredControllers[0].transform.position - SelectedObjects[0].transform.position;

                if (previousRelativePos != relativePos)
                {
                    Vector3 rot = Quaternion.LookRotation(RegisteredControllers[0].transform.position - SelectedObjects[0].transform.position).eulerAngles;
                    rot.z = SelectedObjects[0].transform.rotation.eulerAngles.z;
                    SelectedObjects[0].transform.rotation = Quaternion.Euler(rot);

                    previousRelativePos = relativePos;
                }

                yield return new WaitForSeconds(CoroutineWaitTime);
            }

            yield return null;
        }

        /// <summary>
        /// Spindle and Wheel Selection Coroutine
        /// <para>Only used for the selection of models between controller</para>
        /// </summary>
        /// <returns></returns>
        private IEnumerator SpindleAndWheelSelection()
        {
            Vector3 pos = RegisteredControllers[0].transform.position - (RegisteredControllers[0].transform.position - RegisteredControllers[1].transform.position) / 2;
            Collider[] hitColliders;

            // set up selection model
            if (SelectionModelInstance == null)
            {
                SelectionModelInstance = Instantiate(SelectionModel, pos, Quaternion.identity);
                SelectionModelInstance.name = "VRIL_SelectionModel_SpindleAndWheel";

                float connectionRayDistance = (RegisteredControllers[0].transform.position - RegisteredControllers[1].transform.position).sqrMagnitude;
                CurrentSphereDiagonal = connectionRayDistance - connectionRayDistance / DistanceDivider;

                SelectionModelInstance.transform.localScale = new Vector3(CurrentSphereDiagonal, CurrentSphereDiagonal, CurrentSphereDiagonal);
            }

            while (selectionModeActivated)
            {
                // set line position
                LineRenderer.SetPosition(0, RegisteredControllers[0].transform.position);
                LineRenderer.SetPosition(1, RegisteredControllers[1].transform.position);

                // set model position
                pos = RegisteredControllers[0].transform.position - (RegisteredControllers[0].transform.position - RegisteredControllers[1].transform.position) / 2;
                SelectionModelInstance.transform.position = pos;

                float connectionRayDistance = (RegisteredControllers[0].transform.position - RegisteredControllers[1].transform.position).sqrMagnitude;

                // resize based on connectionraydistance
                if (connectionRayDistance < MaxSphereDiagnoal && connectionRayDistance > MinSphereDiagonal)
                {
                    CurrentSphereDiagonal = connectionRayDistance - connectionRayDistance / DistanceDivider;
                    SelectionModelInstance.transform.localScale = new Vector3(CurrentSphereDiagonal, CurrentSphereDiagonal, CurrentSphereDiagonal);
                }

                // colliders
                hitColliders = Physics.OverlapSphere(SelectionModelInstance.transform.position, CurrentSphereDiagonal / 2);

                if (hitColliders.Length > 0)
                {
                    foreach (var collider in hitColliders)
                    {
                        VRIL_Interactable tempObj = collider.transform.gameObject.GetComponent<VRIL_Interactable>();

                        if (tempObj != null
                            && tempObj.Selection_Selectable
                            && tempObj != SelectionModelInstance
                            && !SelectableObjects.Contains(tempObj))
                        {
                            SelectableObjects.Add(tempObj);
                        }
                    }
                }
                else
                {
                    SelectableObjects.Clear();
                }

                yield return new WaitForSeconds(CoroutineWaitTime);
            }

            SelectionModelInstance?.SetActive(false);

            yield return null;
        }
    }
}