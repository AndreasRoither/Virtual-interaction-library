using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using System.Linq;

namespace VRIL.NavigationTechniques
{
    public class VRIL_WIM : VRIL_NavigationTechniqueBase
    {
        private const float HALF_CIRCLE = 180.0f; // degrees

        /*
         * public properties
         */

        [Header("WIM Settings")]
        [Tooltip("Flying into the miniature for viewpoint translation")]
        public bool FlyingIntoTheMiniature = false;

        [Tooltip("Player representation in WIM (real world size)")]
        public GameObject Doll;
        public GameObject ShadowDoll;

        [Tooltip("Factor to scale down the world")]
        public float ScaleFactor = 0.001f;

        [Tooltip("Refresh WIM on render")]
        public bool RefreshWIM = true;

        [Tooltip("Min sizes of object relevant for WIM")]
        public float TresholdX = 1.0f;
        public float TresholdY = 1.0f;
        public float TresholdZ = 1.0f;
        [Tooltip("Velocities for flying into the miniature")]
        public float ViewpointVelocity = 0.5f;
        public float ScaleVelocity = 0.001f;

        [Tooltip("Distance of controller to WIM")]
        public float DistanceControllerToWIM = 0.005f;

        [Header("Ray Settings")]
        public float MaxRayDistance = 1.0f;
        public Color ValidPositionColor = Color.green;
        public Color InvalidPositionColor = Color.red;
        [Tooltip("Required or the laser color might be something different")]
        public Material LaserMaterial;
        [Tooltip("The assigned material can cast shadows if its not set to transparent")]
        public bool CastShadows = false;
        public float StartRayWidth = 0.005f;
        [Range(0.01f, 1f)]
        public float EndRayWidth = 0.005f;

        [Header("Selection Point Settings")]
        [Tooltip("Visualisation object")]
        public GameObject HitEntity;
        [Tooltip("Distance of hit entity object to ground")]
        public float DistanceHitEntityToGround = 0.005f;
        [Tooltip("Sets the maximum allowed angle for a surface")]
        public float MaximumSurfaceSkewness = 20.0f;

        //public Camera Camera;
        private Vector3 prevCameraRotationEuler;

        /*
         * private or protected members
         */

        private Camera viewpointCamera;

        private Vector3? prevSelectedPosition;

        private bool IsActivated;

        private GameObject WIMHand;
        private GameObject RayHand;
        protected LineRenderer WIMLineRenderer;
        protected GameObject WIMLineRendererObject;

        // current doll instance
        private GameObject currentDoll;

        // shadow doll to visualize new position
        private GameObject dollClone;

        // save current position rotation and scale of WIM
        private Vector3 CurrentWIMPosition;
        private Quaternion CurrentWIMRotation;
        private Vector3 CurrentScale;

        private bool TravelMode = false;

        // wim object and init pos and rotation
        private GameObject Wim;
        private Vector3 origPos;
        private Quaternion origRot;

        // save temporary all clone objects in a list
        private IList<MeshRenderer> clones;
        private float currentVelocity;

        // necessary for doll ghost rotation
        private Vector3 prevControllerRotation;
        private float diff = 0.0f;

        // necessary for viewpoint
        private Quaternion prevViewpointRotation;
        private Quaternion prevWorldRotation;

        // init controller z euler rotation
        private float rayHandZInit;

        private IDictionary<int, MeshRenderer> mappingCloneIdsToOriginals;

        public void Awake()
        {
            base.Initialize();
            if (Doll)
            {
                Doll.SetActive(false);
            }
            if (ShadowDoll)
            {
                ShadowDoll.SetActive(false);
            }
            WIMLineRendererObject = new GameObject("VRIL_WIM_LineRenderer");
            WIMLineRendererObject.AddComponent<LineRenderer>();
            WIMLineRenderer = WIMLineRendererObject.GetComponent<LineRenderer>();
            WIMLineRenderer.startWidth = StartRayWidth;
            WIMLineRenderer.endWidth = EndRayWidth;
            WIMLineRenderer.startColor = ValidPositionColor;
            WIMLineRenderer.endColor = ValidPositionColor;
            WIMLineRenderer.material = LaserMaterial;
            WIMLineRenderer.enabled = false;
            if (CastShadows)
            {
                WIMLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            else
            {
                WIMLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // get camera object
            if (Viewpoint.GetComponent<Camera>())
            {
                viewpointCamera = Viewpoint.GetComponent<Camera>();
            }
            else
            {
                viewpointCamera = Viewpoint.GetComponentsInChildren<Camera>().FirstOrDefault();
            }
        }

        public void Start()
        {
            if (RegisteredControllers.Count > 1)
            {
                WIMHand = RegisteredControllers[1];
                RayHand = RegisteredControllers[0];
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_WIM)}:</b>\n This technique requires at least two controllers!");
            }
            if (HitEntity == null)
            {
                Debug.LogError("No hit entity object for WIM!");
            }
            else
            {
                HitEntity.SetActive(false);
                Renderer rend = HitEntity.transform.gameObject.GetComponent<Renderer>();
                rend.material.SetColor("_Color", ValidPositionColor);
            }
            if(Doll == null)
            {
                Debug.Log("Orientation changes not possible for WIM technique because no doll was set.");
            }
        }

        /// <summary>
        /// When the technique is activated
        /// </summary>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            // only ray hand triggers WIM
            if (e.ControllerIndex != 1)
            {
                return;
            }
            if (RegisteredControllers.Count > 1)
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
                        SelectedPosition = Viewpoint.transform.position;
                        currentVelocity = ViewpointVelocity;
                        CurrentScale = new Vector3(1.0f, 1.0f, 1.0f);
                        CurrentScale *= ScaleFactor;
                        CreateWim();
                        IsActivated = true;
                        rayHandZInit = RayHand.transform.localEulerAngles.z;
                        StartCoroutine(WIM());
                    }
                }
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_WIM)}:</b>\n This technique requires at least two controllers!");
            }
        }

        /// <summary>
        /// Creates the world in miniature with player doll on viewpoint position
        /// </summary>
        private void CreateWim()
        {
            // create WIM as new object
            Wim = new GameObject("WIM");
            Wim.AddComponent<VRIL_WIMObject>();
            Wim.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            Wim.transform.position = new Vector3(0, 0, 0);
            if (!TravelMode)
            {
                HitEntity.SetActive(false);
            }

            // clone all relevant MeshRenderer objects which have a collider
            MeshRenderer[] allObjects = FindObjectsOfType<MeshRenderer>().Where(
                m => m.gameObject != null &&
                m.gameObject.activeSelf &&
                m.GetComponent<Collider>() != null &&
                Array.IndexOf(RegisteredControllers.ToArray(), m.gameObject) < 0 && (m.gameObject.GetInstanceID() != HitEntity.gameObject.GetInstanceID()) &&
                (m.bounds.size.x >= TresholdX || m.bounds.size.y >= TresholdY || m.bounds.size.z >= TresholdZ)).ToArray();

            if (RefreshWIM)
            {
                mappingCloneIdsToOriginals = new Dictionary<int, MeshRenderer>();
            }

            // fill clone list
            clones = new List<MeshRenderer>();
            foreach (MeshRenderer obj in allObjects)
            {
                MeshRenderer objClone = Instantiate<MeshRenderer>(obj);
                objClone.tag = "WIMclone";
                objClone.name = "WIM" + obj.gameObject.name;
                objClone.transform.SetParent(Wim.transform, false);
                objClone.transform.position = obj.transform.position;
                objClone.gameObject.AddComponent<VRIL_WIMObject>();

                //if (obj.GetComponent<MeshCollider>() != null)
                //{
                //    MeshCollider mesh = objClone.GetComponent<MeshCollider>();
                //    mesh.convex = true;
                //    mesh.isTrigger = true;
                //    objClone.AddComponent<BoxCollider>();
                //}
                //else
                //{
                //    Collider col = obj.GetComponent<Collider>();
                //    col.isTrigger = true;
                //}

                // set kinematic for all WIM objects (removes physics)
                if (objClone.GetComponent<Rigidbody>() != null)
                {
                    objClone.GetComponent<Rigidbody>().isKinematic = true;
                }
                objClone.gameObject.SetActive(true);

                if (RefreshWIM)
                {
                    mappingCloneIdsToOriginals[objClone.GetInstanceID()] = obj;
                }

                clones.Add(objClone);
            }

            // add figure to represent current position in WIM
            if (Doll)
            {
                currentDoll = Instantiate(Doll);
                currentDoll.SetActive(true);
                currentDoll.transform.position = SelectedPosition;
                currentDoll.transform.localPosition += new Vector3(0, Doll.transform.position.y, 0);
                
                currentDoll.transform.forward = new Vector3(Viewpoint.transform.forward.x, 0, Viewpoint.transform.forward.z);
                currentDoll.AddComponent<VRIL_WIMObject>();
                currentDoll.transform.eulerAngles = new Vector3(currentDoll.transform.rotation.eulerAngles.x, currentDoll.transform.rotation.eulerAngles.y, currentDoll.transform.rotation.eulerAngles.z);
                currentDoll.transform.parent = Wim.transform;
                dollClone = Instantiate(ShadowDoll != null ? ShadowDoll : currentDoll);
                dollClone.transform.parent = Wim.transform;
                if (viewpointCamera)
                {
                    prevCameraRotationEuler = viewpointCamera.transform.localEulerAngles;
                }
            }

            // attach selection point
            HitEntity.transform.parent = Wim.transform;

            Vector3 before = HitEntity.transform.position;

            origPos = Wim.transform.position;
            origRot = Wim.transform.rotation;

            // downscale the world
            Wim.transform.localScale = CurrentScale;

            // init prev controller rotation
            prevControllerRotation = RayHand.transform.localEulerAngles;
            prevWorldRotation = Wim.transform.rotation;
        }

        /// <summary>
        /// Draws the WIM
        /// </summary>
        private void DrawWIM()
        {
            // recreate always in cases world changes should be applied to WIM
            if (RefreshWIM)
            {
                foreach (MeshRenderer clone in clones)
                {
                    MeshRenderer m = mappingCloneIdsToOriginals[clone.GetInstanceID()];
                    clone.transform.localPosition = m.transform.position;
                    clone.transform.localRotation = m.transform.rotation;
                }
            }

            // no travel mode: refresh WIM position and rotation according to controller
            if (!TravelMode)
            {
                Ray ray = new Ray(WIMHand.transform.position, WIMHand.transform.up);
                CurrentWIMPosition = ray.GetPoint(DistanceControllerToWIM);
                CurrentWIMRotation = WIMHand.transform.rotation;
                Wim.transform.position = CurrentWIMPosition;
                Wim.transform.rotation = CurrentWIMRotation;
                if (currentDoll && viewpointCamera)
                {
                    float rotationDiffY = viewpointCamera.transform.localEulerAngles.y - prevCameraRotationEuler.y;
                    currentDoll.transform.RotateAround(currentDoll.transform.position, currentDoll.transform.up, rotationDiffY);
                    prevCameraRotationEuler = viewpointCamera.transform.localEulerAngles;
                }
                //prevViewpointRotation = Viewpoint.transform.rotation;
            }

            // flight into the miniature: refresh the scale of the world
            else
            {
                Vector3 hitEntityPosBeforeScale = HitEntity.transform.position;
                Wim.transform.localScale = CurrentScale;

                // adapt world position: Selected position is always in center!
                HitEntity.transform.parent = null;
                Wim.transform.parent = HitEntity.transform;
                HitEntity.transform.position = hitEntityPosBeforeScale;
                Wim.transform.parent = null;
                HitEntity.transform.parent = Wim.transform;
            }
        }


        /// <summary>
        /// Coroutine for WIM technique
        /// </summary>
        protected IEnumerator WIM()
        {
            while (IsActivated && !TravelMode)
            {
                DrawWIM();
                yield return new WaitForEndOfFrame();
                HitEntity.SetActive(false);
                if (Doll)
                {
                    dollClone.SetActive(false);
                }

                PositionSelected = false;
                WIMLineRenderer.enabled = false;

                Ray selectionRay = new Ray(RayHand.transform.position, RayHand.transform.forward);

                if (Physics.Raycast(selectionRay, out RaycastHit raycastHit, MaxRayDistance) && raycastHit.transform.CompareTag("WIMclone"))
                {
                    VRIL_WIMObject wimSpace = raycastHit.transform.gameObject.GetComponent<VRIL_WIMObject>();

                    // ray hit with WIM and surface angle is small enough
                    if (wimSpace != null && wimSpace.GetComponent<VRIL_Navigable>() != null && Vector3.Angle(raycastHit.normal, Wim.transform.up) <= MaximumSurfaceSkewness)
                    {
                        WIMLineRenderer.enabled = true;
                        WIMLineRenderer.positionCount = 2;
                        WIMLineRenderer.SetPosition(0, selectionRay.origin);
                        WIMLineRenderer.SetPosition(1, raycastHit.point);
                        WIMLineRenderer.startColor = ValidPositionColor;
                        WIMLineRenderer.endColor = ValidPositionColor;
                        HitEntity.SetActive(true);
                        HitEntity.transform.position = raycastHit.point;
                        //HitEntity.transform.localPosition -= new Vector3(0, 1, 0);
                        PositionSelected = true;

                        // adapt doll clone orientation
                        if (dollClone)
                        {
                            dollClone.transform.position = raycastHit.point;
                            dollClone.transform.localPosition += new Vector3(0, ShadowDoll ? ShadowDoll.transform.position.y : Doll.transform.position.y, 0);
                            float diffZ = RayHand.transform.localEulerAngles.z - prevControllerRotation.z;
                            prevControllerRotation = RayHand.transform.localEulerAngles;
                            dollClone.transform.RotateAround(dollClone.transform.position, dollClone.transform.up, -diffZ);
                            dollClone.SetActive(true);
                        }
                    }
                    else
                    {
                        WIMLineRenderer.enabled = true;
                        WIMLineRenderer.positionCount = 2;
                        WIMLineRenderer.SetPosition(0, selectionRay.origin);
                        WIMLineRenderer.SetPosition(1, raycastHit.point);
                        WIMLineRenderer.startColor = InvalidPositionColor;
                        WIMLineRenderer.endColor = InvalidPositionColor;
                        HitEntity.SetActive(false);
                        if (dollClone)
                        {
                            dollClone.SetActive(false);
                        }
                    }
                }
                else
                {
                    WIMLineRenderer.enabled = false;
                }
                //foreach (MeshRenderer objClone in clones)
                //{
                //    objClone.gameObject.SetActive(true);
                //}
                yield return null;
            }
            WIMLineRenderer.enabled = false;
        }

        /// <summary>
        /// Travel - might trigger flight into miniature world
        /// </summary>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if (PositionSelected)
            {
                PlayAudio();
                InitDistancesToViewpoint();

                // differences of doll angles
                if (Doll)
                {
                    diff = RayHand.transform.localEulerAngles.z - rayHandZInit;
                }
                if (FlyingIntoTheMiniature)
                {
                    StartCoroutine(FlyingIntoMiniature());
                }
                else
                {
                    Finish();
                }
            }
            else
            {
                Finish();
            }
        }

        /// <summary>
        /// Coroutine for the flight into the miniature
        /// </summary>
        protected IEnumerator FlyingIntoMiniature()
        {
            // prepare flight
            prevViewpointRotation = Viewpoint.transform.rotation;
            TravelMode = true;
            float viewpointRotation = 0.0f;
            if (dollClone)
            {
                // calculation of rotation during flight: angle between the two objects * ray length * 2 (constant factor which was the best at some tests)
                viewpointRotation = 2 * Vector3.Angle(viewpointCamera.transform.eulerAngles, dollClone.transform.eulerAngles) 
                    * Vector3.Distance(RayHand.transform.position, dollClone.transform.position) * ScaleVelocity;
                currentDoll.SetActive(false);
            }

            Manager.InputLocked = true;

            // start animation
            while (TravelMode)
            {
                DrawWIM();
                if (CurrentScale.x < 1.0f)
                {
                    CurrentScale *= (1 + ScaleVelocity);
                }
                if (dollClone)
                {
                    float step = viewpointRotation * Time.deltaTime;
                    Viewpoint.transform.rotation = Quaternion.RotateTowards(Viewpoint.transform.rotation, dollClone.transform.rotation, step);
                }
                Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position, HitEntity.transform.position, currentVelocity * Time.deltaTime);
                if (CurrentScale.x >= 1.0f)
                {
                    TravelMode = false;
                    Viewpoint.transform.rotation = prevViewpointRotation;
                    Finish();
                }
                yield return null;
            }
        }

        private void Finish()
        {
            if (Wim)
            {
                Wim.transform.position = origPos;
                Wim.transform.rotation = origRot;
                Wim.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                if (PositionSelected)
                {
                    SelectedPosition = HitEntity.transform.position + new Vector3(0, DistanceToGround, 0);
                    Viewpoint.transform.position = SelectedPosition;

                    if (Doll)
                    {
                        Viewpoint.transform.rotation = dollClone.transform.localRotation;
                    }
                    UpdateObjects();
                    PositionSelected = false;
                }
                Manager.InputLocked = false;
                IsActivated = false;
                if (HitEntity != null)
                {
                    HitEntity.SetActive(false);
                }
                HitEntity.transform.parent = null;

                // destroy WIM after
                DestroyImmediate(Wim);
            }
        }
    }
}
