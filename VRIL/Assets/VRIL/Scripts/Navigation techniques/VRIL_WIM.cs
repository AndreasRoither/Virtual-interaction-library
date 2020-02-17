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

        /*
         * public properties
         */

        [Header("WIM Settings")]
        [Tooltip("True: Refresh WIM always. Position changes of objects in the large-scaled world are applied to WIM")]
        public bool RefreshWIM = true;
        [Tooltip("Sets the maximum allowed angle for a navigable WIM object surface (0° = positions only on horizontal surfaces are allowed, 90° = all positions are allowed)")]
        [Range(0.0f, 90.0f)]
        public float MaximumSurfaceSkewness = 20.0f;
        [Tooltip("Factor to scale down the world")]
        public float ScaleFactor = 0.001f;
        [Tooltip("Min object sizes for WIM, smaller objects will be not cloned")]
        public float TresholdX = 1.0f;
        public float TresholdY = 1.0f;
        public float TresholdZ = 1.0f;
        [Tooltip("Distance of controller to WIM")]
        public float DistanceControllerToWIM = 0.005f;
        public bool DisableCollidersInWIM = true;

        [Header("Flight Into the Miniature Settings")]
        [Tooltip("True: Fly into the new position in WIM (triggers an animation). False: Instant teleportation")]
        public bool UseFlightIntoTheMiniature = false;
        [Tooltip("Velocities for flying into the miniature")]
        public float ViewpointVelocity = 0.5f;
        public float ScaleVelocity = 0.001f;
        public float ViewPointRotationFactor = 3.0f;

        [Header("Doll Settings")]
        [Tooltip("A doll is an object that represents the player in the WIM. Usually, it is a human figure but also a simple arrow can be used. " +
            "The model needs to be in normal size (in larged-scaled world), it will be automatically downscaled with other objects. " +
            "Since the viewpoint orientation changes are based on the doll object, a WIM without any doll will not change players orientation.")]
        public GameObject Doll;
        [Tooltip("The shadow doll visualizes the new position and orientation of the viewpoint. It can be also null (a copy of the doll is used then).")]
        public GameObject ShadowDoll;
        [Tooltip("True: Shadow doll looks away from viewpoint. False: It is possible to change orientation of shadow doll by rotating ray hand controller around.")]
        public bool FixedShadowDoll = true;

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
        [Tooltip("An object that represents the selected position in the WIM. It is required.")]
        public GameObject HitEntity;
        [Tooltip("Distance of hit entity object to WIM ground.")]
        public float DistanceHitEntityToGround = 0.005f;


        /*
         * constants 
         */

        private const float FINAL_SCALE = 1.0f;
        private const float HALF_CIRCLE = 180.0f;
        private const string WIM_OBJECT_NAME = "WIM";
        private const string LINE_RENDERER = "VRIL_WIM_LineRenderer";

        /*
         * private or protected members
         */

        private Vector3 PrevCameraRotationEuler;
        private Camera ViewpointCamera;
        private bool IsActivated;
        private float CurrentVelocity;
        private GameObject WIMHand;
        private GameObject RayHand;
        protected LineRenderer WIMLineRenderer;
        protected GameObject WIMLineRendererObject;

        // current doll instance
        private GameObject CurrentDoll;

        // shadow doll to visualize new position
        private GameObject CurrentShadowDoll;

        // save current position rotation and scale of WIM
        private Vector3 CurrentWIMPosition;
        private Quaternion CurrentWIMRotation;
        private Vector3 CurrentScale;

        private bool TravelMode = false;

        // wim object and init pos and rotation
        private GameObject Wim;
        private Vector3 OrigPos;
        private Quaternion OrigRot;

        // save temporary all clone objects in a list
        private IList<MeshRenderer> Clones;

        // save original MeshRenderer for each cloned object instanceID (required for WIM refresh)
        private IDictionary<int, MeshRenderer> MappingCloneIdsToOriginals;

        // necessary for doll ghost rotation
        private Vector3 PrevControllerRotation;

        // necessary for viewpoint
        private Quaternion PrevViewpointRotation;
        

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
            WIMLineRendererObject = new GameObject(LINE_RENDERER);
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
                ViewpointCamera = Viewpoint.GetComponent<Camera>();
            }
            else
            {
                ViewpointCamera = Viewpoint.GetComponentsInChildren<Camera>().FirstOrDefault();
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
                        CurrentVelocity = ViewpointVelocity;
                        CurrentScale = new Vector3(FINAL_SCALE, FINAL_SCALE, FINAL_SCALE);
                        CurrentScale *= ScaleFactor;
                        CreateWim();
                        IsActivated = true;
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
        /// Find all objects that are relevant for WIM
        /// </summary>
        /// <returns></returns>
        private MeshRenderer[] FindMeshRenderers()
        {
            MeshRenderer[] allObjects = FindObjectsOfType<MeshRenderer>().Where(
            m => m.gameObject != null &&
            m.gameObject.activeSelf &&
            m.GetComponent<Collider>() != null &&
            Array.IndexOf(RegisteredControllers.ToArray(), m.gameObject) < 0 && (m.gameObject.GetInstanceID() != HitEntity.gameObject.GetInstanceID()) &&
            (m.bounds.size.x >= TresholdX || m.bounds.size.y >= TresholdY || m.bounds.size.z >= TresholdZ)).ToArray();
            return allObjects;
        }

        /// <summary>
        /// Clones a mesh renderer object with properties for WIM
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private MeshRenderer GetWIMClone(MeshRenderer obj)
        {
            MeshRenderer objClone = Instantiate(obj);
            objClone.name = WIM_OBJECT_NAME + obj.gameObject.name;

            // append object to WIM
            objClone.transform.SetParent(Wim.transform, false);

            objClone.transform.position = obj.transform.position;
            objClone.gameObject.AddComponent<VRIL_WIMObject>();

            // set kinematic true for all WIM objects
            if (objClone.GetComponent<Rigidbody>() != null)
            {
                objClone.GetComponent<Rigidbody>().isKinematic = true;
            }

            if (RefreshWIM)
            {
                MappingCloneIdsToOriginals[objClone.GetInstanceID()] = obj;
            }
            return objClone;
        }

        /// <summary>
        /// Creates the world in miniature with player doll on viewpoint position
        /// </summary>
        private void CreateWim()
        {
            // create WIM as new object
            Wim = new GameObject(WIM_OBJECT_NAME);
            Wim.AddComponent<VRIL_WIMObject>();
            Wim.transform.localScale = new Vector3(FINAL_SCALE, FINAL_SCALE, FINAL_SCALE);
            Wim.transform.position = new Vector3(0, 0, 0);
            if (!TravelMode)
            {
                HitEntity.SetActive(false);
            }

            // clone all relevant MeshRenderer objects which have a collider
            MeshRenderer[] allObjects = FindMeshRenderers();

            if (RefreshWIM)
            {
                MappingCloneIdsToOriginals = new Dictionary<int, MeshRenderer>();
            }

            // clone all and add to list
            Clones = new List<MeshRenderer>();
            foreach (MeshRenderer obj in allObjects)
            {
                Clones.Add(GetWIMClone(obj));
            }

            // add figure to represent current position in WIM
            if (Doll)
            {
                CurrentDoll = Instantiate(Doll);
                CurrentDoll.SetActive(true);
                CurrentDoll.transform.position = SelectedPosition;
                CurrentDoll.transform.localPosition += new Vector3(0, Doll.transform.position.y, 0);
                
                CurrentDoll.transform.forward = new Vector3(Viewpoint.transform.forward.x, 0, Viewpoint.transform.forward.z);
                CurrentDoll.AddComponent<VRIL_WIMObject>();
                CurrentDoll.transform.eulerAngles = new Vector3(CurrentDoll.transform.rotation.eulerAngles.x, CurrentDoll.transform.rotation.eulerAngles.y, CurrentDoll.transform.rotation.eulerAngles.z);
                CurrentDoll.transform.parent = Wim.transform;
                CurrentShadowDoll = Instantiate(ShadowDoll != null ? ShadowDoll : CurrentDoll);
                CurrentShadowDoll.transform.parent = Wim.transform;
                if (ViewpointCamera)
                {
                    PrevCameraRotationEuler = ViewpointCamera.transform.localEulerAngles;
                }
            }

            // attach selection point
            HitEntity.transform.parent = Wim.transform;

            OrigPos = Wim.transform.position;
            OrigRot = Wim.transform.rotation;

            // downscale the world
            Wim.transform.localScale = CurrentScale;

            // init prev controller rotation
            PrevControllerRotation = RayHand.transform.localEulerAngles;
        }

        /// <summary>
        /// Draws the WIM
        /// </summary>
        private void DrawWIM()
        {
            // refresh positions and rotations in WIM if property set
            if (RefreshWIM)
            {
                foreach (MeshRenderer clone in Clones)
                {
                    MeshRenderer m = MappingCloneIdsToOriginals[clone.GetInstanceID()];
                    clone.transform.localPosition = m.transform.position;
                    clone.transform.localRotation = m.transform.rotation;
                }

                // find all objects again
                MeshRenderer[] findAll = FindMeshRenderers();

                // add new objects to WIM
                MeshRenderer[] toAdd = (from m in findAll where m.GetComponent<VRIL_WIMObject>() == null 
                                        && (from val in MappingCloneIdsToOriginals.Values where val.GetInstanceID() == m.GetInstanceID() select val).FirstOrDefault() == default select m).ToArray();
                foreach(MeshRenderer newM in toAdd)
                {
                    MeshRenderer clone = GetWIMClone(newM);
                    clone.transform.localScale = CurrentScale;
                    clone.transform.localPosition = newM.transform.position;
                    clone.transform.localRotation = newM.transform.rotation;
                    Clones.Add(clone);
                }

                // delete old objects also in WIM
                MeshRenderer[] toDelete = (from m in MappingCloneIdsToOriginals.Values where (from cur in findAll where cur.GetInstanceID() == m.GetInstanceID() select cur).FirstOrDefault() == default select m).ToArray();
                foreach (MeshRenderer oldM in toDelete)
                {
                    int instanceID = MappingCloneIdsToOriginals.FirstOrDefault(x => x.Value.GetInstanceID() == oldM.GetInstanceID()).Key;
                    MeshRenderer mToDelete = (from del in Clones where del.GetInstanceID() == instanceID select del).FirstOrDefault();
                    Clones.Remove(mToDelete);
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
                if (CurrentDoll && ViewpointCamera)
                {
                    float rotationDiffY = ViewpointCamera.transform.localEulerAngles.y - PrevCameraRotationEuler.y;
                    CurrentDoll.transform.RotateAround(CurrentDoll.transform.position, CurrentDoll.transform.up, rotationDiffY);
                    PrevCameraRotationEuler = ViewpointCamera.transform.localEulerAngles;
                    
                    if(FixedShadowDoll)
                    {
                        //Doll should look away from viewpoint
                        CurrentShadowDoll.transform.LookAt(Viewpoint.transform.position);
                        CurrentShadowDoll.transform.localEulerAngles = new Vector3(0, CurrentShadowDoll.transform.localEulerAngles.y - HALF_CIRCLE, 0);
                    }
                }
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
                // draw WIM first (apply changes of controllers etc.)
                DrawWIM();
                
                // prevent from blocking raycasts
                HitEntity.SetActive(false);
                if (Doll)
                {
                    CurrentShadowDoll.SetActive(false);
                }

                PositionSelected = false;
                WIMLineRenderer.enabled = false;

                // raycast results no hit without colliders
                if (DisableCollidersInWIM)
                {
                   foreach (Collider c in Wim.GetComponentsInChildren<Collider>())
                   {
                      c.enabled = true;
                   } 
                }

                Ray selectionRay = new Ray(RayHand.transform.position, RayHand.transform.forward);
                if (Physics.Raycast(selectionRay, out RaycastHit raycastHit, MaxRayDistance))
                {
                    VRIL_WIMObject wimSpace = raycastHit.transform.gameObject.GetComponent<VRIL_WIMObject>();

                    // ray hit with navigable WIM object that surface angle is small enough
                    if (wimSpace != null && wimSpace.GetComponent<VRIL_Navigable>() != null && Vector3.Angle(raycastHit.normal, Wim.transform.up) <= MaximumSurfaceSkewness)
                    {
                        // mark position as selected
                        WIMLineRenderer.enabled = true;
                        WIMLineRenderer.positionCount = 2;
                        WIMLineRenderer.SetPosition(0, selectionRay.origin);
                        WIMLineRenderer.SetPosition(1, raycastHit.point);
                        WIMLineRenderer.startColor = ValidPositionColor;
                        WIMLineRenderer.endColor = ValidPositionColor;
                        HitEntity.SetActive(true);
                        HitEntity.transform.position = raycastHit.point;
                        PositionSelected = true;

                        // adapt doll clone orientation
                        if (CurrentShadowDoll)
                        {
                            CurrentShadowDoll.transform.position = raycastHit.point;
                            CurrentShadowDoll.transform.localPosition += new Vector3(0, (ShadowDoll ? ShadowDoll.transform.position.y : Doll.transform.position.y) + DistanceToGround, 0);
                            float diffZ = RayHand.transform.localEulerAngles.z - PrevControllerRotation.z;
                            PrevControllerRotation = RayHand.transform.localEulerAngles;
                            
                            if(!FixedShadowDoll)
                            {
                                CurrentShadowDoll.transform.RotateAround(CurrentShadowDoll.transform.position, CurrentShadowDoll.transform.up, -diffZ);
                            }
                            CurrentShadowDoll.SetActive(true);
                        }
                    }
                    else
                    {
                        // remove markers in case nothing found
                        WIMLineRenderer.enabled = true;
                        WIMLineRenderer.positionCount = 2;
                        WIMLineRenderer.SetPosition(0, selectionRay.origin);
                        WIMLineRenderer.SetPosition(1, raycastHit.point);
                        WIMLineRenderer.startColor = InvalidPositionColor;
                        WIMLineRenderer.endColor = InvalidPositionColor;
                        HitEntity.SetActive(false);
                        if (CurrentShadowDoll)
                        {
                            CurrentShadowDoll.SetActive(false);
                        }
                    }
                }
                else
                {
                    WIMLineRenderer.enabled = false;
                }

                if (DisableCollidersInWIM)
                {
                    foreach (Collider c in Wim.GetComponentsInChildren<Collider>())
                    {
                        c.enabled = false;
                    }
                }
                yield return null;
            }
            WIMLineRenderer.enabled = false;
        }

        /// <summary>
        /// Travel (might trigger flight into miniature world)
        /// </summary>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if (PositionSelected)
            {
                PlayAudio();
                InitDistancesToViewpoint();

                if (UseFlightIntoTheMiniature)
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
            PrevViewpointRotation = Viewpoint.transform.rotation;
            TravelMode = true;
            float viewpointRotation = 0.0f;
            if (CurrentShadowDoll)
            {
                // calculation of rotation during flight: angle between the two objects * ray length * 2 (constant factor which was the best at some tests)
                viewpointRotation = ViewPointRotationFactor * Vector3.Angle(ViewpointCamera.transform.eulerAngles, CurrentShadowDoll.transform.eulerAngles) 
                    * (MaxRayDistance - Vector3.Distance(RayHand.transform.position, CurrentShadowDoll.transform.position));
                CurrentDoll.SetActive(false);
            }

            Manager.InputLocked = true;

            // start animation
            while (TravelMode)
            {
                DrawWIM();
                if (CurrentScale.x < FINAL_SCALE)
                {
                    CurrentScale *= (1 + ScaleVelocity);
                }
                if (CurrentShadowDoll)
                {
                    float step = viewpointRotation * Time.deltaTime;
                    Viewpoint.transform.rotation = Quaternion.RotateTowards(Viewpoint.transform.rotation, CurrentShadowDoll.transform.rotation, step);
                }
                Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position, HitEntity.transform.position, CurrentVelocity * Time.deltaTime);
                if (CurrentScale.x >= FINAL_SCALE)
                {
                    TravelMode = false;
                    Viewpoint.transform.rotation = PrevViewpointRotation;
                    Finish();
                }
                yield return null;
            }
        }

        private void Finish()
        {
            if (Wim)
            {
                Wim.transform.position = OrigPos;
                Wim.transform.rotation = OrigRot;
                Wim.transform.localScale = new Vector3(FINAL_SCALE, FINAL_SCALE, FINAL_SCALE);
                if (PositionSelected)
                {
                    SelectedPosition = HitEntity.transform.position + new Vector3(0, DistanceToGround, 0);
                    Viewpoint.transform.position = SelectedPosition;

                    if (Doll)
                    {
                        Viewpoint.transform.rotation = CurrentShadowDoll.transform.localRotation;
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
