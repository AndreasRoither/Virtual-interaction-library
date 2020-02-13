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
        private bool IsActivated;

        [Header("WIM Settings")]
        [Tooltip("Flying into the miniature for viewpoint translation")]
        public bool FlyingIntoTheMiniature = false;

        [Tooltip("Player representation in WIM (real world size)")]
        public GameObject Doll;

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

        public GameObject Camera;

        private GameObject WIMHand;
        private GameObject RayHand;
        protected LineRenderer WIMLineRenderer;
        protected GameObject WIMLineRendererObject;

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
        private IList<GameObject> clones;
        private float currentVelocity;

        // doll ghost to visualize new position
        private GameObject dollClone;

        // necessary for doll ghost rotation
        private Vector3 prevControllerRotation;

        // init controller z rot
        private float rayHandZInit;

        public void Awake()
        {
            base.Initialize();
            if (Doll != null)
            {
                Doll.SetActive(false);
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
        }

        public void Start()
        {
            if (RegisteredControllers.Count > 1)
            {
                WIMHand = RegisteredControllers[0];
                RayHand = RegisteredControllers[1];
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
        }

        /// <summary>
        /// When the technique is activated
        /// </summary>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (e.ControllerIndex != 0)
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

            // fill clone list
            clones = new List<GameObject>();
            foreach (MeshRenderer obj in allObjects)
            {
                GameObject objClone = Instantiate(obj.gameObject);
                objClone.tag = "WIMclone";
                objClone.name = "WIM" + obj.gameObject.name;
                objClone.transform.parent = Wim.transform;
                objClone.transform.position = obj.transform.position;
                objClone.AddComponent<VRIL_WIMObject>();

                if (obj.GetComponent<MeshCollider>() != null)
                {
                    MeshCollider mesh = objClone.GetComponent<MeshCollider>();
                    mesh.convex = true;
                    mesh.isTrigger = true;
                    objClone.AddComponent<BoxCollider>();
                }
                else
                {
                    Collider col = obj.GetComponent<Collider>();
                    col.isTrigger = true;
                }

                // set kinematic for all WIM objects (removes physics)
                if (objClone.GetComponent<Rigidbody>() != null)
                {
                    objClone.GetComponent<Rigidbody>().isKinematic = true;
                }
                objClone.SetActive(false);

                clones.Add(objClone);
            }

            // add figure to represent current position in WIM
            if (Doll != null)
            {
                GameObject tempDoll = Instantiate(Doll);
                tempDoll.SetActive(true);
                tempDoll.transform.position = Viewpoint.transform.position - new Vector3(0, DistanceToGround, 0);
                tempDoll.transform.forward = new Vector3(Viewpoint.transform.forward.x, 0, Viewpoint.transform.forward.z);
                tempDoll.AddComponent<VRIL_WIMObject>();
                tempDoll.transform.eulerAngles = new Vector3(tempDoll.transform.rotation.eulerAngles.x, Camera.transform.rotation.eulerAngles.y, tempDoll.transform.rotation.eulerAngles.z);
                tempDoll.transform.parent = Wim.transform;
                dollClone = Instantiate(tempDoll);
                dollClone.transform.parent = Wim.transform;
            }

            // attach selection point
            HitEntity.transform.parent = Wim.transform;

            Vector3 before = HitEntity.transform.position;

            origPos = Wim.transform.position;
            origRot = Wim.transform.rotation;

            // downscale the world
            Wim.transform.localScale = CurrentScale;

            //Init prev controller rotation
            prevControllerRotation = RayHand.transform.localEulerAngles;
        }

        /// <summary>
        /// Draws the WIM
        /// </summary>
        private void DrawWIM()
        {
            // recreate always WIM if not paused (keep in mind this might be slow)
            if (!RefreshWIM)
            {
                HitEntity.transform.parent = null;

                // destroy before next frame
                DestroyImmediate(Wim);
                CreateWim();
            }

            // no travel mode: refresh WIM position and rotation according to controller
            if (!TravelMode)
            {
                Ray ray = new Ray(WIMHand.transform.position, WIMHand.transform.up);
                CurrentWIMPosition = ray.GetPoint(DistanceControllerToWIM);
                CurrentWIMRotation = WIMHand.transform.rotation;
            }

            // travel mode: refresh the scale of the world
            else
            {
                Vector3 before = HitEntity.transform.position;
                Wim.transform.localScale = CurrentScale;
            }
            Wim.transform.position = CurrentWIMPosition;
            Wim.transform.rotation = CurrentWIMRotation;
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
                if(Doll)
                {
                    dollClone.SetActive(false);
                }
                
                PositionSelected = false;
                WIMLineRenderer.enabled = false;

                Ray selectionRay = new Ray(RayHand.transform.position, RayHand.transform.forward);

                if (Physics.Raycast(selectionRay, out RaycastHit raycastHit, MaxRayDistance) && raycastHit.transform.CompareTag("WIMclone"))
                {
                    VRIL_WIMObject wimSpace = raycastHit.transform.gameObject.GetComponent<VRIL_WIMObject>();
                    if (wimSpace != null && wimSpace.GetComponent<VRIL_Navigable>() != null)
                    {
                        WIMLineRenderer.enabled = true;
                        WIMLineRenderer.positionCount = 2;
                        WIMLineRenderer.SetPosition(0, selectionRay.origin);
                        WIMLineRenderer.SetPosition(1, raycastHit.point);
                        WIMLineRenderer.startColor = ValidPositionColor;
                        WIMLineRenderer.endColor = ValidPositionColor;
                        HitEntity.SetActive(true);
                        HitEntity.transform.position = raycastHit.point;
                        PositionSelected = true;
                        if (Doll)
                        {
                            dollClone.transform.position = HitEntity.transform.position;
                            float diffZ = RayHand.transform.localEulerAngles.z - prevControllerRotation.z;
                            prevControllerRotation = RayHand.transform.localEulerAngles;
                            dollClone.transform.RotateAround(dollClone.transform.position, dollClone.transform.up, diffZ);
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
                        if (Doll)
                        {
                            dollClone.SetActive(false);
                        }
                    }
                }
                else
                {
                    WIMLineRenderer.enabled = false;
                }

                foreach (GameObject objClone in clones)
                {
                    objClone.SetActive(true);
                }
                yield return null;
            }
            WIMLineRenderer.enabled = false;
        }

        float diff = 0.0f;
        /// <summary>
        /// Travel - might trigger flight into miniature world
        /// </summary>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if(PositionSelected)
            {
                PlayAudio();
                InitDistancesToViewpoint();

                //Differences of doll angles
                if (Doll)
                {
                    diff = RayHand.transform.localEulerAngles.z - rayHandZInit;
                }

                if (FlyingIntoTheMiniature)
                {
                    TravelMode = true;
                    StartCoroutine(FlyingIntoMiniature());
                }
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
            // no inputs allowed while flying into the miniature world
            Manager.InputLocked = true;
            while (TravelMode)
            {
                DrawWIM();
                if (CurrentScale.x < 1.0f)
                {
                    CurrentScale *= (1 + ScaleVelocity);
                    //CurrentScale += new Vector3(ScaleVelocity, ScaleVelocity, ScaleVelocity);
                }
                currentVelocity *= (2 + CurrentScale.x);
                //currentVelocity += CurrentScale.x;
                HitEntity.transform.parent = null;
                Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position, HitEntity.transform.position, currentVelocity * Time.deltaTime);
                HitEntity.transform.parent = Wim.transform;
                if (Vector3.Distance(Viewpoint.transform.position, HitEntity.transform.position) < 0.0001f)
                {
                    TravelMode = false;
                    PositionSelected = false;
                    Manager.InputLocked = false;
                    Finish();
                }
                yield return null;
            }
        }

        private void Finish()
        {

            Manager.InputLocked = false;
            Wim.transform.position = origPos;
            Wim.transform.rotation = origRot;
            Wim.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            SelectedPosition = HitEntity.transform.position + new Vector3(0, DistanceToGround, 0);
            Viewpoint.transform.position = SelectedPosition;
            
            //Angle diff of both dolls = angle change of viewpoint
            if(Doll)
            {
                Viewpoint.transform.RotateAround(Viewpoint.transform.position, Viewpoint.transform.up, diff);
            }

            UpdateObjects();
            PositionSelected = false;
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
