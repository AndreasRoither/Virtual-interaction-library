
using UnityEngine;

namespace VRIL.Callable
{
    /// <summary>
    /// Abstract class for callable scripts, used by VRIL_Interactable
    /// </summary>
    /// <see cref="VRIL.VRIL_Interactable"/>
    [System.Serializable]
    public abstract class VRIL_Callable : MonoBehaviour
    {
        /// <summary>
        /// Function that is called when something happens like selection, interaction or release
        /// </summary>
        public abstract void OnCall();

        /// <summary>
        /// Function that is called when Action should be stopped, for example a button is no longer pressed
        /// </summary>
        public abstract void OnStop();
    }
}