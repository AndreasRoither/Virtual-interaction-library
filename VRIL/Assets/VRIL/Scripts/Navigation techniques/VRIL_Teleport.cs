using System.Collections;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Interactable;
using VRIL.Manager;
using System;
using System.Collections.Generic;

namespace VRIL.NavigationTechniques
{
    public class VRIL_Teleport : VRIL_NavigationTechniqueBase
    {
        protected LineRenderer TeleportLineRenderer;
        protected GameObject TeleportLineRendererObject;
        
        [Header("General Settings")]
        [Tooltip("Set maximum travel distance")]
        public float MaxTravelDistance = 40;

        [Header("Ray Settings")]
        [Range(0.01f, 1f)]
        public float StartRayWidth = 0.01f;
        [Range(0.01f, 1f)]
        public float EndRayWidth = 0.01f;
        public Color ValidPositionColor = Color.green;
        public Color InvalidPositionColor = Color.red;
        [Tooltip("Required or the laser color might be something different")]
        public Material LaserMaterial;
        [Tooltip("The assigned material can cast shadows if its not set to transparent")]
        public bool CastShadows = false;
        [Tooltip("Set how many points are used for the curve")]
        public int NumberOfPoints = 20;

        [Header("Selection Point Settings")]
        [Tooltip("Visualisation object")]
        public GameObject point;
        
        private float MaxRayDistance;
        protected bool IsActivated = false;
        protected float WaitTime = 0.5f;
        protected float Timer = 0.0f;

        protected Transform TransformToMove;

        /// <summary>
        /// Initialize technique
        /// </summary>
        public virtual void Awake()
        {
            base.Initialize();
            TeleportLineRendererObject = new GameObject("VRIL_Teleport_LineRenderer");
            TeleportLineRendererObject.AddComponent<LineRenderer>();
            TeleportLineRenderer = TeleportLineRendererObject.GetComponent<LineRenderer>();
            TeleportLineRenderer.startWidth = StartRayWidth;
            TeleportLineRenderer.endWidth = EndRayWidth;
            TeleportLineRenderer.startColor = ValidPositionColor;
            TeleportLineRenderer.endColor = ValidPositionColor;
            TeleportLineRenderer.material = LaserMaterial;

            if (CastShadows)
            {
                TeleportLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else
            {
                TeleportLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            MaxRayDistance = (float)Math.Sqrt(MaxTravelDistance * MaxTravelDistance + DistanceToGround * DistanceToGround);
            Renderer rend = point.GetComponent<Renderer>();
            rend.material.SetColor("_Color", ValidPositionColor);
            point.SetActive(false);
        }

        /// <summary>
        /// Technique is activated
        /// </summary>
        /// <param name="e"></param>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (RegisteredControllers.Count > 0)
            {
                // differentiate between ButtonStates
                if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Released)
                {
                    OnTravel(e);
                }
                else if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed)
                {
                    if (!IsActivated)
                    {
                        IsActivated = true;
                        StartCoroutine(ShowRay(e));
                    }
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_Teleport)}:</b>\n No controller registered");
            }
        }

        /// <summary>
        /// Called when the travel should be performed
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if (PositionSelected && Timer > WaitTime)
            {
                InitDistancesToViewpoint();
                Viewpoint.transform.position = SelectedPosition;
                UpdateObjects();
                PositionSelected = false;
                Timer = 0.0f;
            }
            point.SetActive(false);
            IsActivated = false;
        }

        protected virtual void Update()
        {
            Timer += Time.deltaTime;
        }

        /// <summary>
        /// Coroutine for vizualization of the teleport target selection
        /// </summary>
        /// <returns>WaitForSeconds()</returns>
        protected IEnumerator ShowRay(VRIL_ControllerActionEventArgs e)
        {
            TeleportLineRenderer.enabled = true;
            while (IsActivated)
            {
                Ray ray = new Ray(RegisteredControllers[0].transform.position, RegisteredControllers[0].transform.forward);
                RaycastHit raycastHit;

                float PositionDistance = MaxRayDistance;
                
                // check for any objects
                if (Physics.Raycast(ray, out raycastHit, MaxRayDistance))
                {
                    VRIL_Navigable navigable = raycastHit.transform.gameObject.GetComponent<VRIL_Navigable>();
                    
                    // navigable object found, set possible travel position
                    if (navigable != null)
                    {
                        PositionDistance = raycastHit.distance;
                        SelectedPosition = raycastHit.point;
                        PositionSelected = true;
                        if (point != null)
                        {
                            point.transform.position = SelectedPosition + new Vector3(0f, 0.0001f, 0f);
                        }
                        SelectedPosition += new Vector3(0, DistanceToGround, 0);

                        TeleportLineRenderer.startColor = ValidPositionColor;
                        TeleportLineRenderer.endColor = ValidPositionColor;
                    }
                    // any other object blocks
                    else
                    {
                        PositionSelected = false;
                        TeleportLineRenderer.startColor = InvalidPositionColor;
                        TeleportLineRenderer.endColor = InvalidPositionColor;
                    }

                }
                // not any object reachable
                else
                {
                    PositionSelected = false;
                    TeleportLineRenderer.startColor = InvalidPositionColor;
                    TeleportLineRenderer.endColor = InvalidPositionColor;
                }

                // actives the visualization for the target position
                if (PositionSelected)
                {
                    point.SetActive(true);
                }
                else
                {
                    point.SetActive(false);
                }

                // get bezier points p1 and p2
                Vector3 point1 = ray.GetPoint(PositionDistance / 5);
                Vector3 point2 = ray.GetPoint(PositionDistance * 2 / 3);

                // calcuate all points of curve
                TeleportLineRenderer.positionCount = NumberOfPoints;
                Vector3[] positions = new Vector3[NumberOfPoints];
                
                // first point is device position
                positions[0] = RegisteredControllers[0].transform.position;
                
                // next ones have to be calculated
                for (int i = 2; i <= NumberOfPoints; i++)
                {
                    float t = i / (float)NumberOfPoints;
                    Vector3 pos = CalculateBezierPoint(t, ray.origin, point1 + new Vector3(0, PositionDistance / 10, 0), point2 + new Vector3(0, PositionDistance / 8, 0), ray.GetPoint(PositionDistance));
                    positions[i - 1] = pos;
                }
                TeleportLineRenderer.SetPositions(positions);
                yield return null;
            }
            TeleportLineRenderer.enabled = false;
        }

        /// <summary>
        /// Calculates a point of a bezier curve
        /// </summary>
        protected Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            //TODO: ugly code!
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;
            return p;
        }
    }
}