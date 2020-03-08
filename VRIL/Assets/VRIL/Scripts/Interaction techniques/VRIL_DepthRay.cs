using System.Collections;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Interactable;

namespace VRIL.InteractionTechniques
{
    [RequireComponent(typeof(LineRenderer))]
    public class VRIL_DepthRay : VRIL_InteractionTechniqueBase
    {
        private LineRenderer SingleRayCastLineRenderer;
        private bool IsActivated = false;

        public float MaxRayDistance = 25;

        [Space]
        [Header("Selection")]
        [Tooltip("SelectionModel should not have a collider!")]
        public GameObject SelectionModel;
        private GameObject SelectionModelInstance = null;

        [Range(0.0f, 5.0f)]
        public float SphereDiagnoal = 1f;
        private float CurrentSphereDiagonal;

        [Range(0.0f, 25.0f)]
        public float SphereDistance = 5f;

        [Space]
        [Header("Coroutine")]
        [Range(0.0f, 0.1f)]
        [Tooltip("1 = 1 sec")]
        public float CoroutineWaitTime = 0.03f;

        [Space]
        [Header("Distance")]
        [Range(1.0f, 50.0f)]
        public float DistanceMapping = 1.0f;

        public void Awake()
        {
            base.Initialize();
            SingleRayCastLineRenderer = GetComponent<LineRenderer>();
        }

        public void Start()
        {
            if (SelectionModel?.GetComponent<Collider>())
            {
                Debug.LogError($"<b>{nameof(VRIL_DepthRay)}:</b>\n The SelectionModel is selectable! Remove any colliders from this gameobject.");
            }

            if (RegisteredControllers.Count <= 1)
            {
                Debug.LogError($"<b>{nameof(VRIL_DepthRay)}:</b>\n requires at least two controllers!");
            }
        }

        /// <summary>
        /// Activate the technique which starts the coroutine. If no controllers are active, an error will be shown.
        /// </summary>
        /// <param name="e">ControllerEvent</param>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (this.RegisteredControllers.Count > 1)
            {
                if (!IsActivated)
                {
                    IsActivated = true;
                    StartCoroutine(ShowRay());
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_DepthRay)}:</b>\n requires at least two controllers!");
            }
        }

        /// <summary>
        /// Select objects which are of type <see cref="VRIL_Interactable"/>
        /// </summary>
        /// <param name="e">ControllerEvent</param>
        public override void OnSelection(VRIL_ControllerActionEventArgs e)
        {
            // differentiate between ButtonStates
            if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Released)
            {
                base.OnRelease(e);
            }
            else if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed)
            {
                base.OnSelection(e);
            }
        }

        /// <summary>
        /// Stop technique and it's coroutine
        /// </summary>
        /// <param name="e">ControllerEvent</param>
        public override void OnStop(VRIL_ControllerActionEventArgs e)
        {
            StopCoroutine(ShowRay());
            IsActivated = false;
        }

        /// <summary>
        /// Unity specific, when drawing gizmos is activated
        /// Shows the sphere in which <see cref="VRIL_Interactable"/> objects can be selected
        /// </summary>
        public void OnDrawGizmos()
        {
            if (SelectionModelInstance != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(SelectionModelInstance.transform.position, CurrentSphereDiagonal / 2);
            }
        }

        /// <summary>
        /// Coroutine for Depthray
        /// <para>Controller is used with a selection model to select objects</para>
        /// </summary>
        /// <returns></returns>
        private IEnumerator ShowRay()
        {
            SingleRayCastLineRenderer.enabled = true;
            Ray ray = new Ray(RegisteredControllers[0].transform.position, RegisteredControllers[0].transform.forward);
            Collider[] hitColliders;

            if (SelectionModelInstance == null)
            {
                SelectionModelInstance = Instantiate(SelectionModel, ray.GetPoint(0), Quaternion.identity);
                SelectionModelInstance.name = "VRIL_SelectionModel_DepthRay";
                SelectionModelInstance.transform.localScale = new Vector3(SphereDiagnoal, SphereDiagnoal, SphereDiagnoal);
                CurrentSphereDiagonal = SphereDiagnoal;
            }
            else
            {
                SelectionModelInstance.SetActive(true);
            }

            while (IsActivated)
            {
                ray = new Ray(RegisteredControllers[0].transform.position, RegisteredControllers[0].transform.forward);
                SingleRayCastLineRenderer.SetPosition(0, ray.origin);
                SingleRayCastLineRenderer.SetPosition(1, ray.GetPoint(MaxRayDistance));

                float distance = (RegisteredControllers[0].transform.position - RegisteredControllers[1].transform.position).sqrMagnitude;
                float pointValue = distance * DistanceMapping;

                if (pointValue >= MaxRayDistance)
                    pointValue = MaxRayDistance;
                
                SelectionModelInstance.transform.position = ray.GetPoint(pointValue);

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

            SelectionModelInstance.SetActive(false);
            SingleRayCastLineRenderer.enabled = false;
        }
    }
}