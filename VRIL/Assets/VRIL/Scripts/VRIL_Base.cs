
using VRIL.Callable;

namespace VRIL.Base
{
    /// <summary>
    /// All available SDKS
    /// </summary>
    public enum VRIL_LoadedSdkNames
    {
        None,
        OpenVR,
        Oculus,
        CustomSDK
    }

    /// <summary>
    /// Interaction Technique Function Types
    /// </summary>
    public enum VRIL_ActionTypes
    {
        None,
        OnSelection,
        OnActivation,
        OnInteraction,
        OnRelease,
        OnStop,
        CustomScript
    }

    /// <summary>
    /// Types of Buttons
    /// </summary>
    public enum VRIL_ButtonType
    {
        None,
        Button1,
        Button2,
        ButtonStart,
        Grip,
        Trigger,
        Touchpad
    }

    /// <summary>
    /// Button Interaction Types
    /// </summary>
    public enum VRIL_ButtonInteractionType
    {
        None,
        Pressed,
        NearTouch,
        Touched,
        PressedOnce,
        TouchedOnce,
        Released
    }

    public enum ObjectAttachmentTypes
    {
        NotAttached,
        AttachedToHand,
        HandToObject
    }

    public enum ObjectReleaseLocationType
    {
        CurrentLocation,
        BaseLocationWithBaseRotation,
        BaseLocationWithNewRotation,
        NewLocation
    }

    /// <summary>
    /// Used to map a controller action to an action type
    /// </summary>
    [System.Serializable]
    public class VRIL_ActionMapping
    {
        public VRIL_ButtonType ButtonType = VRIL_ButtonType.None;
        public VRIL_ActionTypes ActionType = VRIL_ActionTypes.None;
        public VRIL_Callable Script;
    }

    public delegate VRIL_ButtonInteractionType VRIL_ButtonDelegate();
}