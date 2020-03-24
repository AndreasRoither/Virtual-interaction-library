using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRIL.Manager;
using VRIL.ControllerActionEventArgs;

/// <summary>
/// This keyboard script is used for navigation technique examples
/// </summary>
public class VRIL_Keyboard2 : MonoBehaviour
{
    public VRIL_Manager Manager;

    public GameObject LeftHand = null;
    public GameObject RightHand = null;

    public float RotationFactor = 1f;
    public float MoveFactor = 0.02f;

    public Text UIText;

    public bool pressedF = false;

    public GameObject Viewpoint = null;

    public enum Controller
    {
        LeftController,
        RightController,
    }

    public Controller SelectedController = Controller.LeftController;
    private readonly Dictionary<Controller, int> ControllerNumberMapping = new Dictionary<Controller, int>();

    void Start()
    {
        if(!Viewpoint)
        {
            Debug.LogError("Viewpoint not set!");
        }
        if (!Manager)
        {
            Debug.LogError("Manager not set!");
        }
        UIText.text = SelectedController == Controller.LeftController ? "Selected Controller: LeftController" : "Selected Controller: RightController";
        ControllerNumberMapping[Controller.LeftController] = 0;
        ControllerNumberMapping[Controller.RightController] = 1;
    }

    void Update()
    {
        if (LeftHand != null && RightHand != null && Manager != null && Viewpoint != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectedController = Controller.LeftController;
                UIText.text = "Selected Controller: LeftController";
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectedController = Controller.RightController;
                UIText.text = "Selected Controller: RightController";
            }

            if (Input.GetMouseButtonDown(0))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[SelectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager.OnControllerAction(e);
            }

            if (Input.GetMouseButtonUp(0))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[SelectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager.OnControllerAction(e);
            }

            if (Input.GetMouseButtonDown(1))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[SelectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager.OnControllerAction(e);
            }

            if (Input.GetMouseButtonUp(1))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[SelectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager.OnControllerAction(e);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[SelectedController], VRIL.Base.VRIL_ButtonType.Touchpad, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager.OnControllerAction(e);
            }

            if (Input.GetKeyDown(KeyCode.F) && pressedF == false)
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[SelectedController], VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager.OnControllerAction(e);
                pressedF = true;
            }
            if (Input.GetKeyUp(KeyCode.F) && pressedF)
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[SelectedController], VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager.OnControllerAction(e);
                pressedF = false;
            }

            // stop all
            if (Input.GetKey(KeyCode.Space))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(0, VRIL.Base.VRIL_ButtonType.Button2, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager.OnControllerAction(e);

                VRIL_ControllerActionEventArgs e2 = new VRIL_ControllerActionEventArgs(2, VRIL.Base.VRIL_ButtonType.Button2, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager.OnControllerAction(e2);
            }

            // rotation
            if (Input.GetKey(KeyCode.Q))
            {
                Viewpoint.transform.Rotate(0, -RotationFactor, 0);
            }

            if (Input.GetKey(KeyCode.E))
            {
                Viewpoint.transform.Rotate(0, RotationFactor, 0);
            }
            if (Input.GetKey(KeyCode.W))
            {
                if (SelectedController == Controller.LeftController)
                {
                    LeftHand.transform.position = new Vector3(LeftHand.transform.position.x, LeftHand.transform.position.y + MoveFactor, LeftHand.transform.position.z);
                }
                else if (SelectedController == Controller.RightController)
                {
                    RightHand.transform.position = new Vector3(RightHand.transform.position.x, RightHand.transform.position.y + MoveFactor, RightHand.transform.position.z);
                }
            }

            if (Input.GetKey(KeyCode.S))
            {
                if (SelectedController == Controller.LeftController)
                {
                    LeftHand.transform.position = new Vector3(LeftHand.transform.position.x, LeftHand.transform.position.y - MoveFactor, LeftHand.transform.position.z);
                }
                else if (SelectedController == Controller.RightController)
                {
                    RightHand.transform.position = new Vector3(RightHand.transform.position.x, RightHand.transform.position.y - MoveFactor, RightHand.transform.position.z);
                }
            }

            if (Input.GetKey(KeyCode.Y))
            {
                if (SelectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(-RotationFactor, 0, 0);
                }
                else if (SelectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(-RotationFactor, 0, 0);
                }
            }

            if (Input.GetKey(KeyCode.X))
            {
                if (SelectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(RotationFactor, 0, 0);
                }
                else if (SelectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(RotationFactor, 0, 0);
                }
            }

            if (Input.GetKey(KeyCode.A))
            {
                if (SelectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(0, -RotationFactor, 0);
                }
                else if (SelectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(0, -RotationFactor, 0);
                }
            }

            if (Input.GetKey(KeyCode.D))
            {
                if (SelectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(0, RotationFactor, 0);
                }
                else if (SelectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(0, RotationFactor, 0);
                }
            }

        }
        else
        {
            Debug.LogWarning("Controller, Manager or Viewpoint not set! No action is performed.");
        }
    }
}
