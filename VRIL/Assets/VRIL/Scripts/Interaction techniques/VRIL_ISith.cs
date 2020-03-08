using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Interactable;

namespace VRIL.InteractionTechniques
{
    public class VRIL_ISith : VRIL_InteractionTechniqueBase
    {
        private bool IsActivated = false;
        private LineRenderer ISithLineRendererLeftHand;
        private LineRenderer ISithLineRendererRightHand;
        private LineRenderer ISithLineRendererConnection;
        private GameObject LeftLineRendererObject;
        private GameObject RightLineRendererObject;
        private GameObject LinesConnectionGameObject;
        private GameObject SelectionModelInstance = null;

        private GameObject MainHand;
        private GameObject SecondaryHand;

        [Header("Ray Settings")]
        [Range(0.01f, 1f)]
        public float StartRayWidth = 0.01f;

        [Range(0.01f, 1f)]
        public float EndRayWidth = 0.01f;

        public float MaxRayDistance = 25;

        public Color StartRayColor = Color.red;
        public Color EndRayColor = Color.red;

        [Tooltip("Required or the laser color might be something different")]
        public Material LaserMaterial;

        [Tooltip("The assigned material can cast shadows if its not set to transparent")]
        public bool CastShadows = false;

        [Space]
        [Header("Selection")]
        [Tooltip("SelectionModel should not have a collider!")]
        public GameObject SelectionModel;

        [Range(0.0f, 5.0f)]
        public float MaxSphereDiagnoal = 1f;

        [Range(0.0f, 5.0f)]
        public float MinSphereDiagonal = 0.3f;

        private float CurrentSphereDiagonal;

        [Space]
        [Header("Coroutine")]
        [Range(0.0f, 0.1f)]
        [Tooltip("1 = 1 sec")]
        public float CoroutineWaitTime = 0.03f;

        /// <summary>
        /// Called before the start function to initialize base
        /// </summary>
        public void Awake()
        {
            base.Initialize();
        }

        /// <summary>
        /// Initialize the interaction technique
        /// </summary>
        public void Start()
        {
            if (SelectionModel?.GetComponent<Collider>())
            {
                Debug.LogError($"<b>{nameof(VRIL_ISith)}:</b>\n The SelectionModel is selectable! Remove any colliders from this gameobject.");
            }
            if (RegisteredControllers.Count > 1)
            {
                InitISith();
            }
            else
            {
                Debug.LogError($"<b>{nameof(VRIL_ISith)}:</b>\n This technique requires at least two controllers!");
            }
        }

        /// <summary>
        /// Set and activate all necessary components
        /// </summary>
        private void InitISith()
        {
            MainHand = RegisteredControllers[0];
            SecondaryHand = RegisteredControllers[1];

            LeftLineRendererObject = new GameObject("VRIL_First_LineRenderer");
            RightLineRendererObject = new GameObject("VRIL_Second_LineRenderer");
            LinesConnectionGameObject = new GameObject("VRIL_Line_Connection");

            LeftLineRendererObject.AddComponent<LineRenderer>();
            RightLineRendererObject.AddComponent<LineRenderer>();
            LinesConnectionGameObject.AddComponent<LineRenderer>();

            ISithLineRendererLeftHand = LeftLineRendererObject.GetComponent<LineRenderer>();
            ISithLineRendererRightHand = RightLineRendererObject.GetComponent<LineRenderer>();
            ISithLineRendererConnection = LinesConnectionGameObject.GetComponent<LineRenderer>();

            ISithLineRendererLeftHand.startWidth = ISithLineRendererRightHand.startWidth = ISithLineRendererConnection.startWidth = StartRayWidth;
            ISithLineRendererLeftHand.endWidth = ISithLineRendererRightHand.endWidth = ISithLineRendererConnection.endWidth = EndRayWidth;

            ISithLineRendererLeftHand.startColor = ISithLineRendererRightHand.startColor = ISithLineRendererConnection.startColor = StartRayColor;
            ISithLineRendererLeftHand.endColor = ISithLineRendererRightHand.endColor = ISithLineRendererConnection.endColor = EndRayColor;

            ISithLineRendererLeftHand.material = ISithLineRendererRightHand.material = ISithLineRendererConnection.material = LaserMaterial;

            if (CastShadows)
            {
                ISithLineRendererLeftHand.shadowCastingMode = ShadowCastingMode.On;
                ISithLineRendererRightHand.shadowCastingMode = ShadowCastingMode.On;
                ISithLineRendererConnection.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                ISithLineRendererLeftHand.shadowCastingMode = ShadowCastingMode.Off;
                ISithLineRendererRightHand.shadowCastingMode = ShadowCastingMode.Off;
                ISithLineRendererConnection.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // raycast are not that important so the update function is sufficient
        public void Update()
        {
            if (SelectionModelInstance != null && IsActivated)
            {
                Collider[] hitColliders = Physics.OverlapSphere(SelectionModelInstance.transform.position, MaxSphereDiagnoal / 2);

                if (hitColliders.Length > 0)
                {
                    foreach (var collider in hitColliders)
                    {
                        VRIL_Interactable tempObj = collider.transform.gameObject.GetComponent<VRIL_Interactable>();

                        if (tempObj != null
                            && tempObj.Selection_Selectable
                            && tempObj != SelectionModelInstance
                            && tempObj != MainHand
                            && tempObj != SecondaryHand
                            && !SelectableObjects.Contains(tempObj))
                        {
                            SelectableObjects.Add(tempObj);
                        }
                    }
                }
                else
                {
                    SelectableObjects.Clear();
                }
            }
        }

        /// <summary>
        /// Unity specific, When drawing gizmos is activated
        /// Shows the sphere in which Interactable Objects can be selected
        /// </summary>
        public void OnDrawGizmos()
        {
            if (SelectionModelInstance != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(SelectionModelInstance.transform.position, CurrentSphereDiagonal / 2);
            }
        }

        /// <summary>
        /// When the technique is activated
        /// </summary>
        public override void OnActivation(VRIL_ControllerActionEventArgs e)
        {
            if (!IsActivated)
            {
                IsActivated = true;
                StartCoroutine(ISith());
            }
        }

        /// <summary>
        /// When the technique stops
        /// </summary>
        public override void OnStop(VRIL_ControllerActionEventArgs e)
        {
            // When a coroutine is stared via a IEnumerator, stopping it without the
            // same IEnumerator won't work. So this next line is useless without the object
            // StopCoroutine(ISith());
            IsActivated = false;
        }

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
        /// Coroutine for iSith technique
        /// Recalculates closest point between two controllers, Checks for any selectable object
        /// <para/>
        /// Instantiates a new selection model to indicate selection of objects
        /// <para/>
        /// </summary>
        /// <returns>WaitForSeconds()</returns>
        private IEnumerator ISith()
        {
            Vector3 leftHandRayPoint;
            Vector3 rightHandRayPoint;
            Vector3 midPoint;

            Ray firstRay;
            Ray secondRay;

            ISithLineRendererLeftHand.enabled = true;
            ISithLineRendererRightHand.enabled = true;
            ISithLineRendererConnection.enabled = true;

            if (SelectionModelInstance == null)
            {
                SelectionModelInstance = Instantiate(SelectionModel);
                SelectionModelInstance.SetActive(false);
                SelectionModelInstance.name = "VRIL_SelectionModel_iSith";
                SelectionModelInstance.transform.localScale = new Vector3(MaxSphereDiagnoal, MaxSphereDiagnoal, MaxSphereDiagnoal);
                CurrentSphereDiagonal = MaxSphereDiagnoal;
            }

            while (IsActivated)
            {
                // rays from the hands
                firstRay = new Ray(MainHand.transform.position, MainHand.transform.forward);
                secondRay = new Ray(SecondaryHand.transform.position, SecondaryHand.transform.forward);

                // set origin
                ISithLineRendererLeftHand.SetPosition(0, firstRay.origin);
                ISithLineRendererRightHand.SetPosition(0, secondRay.origin);

                ISithLineRendererLeftHand.SetPosition(1, firstRay.GetPoint(MaxRayDistance));
                ISithLineRendererRightHand.SetPosition(1, secondRay.GetPoint(MaxRayDistance));

                if (ClosestPointsOnTwoLines(out leftHandRayPoint, out rightHandRayPoint, firstRay.GetPoint(0.1f), firstRay.direction, secondRay.GetPoint(0.1f), secondRay.direction))
                {
                    if (IsPointBtwTwoPoints(leftHandRayPoint, firstRay.origin, firstRay.GetPoint(MaxRayDistance)) && IsPointBtwTwoPoints(rightHandRayPoint, secondRay.origin, secondRay.GetPoint(MaxRayDistance)))
                    {
                        // in case it got disabled
                        ISithLineRendererConnection.enabled = true;

                        ISithLineRendererConnection.SetPosition(0, leftHandRayPoint);
                        ISithLineRendererConnection.SetPosition(1, rightHandRayPoint);

                        // non optimized
                        // float connectionRayDistance = Vector3.Distance(leftHandRayPoint, rightHandRayPoint);
                        float connectionRayDistance = (leftHandRayPoint - rightHandRayPoint).sqrMagnitude;

                        midPoint.x = leftHandRayPoint.x - (leftHandRayPoint.x - rightHandRayPoint.x) / 2;
                        midPoint.y = leftHandRayPoint.y - (leftHandRayPoint.y - rightHandRayPoint.y) / 2;
                        midPoint.z = leftHandRayPoint.z - (leftHandRayPoint.z - rightHandRayPoint.z) / 2;

                        // move model
                        SelectionModelInstance.SetActive(true);
                        SelectionModelInstance.transform.position = midPoint;

                        // resize based on connectionraydistance
                        if (connectionRayDistance < MaxSphereDiagnoal && connectionRayDistance > MinSphereDiagonal)
                        {
                            CurrentSphereDiagonal = connectionRayDistance - connectionRayDistance / 4;
                            SelectionModelInstance.transform.localScale = new Vector3(CurrentSphereDiagonal, CurrentSphereDiagonal, CurrentSphereDiagonal);
                        }
                    }
                    else
                    {
                        // disable sphere
                        if (SelectionModelInstance != null)
                        {
                            SelectionModelInstance.SetActive(false);
                            //Destroy(SelectionModelInstance);
                            //SelectionModelInstance = null;
                        }

                        // disable linerenderer
                        ISithLineRendererConnection.enabled = false;
                    }
                }

                yield return new WaitForSeconds(CoroutineWaitTime);
            }

            ISithLineRendererLeftHand.enabled = false;
            ISithLineRendererRightHand.enabled = false;
            ISithLineRendererConnection.enabled = false;

            SelectionModelInstance?.SetActive(false);
        }

        // credit from http://wiki.unity3d.com/index.php/3d_Math_functions
        // VRIL: performance optimization
        /// <summary>
        /// Calculates the closest point between two lines
        /// </summary>
        /// <param name="closestPointLine1">Closest Point on line 1</param>
        /// <param name="closestPointLine2">Closest Point on line 2</param>
        /// <param name="linePoint1">Point on line 1</param>
        /// <param name="lineVec1">Vector of line 1</param>
        /// <param name="linePoint2">Point on line 2</param>
        /// <param name="lineVec2">Vector of line 2</param>
        /// <returns></returns>
        /// <example>ClosestPointsOnTwoLines(out leftHandRayPoint, out rightHandRayPoint, leftHandRay.GetPoint(0.1f), leftHandRay.direction, rightHandRay.GetPoint(0.1f), rightHandRay.direction))</example>
        public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {
            closestPointLine1 = Vector3.zero;
            closestPointLine2 = Vector3.zero;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            //lines are not parallel
            if (d != 0.0f)
            {
                Vector3 r = linePoint1 - linePoint2;
                float c = Vector3.Dot(lineVec1, r);
                float f = Vector3.Dot(lineVec2, r);

                float s = (b * f - c * e) / d;
                float t = (a * f - c * b) / d;

                closestPointLine1 = linePoint1 + lineVec1 * s;
                closestPointLine2 = linePoint2 + lineVec2 * t;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the point is in front of the controller
        /// </summary>
        /// <param name="ControllerForwardVector">Controller vector</param>
        /// <param name="point">point</param>
        /// <returns></returns>
        public bool CheckIfPointIsInfrontOfController(Vector3 ControllerForwardVector, Vector3 point)
        {
            float angle = Vector3.Angle(point, ControllerForwardVector);
            Debug.Log(angle);

            if (Mathf.Abs(angle) < 130) return true;
            return false;
        }

        // credit
        // https://stackoverflow.com/questions/11907947/how-to-check-if-a-point-lies-on-a-line-between-2-other-points
        /// <summary>
        /// Checks if a point is on a line between two other points
        /// </summary>
        /// <param name="point">checking point</param>
        /// <param name="startPoint">starting point</param>
        /// <param name="endPoint">ending point</param>
        /// <returns></returns>
        public bool IsPointBtwTwoPoints(Vector3 point, Vector3 startPoint, Vector3 endPoint)
        {
            float x = endPoint.x - startPoint.x;
            float y = endPoint.y - startPoint.y;

            // check if line is more horizontal than vertical etc.
            if (Mathf.Abs(x) >= Mathf.Abs(y))
                return x > 0 ?
                  (startPoint.x <= point.x && point.x <= endPoint.x) :
                  (endPoint.x <= point.x && point.x <= startPoint.x);
            else
                return y > 0 ?
                  (startPoint.y <= point.y && point.y <= endPoint.y) :
                  (endPoint.y <= point.y && point.y <= startPoint.y);
        }
    }
}