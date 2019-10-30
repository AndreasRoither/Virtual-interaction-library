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
        public Color ValidPositionColor = Color.green;
        public Color InvalidPositionColor = Color.red;
        [Tooltip("Required or the laser color might be something different")]
        public Material LaserMaterial;
        [Tooltip("The assigned material can cast shadows if its not set to transparent")]
        public bool CastShadows = false;
        public float StartRayWidth = 0.005f;
        [Range(0.01f, 1f)]
        public float EndRayWidth = 0.005f;

        private GameObject WIMHand;
        private GameObject RayHand;
        protected LineRenderer WIMLineRenderer;
        protected GameObject WIMLineRendererObject;

        //Save current position rotation and scale of WIM
        private Vector3 CurrentWIMPosition;
        private Quaternion CurrentWIMRotation;
        private Vector3 CurrentScale;

        private GameObject selectionPointWIM;
        private bool TravelMode = false;
        private GameObject Wim;
        private float TimeScale;

        public void Awake()
        {
            base.Initialize();
            Doll?.SetActive(false);
            selectionPointWIM = new GameObject();
            WIMLineRendererObject = new GameObject("VRIL_WIM_LineRenderer");
            WIMLineRendererObject.AddComponent<LineRenderer>();
            WIMLineRenderer = WIMLineRendererObject.GetComponent<LineRenderer>();
            WIMLineRenderer.startWidth = StartRayWidth;
            WIMLineRenderer.endWidth = EndRayWidth;
            WIMLineRenderer.startColor = ValidPositionColor;
            WIMLineRenderer.endColor = ValidPositionColor;
            WIMLineRenderer.material = LaserMaterial;
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
        }

        /// <summary>
        /// When the technique is activated
        /// </summary>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
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
            Wim = new GameObject();
            Wim.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            Wim.transform.position = new Vector3(0, 0, 0);
            selectionPointWIM.transform.parent = Wim.transform;
            
            // clone all relevant MeshRenderer
            MeshRenderer[] allObjects = UnityEngine.Object.FindObjectsOfType<MeshRenderer>().Where(m => m.bounds.size.x >= TresholdX || m.bounds.size.y >= TresholdY || m.bounds.size.z >= TresholdZ).ToArray();
            IList<MeshRenderer> clones = new List<MeshRenderer>();
            foreach (MeshRenderer obj in allObjects)
            {
                MeshRenderer objClone = UnityEngine.Object.Instantiate(obj);
                objClone.transform.parent = Wim.transform;
                objClone.transform.position = obj.transform.position;
                objClone.transform.gameObject.AddComponent<VRIL_WIMObject>();
                clones.Add(objClone);
            }

            // add figure to represent current position in WIM
            if (Doll != null)
            {
                GameObject tempDoll = UnityEngine.Object.Instantiate(Doll);
                tempDoll.SetActive(true);
                tempDoll.transform.position = Viewpoint.transform.position - new Vector3(0, DistanceToGround, 0);
                tempDoll.transform.forward = new Vector3(Viewpoint.transform.forward.x, 0, Viewpoint.transform.forward.z);
                tempDoll.transform.gameObject.AddComponent<VRIL_WIMObject>();
                tempDoll.transform.parent = Wim.transform;
            }

            // downscale the world
            Wim.transform.localScale = CurrentScale;

            // set kinematic for all WIM objects (removes physics)
            foreach (MeshRenderer objClone in clones)
            {
                if (objClone.GetComponent<Rigidbody>() != null)
                {
                    objClone.GetComponent<Rigidbody>().isKinematic = true;
                }
            }
        }

        /// <summary>
        /// Draws the WIM
        /// </summary>
        private void DrawWIM(VRIL_ControllerActionEventArgs e)
        {
            // recreate always WIM if not paused (keep in mind this might be slow)
            if (!PauseOnWIM)
            {
                Destroy(Wim);
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
                Wim.transform.localScale = CurrentScale;
            }
            Wim.transform.position = CurrentWIMPosition;
            Wim.transform.rotation = CurrentWIMRotation;
        }

        /// <summary>
        /// Coroutine for WIM technique
        /// </summary>
        protected IEnumerator WIM(VRIL_ControllerActionEventArgs e)
        {
            WIMLineRenderer.enabled = true;
            while (IsActivated && !TravelMode)
            {
                DrawWIM(e);
                Ray selectionRay = new Ray(RayHand.transform.position, RayHand.transform.forward);
                RaycastHit raycastHit;
                if (Physics.Raycast(selectionRay, out raycastHit, 2.0f))
                {
                    VRIL_WIMObject wimSpace = raycastHit.transform.gameObject.GetComponent<VRIL_WIMObject>();
                    if (wimSpace != null)
                    {
                        WIMLineRenderer.enabled = true;
                        WIMLineRenderer.SetPosition(0, selectionRay.origin);
                        WIMLineRenderer.SetPosition(1, raycastHit.point);
                        VRIL_Navigable navigable = wimSpace.transform.gameObject.GetComponent<VRIL_Navigable>();
                        if (navigable != null)
                        {
                            WIMLineRenderer.startColor = ValidPositionColor;
                            WIMLineRenderer.endColor = ValidPositionColor;
                            selectionPointWIM.transform.position = navigable.transform.position;
                        }
                        else
                        {
                            WIMLineRenderer.startColor = InvalidPositionColor;
                            WIMLineRenderer.endColor = InvalidPositionColor;
                        }
                    }
                    else
                    {
                        WIMLineRenderer.enabled = false;
                    }
                }
                else
                {
                    WIMLineRenderer.enabled = false;
                }
                yield return null;
            }
            WIMLineRenderer.enabled = false;
        }

        /// <summary>
        /// Travel triggers flight into miniature world
        /// </summary>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            TravelMode = true;
            StartCoroutine(FlyingIntoMiniature(e));
        }

        /// <summary>
        /// Coroutine for the flight into the miniature
        /// </summary>
        protected IEnumerator FlyingIntoMiniature(VRIL_ControllerActionEventArgs e)
        {
            bool flying = true;

            // no inputs allowed while flying into the miniature world
            Manager.InputLocked = true;
            while (flying)
            {
                DrawWIM(e);
                CurrentScale *= (1 + ScaleFactor * 0.5f);
                if (CurrentScale.x >= 1.0f)
                {
                    flying = false;
                }
                //Viewpoint.transform.position = Vector3.MoveTowards(Viewpoint.transform.position, selectionPointWIM.transform.position, Velocity * Time.unscaledDeltaTime);
                yield return null;
            }
            IsActivated = false;

            // destroy WIM after
            Destroy(Wim);
            TravelMode = false;
            Time.timeScale = TimeScale;
            Manager.InputLocked = false;
        }
    }
}
