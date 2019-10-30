using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Manager;

namespace VRIL.TechniqueBase
{
    /// <summary>
    /// Base class for all VRIL Techniques
    /// </summary>
    public abstract class VRIL_TechniqueBase : MonoBehaviour
    {
        [Header("VRIL Manager")]
        public VRIL_Manager Manager;

        [Header("Controller - Action Mapping")]
        public List<VRIL_ActionMapping> Mappings = new List<VRIL_ActionMapping>();

        protected List<GameObject> RegisteredControllers = new List<GameObject>();

        public void Initialize()
        {
            if (Manager != null)
            {
                RegisteredControllers = Manager.GetRegisteredControllers(this);
            }
            else
            {
                Debug.LogWarning($"<b>VRIL:</b>\nManager not assigned at {this.GetType().Name}");
            }
        }

        /// <summary>
        /// Called from VRIL_Manager when a button is pressed
        /// </summary>
        /// <param name="sender">sending class</param>
        /// <param name="e">EventArgs that contains the controller, buttonType, buttonInteractionType</param>
        /// <see cref="VRIL_Manager"/>
        /// <seealso cref="VRIL_ControllerActionEventArgs"/>
        public abstract void ControllerAction(object sender, VRIL_ControllerActionEventArgs e);

        /// <summary>
        /// Called when the technique is activated
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public abstract void OnActivation(VRIL_ControllerActionEventArgs e);
    }
}
