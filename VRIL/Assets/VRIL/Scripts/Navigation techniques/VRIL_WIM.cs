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

        [Tooltip("Pauses the game while WIM active")]
        public bool PauseOnWIM = true;

        [Tooltip("Min sizes of object relevant for WIM")]
        public float TresholdX = 1.0f;
        public float TresholdY = 1.0f;
        public float TresholdZ = 1.0f;
        [Tooltip("Velocity for flying into the miniature")]
        public float Velocity = 0.1f;

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

        private GameObject WIMHand;
        private GameObject RayHand;
        protected LineRenderer WIMLineRenderer;
        protected GameObject WIMLineRendererObject;

        //Save current position rotation and scale of WIM
        private Vector3 CurrentWIMPosition;
        private Quaternion CurrentWIMRotation;
        private Vector3 CurrentScale;

        private bool TravelMode = false;
        private GameObject Wim;
        private float TimeScale;
        //private GameObject WimClone;
        private Vector3 origPos;
        private Quaternion origRot;

        private IList<GameObject> clones;

        public void Awake()
        {
            base.Initialize();
            if(Doll != null)
            {
                Doll.SetActive(false);
            }
            //selectionPointWIM = new GameObject();
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
            if(HitEntity == null)
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
            if(e.ControllerIndex != 0)
            {
                return;
            }
            if (RegisteredControllers.Count > 1)
            {
                // differentiate between ButtonStates
                if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Released)
                {
                    Debug.Log("Released!!");
                    if(PositionSelected)
                    {
                        OnTravel(e);
                    }
                }
                else if (e.ButtonInteractionType == VRIL_ButtonInteractionType.Pressed)
                {
                    if (!IsActivated)
                    {
                        CurrentScale = new Vector3(1.0f, 1.0f, 1.0f);
                        CurrentScale *= ScaleFactor;
                        CreateWim(e);
                        TimeScale = Time.timeScale;

                        // pauses the application if while WIM active
                        if (PauseOnWIM)
                        {
                            Time.timeScale = 0;
                        }
                        IsActivated = true;
                        StartCoroutine(WIM(e));
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
        private void CreateWim(VRIL_ControllerActionEventArgs e)
        {
            // create WIM as new object
            Wim = new GameObject("WIM");
            Wim.AddComponent<VRIL_WIMObject>();
            Wim.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            Wim.transform.position = new Vector3(0, 0, 0);
            if(!TravelMode)
            {
                HitEntity.SetActive(false);
            }
            
            // clone all relevant MeshRenderer
            MeshRenderer[] allObjects = FindObjectsOfType<MeshRenderer>().Where(
                m => m.gameObject != null &&
                m.gameObject.activeSelf &&
                Array.IndexOf(RegisteredControllers.ToArray(), m.gameObject) < 0 &&
                (m.bounds.size.x >= TresholdX || m.bounds.size.y >= TresholdY || m.bounds.size.z >= TresholdZ)).ToArray();


            clones = new List<GameObject>();
            foreach (MeshRenderer obj in allObjects)
            {
                GameObject objClone = Instantiate(obj.gameObject);
                //MeshRenderer objClone = Instantiate(obj);
                objClone.tag = "WIMclone";
                objClone.name = "WIM" + obj.gameObject.name;
                objClone.transform.parent = Wim.transform;
                objClone.transform.position = obj.transform.position;
                objClone.AddComponent<VRIL_WIMObject>();

                if(obj.GetComponent<MeshCollider>() != null)
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
                tempDoll.transform.parent = Wim.transform;
            }

            // attach selection point
            HitEntity.transform.parent = Wim.transform;

            Vector3 before = HitEntity.transform.position;

            origPos = Wim.transform.position;
            origRot = Wim.transform.rotation;

            // downscale the world
            Wim.transform.localScale = CurrentScale;
            
        }

        /// <summary>
        /// Draws the WIM
        /// </summary>
        private void DrawWIM(VRIL_ControllerActionEventArgs e)
        {
            // recreate always WIM if not paused (keep in mind this might be slow)
            if (!PauseOnWIM)
            {
                HitEntity.transform.parent = null;

                // destroy before next frame
                DestroyImmediate(Wim);
                CreateWim(e);
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
                Debug.Log("Pos before scale: " + before + ", after: " + HitEntity.transform.position);
            }
            Wim.transform.position = CurrentWIMPosition;
            Wim.transform.rotation = CurrentWIMRotation;
            //diffWIMViewpoint = Viewpoint.transform.rotation * Quaternion.Inverse(Wim.transform.rotation);
        }

        /// <summary>
        /// Coroutine for WIM technique
        /// </summary>
        protected IEnumerator WIM(VRIL_ControllerActionEventArgs e)
        {
            while (IsActivated && !TravelMode)
            {
                DrawWIM(e);
                HitEntity.SetActive(false);
                PositionSelected = false;

                Ray selectionRay = new Ray(RayHand.transform.position, RayHand.transform.forward);

                //Debug.DrawRay(selectionRay.origin, selectionRay.GetPoint(MaxRayDistance * 2));
                if (Physics.Raycast(selectionRay, out RaycastHit raycastHit, MaxRayDistance) && raycastHit.transform.CompareTag("WIMclone"))
                {
                    VRIL_WIMObject wimSpace = raycastHit.transform.gameObject.GetComponent<VRIL_WIMObject>();
                    if(wimSpace != null && wimSpace.GetComponent<VRIL_Navigable>() != null)
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
                        //Debug.Log("HitEntity-Position: " + HitEntity.transform.position);
                        //Renderer rend = raycastHit.transform.gameObject.GetComponent<Renderer>();
                        //rend.material.SetColor("_Color", ValidPositionColor);
                        
                        //SelectedPosition = HitEntity.transform.position * (1 / ScaleFactor);
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
                    }

                    //Renderer rend = raycastHit.transform.gameObject.GetComponent<Renderer>();
                    //rend.material.SetColor("_Color", ValidPositionColor);
                    //Debug.Log("Hit: " + raycastHit.transform.gameObject.name);
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
                //yield return new WaitForFixedUpdate();
            }
            //HitEntity.SetActive(false);
            WIMLineRenderer.enabled = false;
        }

        /// <summary>
        /// Travel triggers flight into miniature world
        /// </summary>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if(FlyingIntoTheMiniature)
            {
                TravelMode = true;
            }
            else
            {
                if (PositionSelected)
                {
                    Wim.transform.position = origPos;
                    Wim.transform.rotation = origRot;
                    Wim.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    SelectedPosition = HitEntity.transform.position + new Vector3(0, DistanceToGround, 0);
                    InitDistancesToViewpoint();
                    Viewpoint.transform.position = SelectedPosition;
                    UpdateObjects();
                    PositionSelected = false;
                }
                if (HitEntity != null)
                {
                    HitEntity.SetActive(false);
                }
            }

            IsActivated = false;

            // destroy WIM after
            HitEntity.transform.parent = null;
            DestroyImmediate(Wim);

            TravelMode = false;
            Time.timeScale = TimeScale;
            Manager.InputLocked = false;
            //GameObject clone = Instantiate(Wim);

            //Vector3 prevScale = Wim.transform.localScale;
            //Vector3 prevPosition = Wim.transform.position;
            //Quaternion prevRotation = Wim.transform.rotation;

            //StartCoroutine(FlyingIntoMiniature(e));
        }

        /// <summary>
        /// Coroutine for the flight into the miniature
        /// </summary>
        protected IEnumerator FlyingIntoMiniature(VRIL_ControllerActionEventArgs e)
        {
            // no inputs allowed while flying into the miniature world
            Manager.InputLocked = true;
            while (TravelMode)
            {
                DrawWIM(e);
                CurrentScale *= (1 + ScaleFactor);
                if (CurrentScale.x >= 1.0f)
                {
                    TravelMode = false;
                }
                Debug.Log("HitEntity-Position scaled: " + HitEntity.transform.position);
                Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position, HitEntity.transform.position, (1 + ScaleFactor) * (1 + ScaleFactor) * Time.unscaledDeltaTime);


                if (Vector3.Distance(Viewpoint.transform.position, HitEntity.transform.position) < 0.0001f)
                {
                    Debug.Log("Distance!");
                    TravelMode = false;
                    PositionSelected = false;
                    TravelMode = false;
                    Manager.InputLocked = false;
                }

                //Debug.Log("Viewpoint: " + Viewpoint.transform.position);
                //Debug.Log("HitEntity-Position scaled: " + HitEntity.transform.position);
                yield return null;
            }
            //Viewpoint.transform.rotation = diffWIMViewpoint * Viewpoint.transform.rotation;

        }
    }
}
