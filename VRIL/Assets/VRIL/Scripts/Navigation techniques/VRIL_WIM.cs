using Assets.VRIL.Scripts;
using System;
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
    /// Implementation for a WIM technique
    /// </summary>
    public class VRIL_WIM : VRIL_NavigationTechniqueBase
    {
        // *************************************
        // public properties
        // *************************************

        [Header("WIM Settings")]
        [Tooltip(
            "True: Refresh WIM always. Position changes of objects in the large-scaled world are applied to WIM. False: No refresh")]
        public bool RefreshWIM = true;

        [Tooltip(
            "Sets the maximum allowed angle for a navigable WIM object surface (0° = positions only on horizontal surfaces are allowed, 90° = all positions are allowed)")]
        [Range(0.0f, 90.0f)]
        public float MaximumSurfaceAngle = 20.0f;

        [Tooltip("Factor to scale down the world")] [Range(0.0f, 0.99f)]
        public float ScaleFactor = 0.001f;

        [Tooltip("Min object sizes for WIM, smaller objects will be not cloned")]
        public float ObjectTresholdX = 1.0f;

        public float ObjectTresholdY = 1.0f;
        public float ObjectTresholdZ = 1.0f;

        [Tooltip("Distance of controller to WIM")]
        public float DistanceControllerToWIM = 0.005f;

        public bool DisableCollidersInWIM = true;

        [Tooltip("Time (in seconds) to wait for next WIM. Forces a pause between travel and next activation.")]
        public float SecondsToWaitForNextWIM = 0.5f;

        [Header("Flight Into the Miniature Settings")]
        [Tooltip("True: Fly into the new position in WIM (triggers an animation). False: Instant teleportation")]
        public bool UseFlightIntoTheMiniature = false;

        [Tooltip("Velocities for flying into the miniature")]
        public float ViewpointVelocity = 0.5f;

        [Range(1.0f, 10.0f)]
        public float ScaleVelocity = 1.001f;
        public float ViewPointRotationFactor = 3.0f;

        [Header("Avatar Settings")]
        [Tooltip(
            "An avatar is an object that represents the player in the WIM. Usually, it is a human figure but also a simple arrow can be used. " +
            "The model needs to be in normal size (in larged-scaled world), it will be automatically downscaled with other objects.")]
        public GameObject Avatar;

        [Tooltip(
            "The shadow avatar visualizes the new position and orientation of the viewpoint at the desired target position.")]
        public GameObject ShadowAvatar;

        [Tooltip(
            "True: Shadow avatar looks away from viewpoint. False: It is possible to change orientation of shadow avatar by rotating ray hand controller around.")]
        public bool FixedShadowAvatarOrientation = true;

        [Header("Ray Settings")] public float MaxRayDistance = 1.0f;
        public Color ValidPositionColor = Color.green;
        public Color InvalidPositionColor = Color.red;

        [Tooltip("Required or the laser color might be something different")]
        public Material LaserMaterial;

        [Tooltip("The assigned material can cast shadows if its not set to transparent")]
        public bool CastShadows = false;

        public float StartRayWidth = 0.005f;
        [Range(0.01f, 1f)] public float EndRayWidth = 0.005f;

        [Header("Selection Point Settings")]
        [Tooltip(
            "An object that represents the selected position in the WIM (for example, can be a disc similar to teleport).")]
        public GameObject HitEntity;

        [Tooltip("Distance of hit entity object to WIM ground.")]
        public float DistanceHitEntityToGround = 0.005f;

        [Header("Light Settings")]
        [Tooltip("Create Layer for WIM and set CullingMask in LightSource according to")]
        public LayerMask Layer;
        [Tooltip("Set how far above the WIM the light source should be")]
        public float DistanceLightToWim = 0.5f;
        public Light LightSource;

        // *************************************
        // constants
        // *************************************

        private const float FINAL_SCALE = 1.0f;
        private const float HALF_CIRCLE = 180.0f;
        private const string WIM_OBJECT_NAME = "WIM";
        private const string LINE_RENDERER = "VRIL_WIM_LineRenderer";


        // *************************************
        // private members
        // *************************************

        private Vector3 PrevCameraRotationEuler;

        // camera necessary to update avatar according to camera
        private Camera ViewpointCamera;

        // when technique is activated, the WIM is shown
        private bool IsActivated;
        private bool WIMObjectHit;

        // the viewpoint velocity used for flying into the miniature
        private float Velocity;

        // both controller objects (used for ray and WIM)
        private GameObject WIMHand;
        private GameObject RayHand;

        // line renderer for the position selection on the WIM
        private LineRenderer WIMLineRenderer;
        private GameObject WIMLineRendererObject;

        // allow next activation after given time
        private float Timer = 0.0f;
        private bool DelayToNextTravel = false;

        // figure which represents the player in the WIM
        private GameObject CurrentAvatar;

        // figure to visualize of the player on target position in the WIM
        private GameObject CurrentShadowAvatar;
        private float DistanceToGroundShadowAvatar = 0.0f;

        // save current position rotation and scale of WIM
        private Vector3 CurrentWIMPosition;
        private Quaternion CurrentWIMRotation;
        private Vector3 CurrentScale;

        // during flight into the miniature, no further inputs are allowed
        private bool TravelMode = false;

        // wim object and initial position and rotation of the miniature world
        private GameObject Wim;
        private Vector3 OrigPos;
        private Quaternion OrigRot;

        // save all WIM components (used for synchronizing both worlds)
        private IList<MeshRenderer> Clones;

        // save original mesh renderer for each cloned object instanceID (required for refreshing WIM)
        private IDictionary<int, MeshRenderer> MappingCloneIdsToOriginals;

        // reverse mapping for faster access
        private IDictionary<int, MeshRenderer> MappingOriginalIdsToClones;

        // necessary for shadow avatar rotation manipulation
        private Vector3 PrevControllerRotation;

        // necessary for viewpoint rotation manipulation
        private Quaternion PrevViewpointRotation;

        // calculated layer
        private int layer = 0;

        private bool checkWIMIgnores = false;

        protected virtual void Update()
        {
            if (DelayToNextTravel)
            {
                Timer += Time.deltaTime;
                if (Timer >= SecondsToWaitForNextWIM)
                {
                    DelayToNextTravel = false;
                    Timer = 0.0f;
                }
            }
        }

        public void Awake()
        {
            Initialize();
            layer = LayerMaskToLayer(Layer);
            WIMLineRendererObject = new GameObject(LINE_RENDERER);
            WIMLineRendererObject.AddComponent<LineRenderer>();
            WIMLineRenderer = WIMLineRendererObject.GetComponent<LineRenderer>();
            WIMLineRenderer.startWidth = StartRayWidth;
            WIMLineRenderer.endWidth = EndRayWidth;
            WIMLineRenderer.startColor = ValidPositionColor;
            WIMLineRenderer.endColor = ValidPositionColor;
            WIMLineRenderer.material = LaserMaterial;
            WIMLineRenderer.enabled = false;
            
            if (Avatar)
            {
                Avatar.SetActive(false);
            }

            if (ShadowAvatar)
            {
                ShadowAvatar.SetActive(false);
            }

            WIMLineRenderer.shadowCastingMode = CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;

            // get camera object
            if(HasComponent(Viewpoint, out Camera cam))
            {
                ViewpointCamera = cam;
            }
            else if(HasComponent(Viewpoint, out Camera camChild, true))
            {
                ViewpointCamera = camChild;
            }

            // bot required for changing position and orientation (create empty in case nothing defined)
            if (!HitEntity)
            {
                HitEntity = new GameObject();
            }

            if (!Avatar)
            {
                Avatar = new GameObject();
            }
            if(!ShadowAvatar)
            {
                ShadowAvatar = Avatar;
            }
            DistanceToGroundShadowAvatar = ShadowAvatar.transform.position.y;
            if(FindObjectsOfType<CanvasRenderer>() != null)
            {
                checkWIMIgnores = true;
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

            HitEntity.SetActive(false);
            if (HasComponent(HitEntity, out Renderer rend))
            {
                rend.material.SetColor("_Color", ValidPositionColor);
            }

            if (Avatar == null)
            {
                Debug.Log("Orientation changes not possible for WIM technique because no avatar was set.");
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
                if (TravelOnRelease && e.ButtonInteractionType == VRIL_ButtonInteractionType.Released)
                {
                    OnTravel(e);
                }
                else if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed)
                {
                    if (!IsActivated && !DelayToNextTravel)
                    {
                        TargetPosition = Viewpoint.transform.position - new Vector3(0, DistanceViewpointToGround, 0);
                        Velocity = ViewpointVelocity;
                        CurrentScale = Vector3.one;
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
            MeshRenderer[] meshRenderer = FindObjectsOfType<MeshRenderer>().Where(
                m => m.gameObject != null &&
                     m.gameObject.activeSelf &&
                     m.GetComponent<Collider>() != null &&
                     m.GetComponent<VRIL_WIMObject>() == null &&
                     (checkWIMIgnores ? m.GetComponent<VRIL_WIMIgnore>() == null : true) &&
                     Array.IndexOf(RegisteredControllers.ToArray(), m.gameObject) < 0 &&
                     (m.gameObject.GetInstanceID() != HitEntity.gameObject.GetInstanceID()) &&
                     (m.bounds.size.x >= ObjectTresholdX || m.bounds.size.y >= ObjectTresholdY ||
                      m.bounds.size.z >= ObjectTresholdZ)).ToArray();
            return meshRenderer;
        }

        /// <summary>
        /// Clones a mesh renderer object with properties for WIM
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private MeshRenderer CloneMeshRenderer(MeshRenderer obj)
        {
            MeshRenderer objClone = Instantiate(obj);
            objClone.name = WIM_OBJECT_NAME + obj.gameObject.name;
            if(layer != -1)
            {
                objClone.gameObject.layer = layer;
            }
            // append object to WIM
            objClone.transform.SetParent(Wim.transform, false);

            objClone.transform.position = obj.transform.position;
            objClone.gameObject.AddComponent<VRIL_WIMObject>();

            // set kinematic true for all WIM objects
            if (HasComponent(objClone.gameObject, out Rigidbody rigibody))
            {
                rigibody.isKinematic = true;
            }

            if (RefreshWIM)
            {
                MappingCloneIdsToOriginals[objClone.GetInstanceID()] = obj;
                MappingOriginalIdsToClones[obj.GetInstanceID()] = objClone;
            }

            return objClone;
        }

        /// <summary>
        /// https://answers.unity.com/questions/1288179/layer-layermask-which-is-set-in-inspector.html
        /// </summary>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        private int LayerMaskToLayer(LayerMask layerMask)
        {
            int layerNumber = 0;
            int layer = layerMask.value;
            while (layer > 0)
            {
                layer >>= 1;
                layerNumber++;
            }
            return layerNumber - 1;
        }

        /// <summary>
        /// Creates the world in miniature with player avatar on viewpoint position
        /// </summary>
        private void CreateWim()
        {
            // create WIM as new object
            Wim = new GameObject(WIM_OBJECT_NAME);
            
            Wim.AddComponent<VRIL_WIMObject>();
            Wim.transform.localScale = Vector3.one;
            Wim.transform.position = new Vector3(0, 0, 0);
            if (!TravelMode)
            {
                HitEntity.SetActive(false);
                if (layer != -1)
                {
                    HitEntity.gameObject.layer = layer;
                }
            }

            // clone all relevant MeshRenderer objects which have a collider
            MeshRenderer[] allObjects = FindMeshRenderers();

            if (RefreshWIM)
            {
                MappingCloneIdsToOriginals = new Dictionary<int, MeshRenderer>();
                MappingOriginalIdsToClones = new Dictionary<int, MeshRenderer>();
            }

            // clone all and add to list
            Clones = new List<MeshRenderer>();
            foreach (MeshRenderer obj in allObjects)
            {
                Clones.Add(CloneMeshRenderer(obj));
            }

            // add figure to represent current position in WIM
            if (Avatar)
            {
                CurrentAvatar = Instantiate(Avatar);
                CurrentAvatar.SetActive(true);
                CurrentAvatar.transform.position = TargetPosition;
                CurrentAvatar.transform.localPosition += new Vector3(0, Avatar.transform.position.y, 0);

                if (ViewpointCamera)
                {
                    CurrentAvatar.transform.forward = new Vector3(ViewpointCamera.transform.forward.x, 0,
                        ViewpointCamera.transform.forward.z);
                }
                else
                {
                    CurrentAvatar.transform.forward =
                        new Vector3(Viewpoint.transform.forward.x, 0, Viewpoint.transform.forward.z);
                }

                CurrentAvatar.AddComponent<VRIL_WIMObject>();
                CurrentAvatar.transform.eulerAngles = new Vector3(CurrentAvatar.transform.rotation.eulerAngles.x,
                    CurrentAvatar.transform.rotation.eulerAngles.y, CurrentAvatar.transform.rotation.eulerAngles.z);
                CurrentAvatar.transform.parent = Wim.transform;
                if (layer != -1)
                {
                    CurrentAvatar.gameObject.layer = layer;
                }
                CurrentShadowAvatar = Instantiate(ShadowAvatar);
                CurrentShadowAvatar.transform.parent = Wim.transform;
                if (layer != -1)
                {
                    CurrentShadowAvatar.gameObject.layer = layer;
                }
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
        /// Refresh WIM objects. Removes old objects which do not exist anymore and add new ones to the WIM
        /// It also updates all objects positions in the WIM
        /// </summary>
        private void RefreshWIMObjects()
        {
            // find mesh renderer objects again
            MeshRenderer[] findAll = FindMeshRenderers();

            // delete old objects
            foreach (KeyValuePair<int, MeshRenderer> m in MappingCloneIdsToOriginals.ToList())
            {
                if (!m.Value)
                {
                    MeshRenderer mToDelete = (from del in Clones where del.GetInstanceID() == m.Key select del)
                        .FirstOrDefault();
                    Clones.Remove(mToDelete);
                    MappingCloneIdsToOriginals.Remove(m.Key);
                    MappingOriginalIdsToClones.Remove(m.Value.GetInstanceID());
                    Destroy(mToDelete);
                }
            }

            // check wether new objects are added in the large-scaled world and add them in the WIM
            foreach (MeshRenderer m in findAll)
            {
                //bool found = MappingOriginalIdsToClones[m.GetInstanceID()] != default;

                //foreach (MeshRenderer orig in MappingCloneIdsToOriginals.Values)
                //{
                //    if (orig.GetInstanceID() == m.GetInstanceID())
                //    {
                //        found = true;
                //        break;
                //    }
                //}
                
                if (!MappingOriginalIdsToClones.ContainsKey(m.GetInstanceID()))
                {
                    MeshRenderer clone = CloneMeshRenderer(m);
                    clone.transform.localPosition = m.transform.position;
                    clone.transform.localRotation = m.transform.rotation;
                    Clones.Add(clone);
                }
            }

            // refresh position and rotation of all WIM objects according to their originals in the large-scaled world
            foreach (MeshRenderer clone in Clones)
            {
                MeshRenderer m = MappingCloneIdsToOriginals[clone.GetInstanceID()];
                clone.transform.localPosition = m.transform.position;
                clone.transform.localRotation = m.transform.rotation;
            }
        }

        /// <summary>
        /// Draws the WIM
        /// </summary>
        private void DrawWIM()
        {
            // refresh positions and rotations in WIM if property set
            if (RefreshWIM)
            {
                RefreshWIMObjects();
            }

            // no travel mode: refresh WIM position and rotation according to controller
            if (!TravelMode)
            {
                Ray ray = new Ray(WIMHand.transform.position, WIMHand.transform.up);
                CurrentWIMPosition = ray.GetPoint(DistanceControllerToWIM);
                CurrentWIMRotation = WIMHand.transform.rotation;
                Wim.transform.position = CurrentWIMPosition;
                Wim.transform.rotation = CurrentWIMRotation;
                if(LightSource)
                {
                    Vector3 lightPosition = ray.GetPoint(DistanceLightToWim);
                    LightSource.transform.position = lightPosition;
                }
                if (CurrentAvatar && ViewpointCamera)
                {
                    float rotationDiffY = ViewpointCamera.transform.localEulerAngles.y - PrevCameraRotationEuler.y;
                    CurrentAvatar.transform.RotateAround(CurrentAvatar.transform.position, CurrentAvatar.transform.up,
                        rotationDiffY);
                    PrevCameraRotationEuler = ViewpointCamera.transform.localEulerAngles;

                    if (FixedShadowAvatarOrientation)
                    {
                        //Avatar should look away from viewpoint
                        CurrentShadowAvatar.transform.LookAt(Viewpoint.transform.position);
                        CurrentShadowAvatar.transform.localEulerAngles = new Vector3(0,
                            CurrentShadowAvatar.transform.localEulerAngles.y - HALF_CIRCLE, 0);
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
                if (Avatar)
                {
                    CurrentShadowAvatar.SetActive(false);
                }

                PositionSelected = false;
                WIMLineRenderer.enabled = false;

                // raycast without colliders results no hit. This is why they have to be enabled for this method
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

                    if (wimSpace != null)
                    {
                        WIMObjectHit = true;
                    }
                    else
                    {
                        WIMObjectHit = false;
                    }

                    // ray hit with navigable WIM object that surface angle is small enough
                    if (wimSpace != null && wimSpace.GetComponent<VRIL_Navigable>() != null &&
                        Vector3.Angle(raycastHit.normal, Wim.transform.up) <= MaximumSurfaceAngle)
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

                        // set avatar clone orientation
                        if (CurrentShadowAvatar)
                        {
                            CurrentShadowAvatar.transform.position = raycastHit.point;
                            CurrentShadowAvatar.transform.localPosition += new Vector3(0, DistanceToGroundShadowAvatar, 0);
                            float diffZ = RayHand.transform.localEulerAngles.z - PrevControllerRotation.z;
                            PrevControllerRotation = RayHand.transform.localEulerAngles;

                            if (!FixedShadowAvatarOrientation)
                            {
                                CurrentShadowAvatar.transform.RotateAround(CurrentShadowAvatar.transform.position,
                                    CurrentShadowAvatar.transform.up, -diffZ);
                            }

                            CurrentShadowAvatar.SetActive(true);
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
                        if (CurrentShadowAvatar)
                        {
                            CurrentShadowAvatar.SetActive(false);
                        }
                    }
                }
                else
                {
                    WIMLineRenderer.enabled = false;
                    WIMObjectHit = false;
                }

                // disable colliders
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
            if (Wim)
            {
                // do nothing in case an invalid position was selected
                if (WIMObjectHit && !PositionSelected)
                {
                    return;
                }

                // valid position selected
                if (PositionSelected)
                {
                    PlayAudio();
                    SaveDistancesToViewpoint();

                    // trigger flight into the miniature
                    if (UseFlightIntoTheMiniature)
                    {
                        StartCoroutine(FlyingIntoMiniature());
                    }
                    else
                    {
                        Finish();
                    }
                }
                // selection out of WIM - cancel technique
                else
                {
                    Finish();
                }
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
            if (CurrentShadowAvatar)
            {
                // calculation of rotation during flight. The angle is multiplied by a constant factor
                // the result is multiplied with the remaining distance to max ray length
                // (the closer the point, the higher the rotation. The farer away, the lower the rotation)
                viewpointRotation = ViewPointRotationFactor * Vector3.Angle(ViewpointCamera.transform.eulerAngles,
                                                                CurrentShadowAvatar.transform.eulerAngles)
                                                            * (MaxRayDistance -
                                                               Vector3.Distance(RayHand.transform.position,
                                                                   CurrentShadowAvatar.transform.position));
                CurrentAvatar.SetActive(false);
            }

            Manager.InputLocked = true;

            Quaternion rotation = new Quaternion();
            if (CurrentShadowAvatar)
            {
                // target rotation is not rotation of shadow avatar - subtract local camera rotation first!
                Vector3 origRotShadowAvatar = CurrentShadowAvatar.transform.localEulerAngles;
                Vector3 temp = CurrentShadowAvatar.transform.localEulerAngles;
                temp.y -= ViewpointCamera.transform.localEulerAngles.y;
                CurrentShadowAvatar.transform.localEulerAngles = temp;
                rotation = Quaternion.Euler(CurrentShadowAvatar.transform.eulerAngles);
                CurrentShadowAvatar.transform.localEulerAngles = origRotShadowAvatar;
            }

            // use an empty hit entity object for correct position (includes distance to ground)
            GameObject temporaryHitEntity = new GameObject();
            temporaryHitEntity.transform.parent = Wim.transform;
            temporaryHitEntity.transform.localPosition = HitEntity.transform.localPosition;
            temporaryHitEntity.transform.localPosition += new Vector3(0, DistanceViewpointToGround, 0);

            // start animation
            while (TravelMode)
            {
                // draw WIM with current scale
                DrawWIM();

                // calculate next scale (WIM becomes larger)
                if (CurrentScale.x < FINAL_SCALE)
                {
                    CurrentScale *= ScaleVelocity;
                }

                // perform a rotation step
                if (CurrentShadowAvatar)
                {
                    float step = viewpointRotation * Time.deltaTime;
                    Viewpoint.transform.rotation =
                        Quaternion.RotateTowards(Viewpoint.transform.rotation, rotation, step);
                }

                // move viewpoint closer to the target position
                Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position,
                    temporaryHitEntity.transform.position, Velocity * Time.deltaTime);
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
                // replace viewpoint
                if (PositionSelected)
                {
                    TargetPosition = HitEntity.transform.localPosition + new Vector3(0, DistanceViewpointToGround, 0);
                    Viewpoint.transform.position = TargetPosition;

                    float? rotationDiffY = null;
                    if (Avatar)
                    {
                        Vector3 temp = CurrentShadowAvatar.transform.localEulerAngles;
                        temp.y -= ViewpointCamera.transform.localEulerAngles.y;
                        Quaternion rotation = Quaternion.Euler(temp);
                        Viewpoint.transform.rotation = rotation; //CurrentShadowAvatar.transform.localRotation;
                        rotationDiffY = CurrentAvatar.transform.localEulerAngles.y -
                                        CurrentShadowAvatar.transform.localEulerAngles.y;
                    }

                    TransferSelectedObjects(rotationDiffY);
                    PositionSelected = false;
                }

                Manager.InputLocked = false;
                IsActivated = false;
                if (HitEntity != null)
                {
                    HitEntity.SetActive(false);
                }

                HitEntity.transform.parent = null;

                // finally destroy WIM
                DestroyImmediate(Wim);

                DelayToNextTravel = true;
            }
        }

        /// <summary>
        /// Empty component to identify WIM objects
        /// </summary>
        private class VRIL_WIMObject : MonoBehaviour
        {
        }
    }
}