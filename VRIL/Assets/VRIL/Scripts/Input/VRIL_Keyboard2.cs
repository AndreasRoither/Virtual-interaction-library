﻿using System.Collections;
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

    private enum Controller
    {
        LeftController,
        RightController,
    }

    private Controller selectedController = Controller.LeftController;
    private readonly Dictionary<Controller, int> ControllerNumberMapping = new Dictionary<Controller, int>();

    void Start()
    {
        UIText.text = "Selected Controller: LeftController";
        ControllerNumberMapping[Controller.LeftController] = 0;
        ControllerNumberMapping[Controller.RightController] = 1;
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

            if (Input.GetMouseButtonDown(0))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[selectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetMouseButtonUp(0))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[selectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetMouseButtonDown(1))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[selectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetMouseButtonUp(1))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[selectedController], VRIL.Base.VRIL_ButtonType.Button1, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[selectedController], VRIL.Base.VRIL_ButtonType.Touchpad, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);
            }

            if (Input.GetKeyDown(KeyCode.F) && pressedF == false)
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[selectedController], VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Pressed);
                Manager?.OnControllerAction(e);
                pressedF = true;
            }
            if (Input.GetKeyUp(KeyCode.F) && pressedF)
            {
                VRIL_ControllerActionEventArgs e = new VRIL_ControllerActionEventArgs(ControllerNumberMapping[selectedController], VRIL.Base.VRIL_ButtonType.Trigger, VRIL.Base.VRIL_ButtonInteractionType.Released);
                Manager?.OnControllerAction(e);
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
                Viewpoint?.transform.Rotate(0, -RotationFactor, 0);
            }

            if (Input.GetKey(KeyCode.E))
            {
                Viewpoint?.transform.Rotate(0, RotationFactor, 0);
            }
            if (Input.GetKey(KeyCode.W))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.position = new Vector3(LeftHand.transform.position.x, LeftHand.transform.position.y + MoveFactor, LeftHand.transform.position.z);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.position = new Vector3(RightHand.transform.position.x, RightHand.transform.position.y + MoveFactor, RightHand.transform.position.z);
                }
            }

            if (Input.GetKey(KeyCode.S))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.position = new Vector3(LeftHand.transform.position.x, LeftHand.transform.position.y - MoveFactor, LeftHand.transform.position.z);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.position = new Vector3(RightHand.transform.position.x, RightHand.transform.position.y - MoveFactor, RightHand.transform.position.z);
                }
            }

            if (Input.GetKey(KeyCode.Y))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(-RotationFactor, 0, 0);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(-RotationFactor, 0, 0);
                }
            }

            if (Input.GetKey(KeyCode.X))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(RotationFactor, 0, 0);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(RotationFactor, 0, 0);
                }
            }

            if (Input.GetKey(KeyCode.A))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(0, -RotationFactor, 0);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(0, -RotationFactor, 0);
                }
            }

            if (Input.GetKey(KeyCode.D))
            {
                if (selectedController == Controller.LeftController)
                {
                    LeftHand.transform.Rotate(0, RotationFactor, 0);
                }
                else if (selectedController == Controller.RightController)
                {
                    RightHand.transform.Rotate(0, RotationFactor, 0);
                }
            }

        }
    }
}
