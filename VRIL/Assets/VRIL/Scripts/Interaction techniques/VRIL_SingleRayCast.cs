using System.Collections;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Interactable;

namespace VRIL.InteractionTechniques
{
    [RequireComponent(typeof(LineRenderer))]
    public class VRIL_SingleRayCast : VRIL_InteractionTechniqueBase
    {
        private LineRenderer SingleRayCastLineRenderer;
        public float MaxRayDistance = 25;

        private bool IsActivated = false;

        /// <summary>
        /// Initialize technique
        /// </summary>
        public void Awake()
        {
            base.Initialize();
            SingleRayCastLineRenderer = GetComponent<LineRenderer>();
        }

        /// <summary>
        /// Technique is activated
        /// </summary>
        /// <param name="e"></param>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (RegisteredControllers.Count > 0)
            {
                if (!IsActivated)
                {
                    IsActivated = true;
                    StartCoroutine(ShowRay());
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_SingleRayCast)}:</b>\n No controller registered");
            }
        }

        /// <summary>
        /// Select objects only if a button is pressed
        /// </summary>
        /// <param name="e"></param>
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
        /// Stop interaction technique and coroutine
        /// </summary>
        /// <param name="e"></param>
        public override void OnStop(VRIL_ControllerActionEventArgs e)
        {
            IsActivated = false;
        }

        /// <summary>
        /// Coroutine for single ray cast technique
        /// </summary>
        /// <returns>WaitForSeconds()</returns>
        private IEnumerator ShowRay()
        {
            SingleRayCastLineRenderer.enabled = true;

            while (IsActivated)
            {
                Ray ray = new Ray(RegisteredControllers[0].transform.position, RegisteredControllers[0].transform.forward);
                RaycastHit raycastHit;

                SingleRayCastLineRenderer.SetPosition(0, ray.origin);

                // check for obstacle at max distance
                if (Physics.Raycast(ray, out raycastHit, MaxRayDistance))
                {
                    VRIL_Interactable interactable = raycastHit.transform.gameObject.GetComponent<VRIL_Interactable>();
                    if (interactable != null && interactable.Selection_Selectable)
                    {
                        if (!SelectableObjects.Contains(interactable))
                            SelectableObjects.Add(interactable);
                    }
                    else
                    {
                        SelectableObjects.Clear();
                    }

                    SingleRayCastLineRenderer.SetPosition(1, raycastHit.point);
                }
                else
                {
                    SelectableObjects.Clear();
                    SingleRayCastLineRenderer.SetPosition(1, ray.GetPoint(MaxRayDistance));
                }

                yield return null;
            }

            SingleRayCastLineRenderer.enabled = false;
        }
    }
}