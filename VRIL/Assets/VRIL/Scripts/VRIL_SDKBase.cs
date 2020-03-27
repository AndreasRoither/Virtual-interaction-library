using UnityEngine;
using VRIL.Base;

namespace VRIL.SDKBase
{
    /// <summary>
    /// Abstract class for SDK scripts, used by VRIL_Manager
    /// </summary>
    /// <see cref="VRIL.Manager"/>
    [System.Serializable]
    public abstract class VRIL_SDKBase : MonoBehaviour
    {
        public abstract VRIL_ButtonInteractionType GetButtonState(VRIL_ButtonType button, GameObject controller);
    }
}