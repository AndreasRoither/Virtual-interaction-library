using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRIL.Manager;
using VRIL.ControllerActionEventArgs;

public class VRIL_Keyboard : MonoBehaviour
{
    public VRIL_Manager Manager;

    public GameObject LeftHand = null;
    public GameObject RightHand = null;
    public GameObject AdditionalHand = null;

    public float RotationFactor = 1f;
    public float MoveFactor = 0.02f;

    public Text UIText;

    public bool pressedF = false;
    
    private enum Controller
    {
        LeftController,
        RightController,
        AdditionalController
    }

    private Controller selectedController = Controller.LeftController;

    void Start()
    {
        UIText.text = "Selected Controller: LeftController";
    }

    void Update()
    {

        if (LeftHand != null && RightHand != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                selectedController = Controller.LeftController;
                UIText.text = "Selected Controller: LeftController";
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                selectedController = Controller.RightController;
                UIText.text = "Selected Controller: RightController";
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                selectedController = Controller.AdditionalController;
                UIText.text = "Selected Controller: 3rd Controller";
            }

            if (Input.GetMouseButtonDown(0))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(0, VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);   
            }

            if (Input.GetMouseButtonUp(0))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(0, VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetMouseButtonDown(1))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(2, VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetMouseButtonUp(1))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(2, VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(1, VRIL.Base.VRIL_ButtonType.Touchpad, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetKeyDown(KeyCode.F) && pressedF == false)
            {
                if (selectedController == Controller.LeftController)
                {
                    VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(0, VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                    Manager?.OnControllerAction(e);
                }
                else if (selectedController == Controller.RightController)
                {
                    VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(1, VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                    Manager?.OnControllerAction(e);
                }
                else
                {
                    VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(2, VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                    Manager?.OnControllerAction(e);
                }

                pressedF = true;
            }

            if (Input.GetKeyUp(KeyCode.F) && pressedF)
            {
                if (selectedController == Controller.LeftController)
                {
                    VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(0, VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Released);
                    Manager?.OnControllerAction(e);
                }
                else if (selectedController == Controller.RightController)
                {
                    VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(1, VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Released);
                    Manager?.OnControllerAction(e);
                }
                else
                {
                    VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(2, VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Released);
                    Manager?.OnControllerAction(e);
                }

                pressedF = false;
            }

            // stop all
            if (Input.GetKey(KeyCode.Space))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(0, VRIL.Base.VRIL_ButtonType.Button2, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);

                VRIL_ControllerActionEventArgs e2 = new VRIL_ControllerActionEventArgs(2, VRIL.Base.VRIL_ButtonType.Button2, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e2);
            }

            // rotation
            if (Input.GetKey(KeyCode.Q))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(0, -RotationFactor, 0);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(0, -RotationFactor, 0);
                }
                else
                {
                    AdditionalHand.transform.Rotate(0, -RotationFactor, 0);
                }
            }

            if (Input.GetKey(KeyCode.E))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(0, RotationFactor, 0);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(0, RotationFactor, 0);
                }
                else
                {
                    AdditionalHand.transform.Rotate(0, RotationFactor, 0);
                }
            }
            // up down
            if (Input.GetKey(KeyCode.W))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.localPosition = new Vector3(LeftHand.transform.position.x, LeftHand.transform.position.y + MoveFactor, LeftHand.transform.position.z);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.localPosition = new Vector3(RightHand.transform.position.x, RightHand.transform.position.y + MoveFactor, RightHand.transform.position.z);
                }
                else
                {
                    AdditionalHand.transform.localPosition = new Vector3(AdditionalHand.transform.position.x, AdditionalHand.transform.position.y + MoveFactor, AdditionalHand.transform.position.z);
                }
            }

            if (Input.GetKey(KeyCode.S))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.localPosition = new Vector3(LeftHand.transform.position.x, LeftHand.transform.position.y - MoveFactor, LeftHand.transform.position.z);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.localPosition = new Vector3(RightHand.transform.position.x, RightHand.transform.position.y - MoveFactor, RightHand.transform.position.z);
                }
                else
                {
                    AdditionalHand.transform.localPosition = new Vector3(AdditionalHand.transform.position.x, AdditionalHand.transform.position.y - MoveFactor, AdditionalHand.transform.position.z);
                }
            }
        }
    }
}
