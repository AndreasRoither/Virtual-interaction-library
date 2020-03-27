using System;
using VRIL.Base;

namespace VRIL.ControllerActionEventArgs
{
    /// <summary>
    /// ControllerActionEventArgs thats holds all necessary fields for an interaction technique
    /// </summary>
    public class VRIL_ControllerActionEventArgs : EventArgs
    {
        public int ControllerIndex { get; set; }
        public VRIL_ButtonType ButtonType { get; set; }
        public VRIL_ButtonInteractionType ButtonInteractionType { get; set; }
        public float ButtonPressStrength { get; set; }

        public VRIL_ControllerActionEventArgs()
        {
        }

        public VRIL_ControllerActionEventArgs(int controllerIndex, VRIL_ButtonType buttonType,
            VRIL_ButtonInteractionType interactionType)
        {
            ControllerIndex = controllerIndex;
            ButtonType = buttonType;
            ButtonInteractionType = interactionType;
            ButtonPressStrength = 0f;
        }

        public VRIL_ControllerActionEventArgs(int controllerIndex, VRIL_ButtonType buttonType,
            VRIL_ButtonInteractionType interactionType, float buttonPressStrength)
        {
            ControllerIndex = controllerIndex;
            ButtonType = buttonType;
            ButtonInteractionType = interactionType;
            ButtonPressStrength = buttonPressStrength;
        }
    }
}