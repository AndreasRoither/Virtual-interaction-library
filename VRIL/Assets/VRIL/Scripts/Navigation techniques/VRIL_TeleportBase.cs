using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;

namespace VRIL.NavigationTechniques
{
    /// <summary>
    /// Abstract base class for teleport techniques
    /// </summary>
    public abstract class VRIL_TeleportBase : VRIL_NavigationTechniqueBase
    {
        // *************************************
        // public properties
        // *************************************

        [Header("Teleport settings")] [Tooltip("Time in seconds to unlock next teleport (avoids too many teleports)")]
        public float DelayToNextActivation = 0.5f;

        [Tooltip("Travel task triggers selection mode again.")]
        public bool TravelDisablesTechnique = true;

        [Tooltip(
            "Sets the maximum allowed angle for a navigable WIM object surface (0° = positions only on horizontal surfaces are allowed, 90° = all positions are allowed)")]
        [Range(0.0f, 90.0f)]
        public float MaximumSurfaceAngle = 0;

        [Header("Ray Settings")] [Range(0.01f, 1f)]
        public float StartRayWidth = 0.01f;

        [Range(0.01f, 1f)] public float EndRayWidth = 0.01f;
        public Color ValidPositionColor = Color.green;
        public Color InvalidPositionColor = Color.red;

        [Tooltip("Required or the laser color might be something different")]
        public Material LaserMaterial;

        [Tooltip("The assigned material can cast shadows if its not set to transparent")]
        public bool CastShadows = false;

        [Tooltip("Set how many line fragments are used for the curve")]
        public int NumberOfRayFragments = 20;

        [Tooltip("Set how many line fragments are used target is out of range")]
        public int NumberOfRayFragmentsNoObject = 3;

        [Tooltip("Velocity to take for trajectory calculation (base is thrown objects curve)")]
        public float CurveVelocity = 6f;

        [Header("Selection Point Settings")] [Tooltip("Visualisation object")]
        public GameObject HitEntity;

        [Tooltip("Distance of hit entity object to ground")]
        public float DistanceHitEntityToGround = 0.005f;


        // *************************************
        // constants
        // *************************************

        private const string NAME_LINE_RENDERER = "VRIL_Teleport_LineRenderer";


        // *************************************
        // protected members
        // *************************************

        protected bool IsActivated = false;
        protected float Timer = 0.0f;
        protected Transform TransformToMove;
        protected Camera Camera;
        protected LineRenderer TeleportLineRenderer;
        protected GameObject TeleportLineRendererObject;
        protected bool DelayToNextTravel = false;

        /// <summary>
        /// Initialize technique
        /// </summary>
        public virtual void Awake()
        {
            Initialize();
            TeleportLineRendererObject = new GameObject(NAME_LINE_RENDERER);
            TeleportLineRendererObject.AddComponent<LineRenderer>();
            TeleportLineRenderer = TeleportLineRendererObject.GetComponent<LineRenderer>();
            TeleportLineRenderer.startWidth = StartRayWidth;
            TeleportLineRenderer.endWidth = EndRayWidth;
            TeleportLineRenderer.startColor = ValidPositionColor;
            TeleportLineRenderer.endColor = ValidPositionColor;
            TeleportLineRenderer.material = LaserMaterial;

            if (CastShadows)
            {
                TeleportLineRenderer.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                TeleportLineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            if (HitEntity != null)
            {
                Renderer rend = HitEntity.GetComponent<Renderer>();
                if (rend)
                {
                    rend.material.SetColor("_Color", ValidPositionColor);
                }

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
                if (!TravelDisablesTechnique)
                {
                    if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed)
                    {
                        if (IsActivated)
                        {
                            IsActivated = false;
                            PositionSelected = false;
                            if (HitEntity != null)
                            {
                                HitEntity.SetActive(false);
                            }
                        }
                        else
                        {
                            IsActivated = true;
                            StartCoroutine(SelectPosition(e));
                        }
                    }
                }
                else
                {
                    // differentiate between ButtonStates
                    if (TravelOnRelease && e.ButtonInteractionType == VRIL_ButtonInteractionType.Released)
                    {
                        OnTravel(e);
                    }
                    else if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed)
                    {
                        if (!IsActivated)
                        {
                            IsActivated = true;
                            StartCoroutine(SelectPosition(e));
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_BlinkTeleport)}:</b>\n No controller registered");
            }
        }

        protected virtual void Update()
        {
            if (DelayToNextTravel)
            {
                Timer += Time.deltaTime;
                if (Timer >= DelayToNextActivation)
                {
                    DelayToNextTravel = false;
                    Timer = 0.0f;
                }
            }
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
            return new Vector3(CalcPoint(p0.x, velocity.x, acc.x, t), CalcPoint(p0.y, velocity.y, acc.y, t),
                CalcPoint(p0.z, velocity.z, acc.z, t));
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
        /// It is "virtual" to allow custom ray implementation in future (such as curved trajectories or just a straight line)
        /// </summary>
        /// <returns>WaitForSeconds()</returns>
        protected virtual IEnumerator SelectPosition(VRIL_ControllerActionEventArgs e)
        {
            TeleportLineRenderer.enabled = true;
            while (IsActivated)
            {
                // disable hit entity to avoid ray cast blocking
                if (HitEntity != null)
                {
                    HitEntity.SetActive(false);
                }

                TeleportLineRenderer.startColor = InvalidPositionColor;
                TeleportLineRenderer.endColor = InvalidPositionColor;

                // first point of curve is at controller
                Vector3 p0 = RegisteredControllers[0].transform.position;
                List<Vector3> positions = new List<Vector3>() {p0};

                // save last point
                Vector3 lastPoint = p0;
                bool hit = false;
                Vector3 InitialVelocity = RegisteredControllers[0].transform.forward * CurveVelocity;
                Vector3 velocity = transform.TransformDirection(InitialVelocity);
                float t = 0;

                // calculate all points
                for (int i = 1; i <= NumberOfRayFragments && !hit; i++)
                {
                    t += 0.3f / ParabolicCurveDeriv(velocity, Physics.gravity, t).magnitude;
                    Vector3 curPoint = GetTrajectoryVector(p0, velocity, Physics.gravity, t);
                    Vector3 diff = curPoint - lastPoint;
                    Ray ray = new Ray(lastPoint, diff.normalized);

                    // check next trajectory part for object collision
                    if (Physics.Raycast(ray, out RaycastHit raycastHit, diff.magnitude))
                    {
                        VRIL_Navigable navigableObject = raycastHit.transform.gameObject.GetComponent<VRIL_Navigable>();

                        // valid position in case it is navigable and angle is allowed
                        if (navigableObject && Vector3.Angle(raycastHit.normal, Vector3.up) <= MaximumSurfaceAngle)
                        {
                            TargetPosition = raycastHit.point;
                            PositionSelected = true;
                            if (HitEntity != null)
                            {
                                HitEntity.transform.position =
                                    TargetPosition + new Vector3(0f, DistanceHitEntityToGround, 0f);
                                HitEntity.SetActive(true);
                            }

                            TargetPosition += new Vector3(0, DistanceViewpointToGround, 0);
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

                if (!hit)
                {
                    if (HitEntity != null)
                    {
                        HitEntity.SetActive(false);
                    }

                    PositionSelected = false;
                }

                // set line renderer
                TeleportLineRenderer.positionCount = hit ? positions.Count : NumberOfRayFragmentsNoObject;
                TeleportLineRenderer.SetPositions(hit
                    ? positions.ToArray()
                    : positions.Take(NumberOfRayFragmentsNoObject).ToArray());
                yield return null;
            }

            TeleportLineRenderer.enabled = false;
        }
    }
}