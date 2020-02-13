using System.Collections;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Interactable;
using VRIL.Manager;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VRIL.NavigationTechniques
{

    public class VRIL_Teleport : VRIL_NavigationTechniqueBase
    {
        protected LineRenderer TeleportLineRenderer;
        protected GameObject TeleportLineRendererObject;

        [Header("Technique settings")]
        [Tooltip("Time to next teleport")]
        public float TimeToWaitForNextTeleport = 0.5f;
        [Tooltip("Angle in degrees: 0 = Horizontal surfaces, 90 = Vertical surfaces")]
        [Range(0.0f, 90.0f)]
        public float MaximumSurfaceSkewness = 0;

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
        [Tooltip("Set how many points for no target selected")]
        public int NumberOfPointsNoTarget = 3;
        [Tooltip("Velocity to take for trajectory calculation")]
        public float CurveVelocity = 6f;
        [Header("Selection Point Settings")]
        [Tooltip("Visualisation object")]
        public GameObject HitEntity;
        [Tooltip("Distance of hit entity object to ground")]
        public float DistanceHitEntityToGround = 0.005f;

        protected bool IsActivated = false;
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
            if(HitEntity != null)
            {
                Renderer rend = HitEntity.GetComponent<Renderer>();
                rend.material.SetColor("_Color", ValidPositionColor);
                HitEntity.SetActive(false);
            }
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
            if (PositionSelected && Timer > TimeToWaitForNextTeleport)
            {
                PlayAudio();
                InitDistancesToViewpoint();
                Viewpoint.transform.position = SelectedPosition;
                UpdateObjects();
                PositionSelected = false;
                Timer = 0.0f;
            }
            if(HitEntity != null)
            {
                HitEntity.SetActive(false);
            }
            IsActivated = false;
        }

        protected virtual void Update()
        {
            Timer += Time.deltaTime;
        }

        /// <summary>
        /// Gets a point of the projectile shot curve
        /// Formula see https://aframe.io/blog/teleport-component/
        /// </summary>
        private float CalcPoint(float p0, float v0, float a, float t)
        {
            return p0 + v0 * t + 0.5f * a * t * t;
        }

        /// <summary>
        /// Gets the point vector (calculated for each axis)
        /// </summary>
        private Vector3 GetTrajectoryVector(Vector3 p0, Vector3 velocity, Vector3 acc, float t)
        {
            return new Vector3(CalcPoint(p0.x, velocity.x, acc.x, t), CalcPoint(p0.y, velocity.y, acc.y, t), CalcPoint(p0.z, velocity.z, acc.z, t));
        }

        /// <summary>
        /// Parabolic motion derivative
        /// </summary>
        private static Vector3 ParabolicCurveDeriv(Vector3 v0, Vector3 a, float t)
        {
            return new Vector3(v0.x + a.x * t, v0.y + a.y * t, v0.z + a.z * t);
        }

        /// <summary>
        /// Coroutine for vizualization of the target selection
        /// </summary>
        /// <returns>WaitForSeconds()</returns>
        protected IEnumerator ShowRay(VRIL_ControllerActionEventArgs e)
        {
            TeleportLineRenderer.enabled = true;
            while (IsActivated)
            {
                if(HitEntity != null)
                {
                    HitEntity.SetActive(false);
                }
                TeleportLineRenderer.startColor = InvalidPositionColor;
                TeleportLineRenderer.endColor = InvalidPositionColor;

                // first point of curve is at controller
                Vector3 p0 = RegisteredControllers[0].transform.position;
                List<Vector3> positions = new List<Vector3>() { p0 };

                // save last point
                Vector3 lastPoint = p0;
                bool hit = false;
                Vector3 InitialVelocity = RegisteredControllers[0].transform.forward * CurveVelocity;
                Vector3 velocity = transform.TransformDirection(InitialVelocity);
                float t = 0;

                // calculate all points
                for (int i = 1; i <= NumberOfPoints && !hit; i++)
                {
                    t += 0.3f / ParabolicCurveDeriv(velocity, Physics.gravity, t).magnitude;
                    Vector3 curPoint = GetTrajectoryVector(p0, velocity, Physics.gravity, t);
                    //positions.Add(curPoint);
                    Vector3 diff = curPoint - lastPoint;
                    Ray ray = new Ray(lastPoint, diff.normalized);

                    // check next trajectory part for object collision
                    if (Physics.Raycast(ray, out RaycastHit raycastHit, diff.magnitude))
                    {
                        VRIL_Navigable navigableObject = raycastHit.transform.gameObject.GetComponent<VRIL_Navigable>();

                        // valid position in case it is navigable and ray hits even surface
                        if(navigableObject && Vector3.Angle(raycastHit.normal, new Vector3(0, 1, 0)) <= MaximumSurfaceSkewness)
                        {
                            SelectedPosition = raycastHit.point;
                            PositionSelected = true;
                            if (HitEntity != null)
                            {
                                HitEntity.transform.position = SelectedPosition + new Vector3(0f, DistanceHitEntityToGround, 0f);
                                HitEntity.SetActive(true);
                            }
                            SelectedPosition += new Vector3(0, DistanceToGround, 0);
                            TeleportLineRenderer.startColor = ValidPositionColor;
                            TeleportLineRenderer.endColor = ValidPositionColor;
                        }
                        else
                        {
                            PositionSelected = false;
                        }
                        positions.Add(raycastHit.point);
                        hit = true;
                    }
                    else
                    {
                        positions.Add(curPoint);
                    }
                    lastPoint = curPoint;
                }
                if(!hit)
                {
                    if(HitEntity != null)
                    {
                        HitEntity.SetActive(false);
                    }
                    PositionSelected = false;
                }
                // set line renderer
                TeleportLineRenderer.positionCount = hit ? positions.Count : NumberOfPointsNoTarget;
                TeleportLineRenderer.SetPositions(hit ? positions.ToArray() : positions.Take(NumberOfPointsNoTarget).ToArray());
                yield return null;
            }
            TeleportLineRenderer.enabled = false;
        }
    }
}