using System.Collections;
using UnityEngine;
using VRIL.Base;
using VRIL.ControllerActionEventArgs;
using VRIL.Interactable;
using VRIL.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using Valve.VR;

namespace VRIL.NavigationTechniques
{

    /// <summary>
    /// Implementation of a teleport technique which allows to blink the scene away during travel
    /// </summary>
    public class VRIL_BlinkTeleport : VRIL_TeleportBase
    {


        // *************************************
        // public properties
        // *************************************

        [Header("Blink Settings")]
        [Tooltip("Turn scene off for a moment")]
        public bool SceneBlinksAway = true;
        [Tooltip("Set how many seconds should the scene be turned off")]
        public float SceneOffDuration = 1.0f;
        [Tooltip("Set how long the fade in effect should take")]
        public float FadeInDuration = 1.0f;
        [Tooltip("Choose the color that will be shown while the scene is off")]
        public Color SceneOffColor = Color.black;


        // *************************************
        // private and protected members
        // *************************************

        private bool SceneOff = false;
        protected float SceneOffTimer = 0.0f;


        /// <summary>
        /// Called when the travel should be performed
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if(IsActivated)
            {
                if (PositionSelected && TravelPauseTimer > SecondsToWaitForNextTeleport)
                {
                    PlayAudio();
                    InitDistancesToViewpoint();
                    Viewpoint.transform.position = SelectedPosition;
                    UpdateObjects();
                    PositionSelected = false;
                    TravelPauseTimer = 0.0f;
                    if(SceneBlinksAway)
                    {
                        SteamVR_Fade.View(SceneOffColor, 0);
                        SceneOff = true;
                    }
                }
                if (HitEntity != null)
                {
                    HitEntity.SetActive(false);
                }
                IsActivated = false;
                if (!OnTravelDisablesTechnique)
                {
                    IsActivated = true;
                    StartCoroutine(ShowRay(e));
                }
            }
        }

        protected override void Update()
        {
            TravelPauseTimer += Time.deltaTime;
            if (SceneOff)
            {
                SceneOffTimer += Time.deltaTime;
                if (SceneOffTimer >= SceneOffDuration)
                {
                    SteamVR_Fade.View(Color.clear, FadeInDuration);
                    SceneOff = false;
                    SceneOffTimer = 0.0f;
                }
            }
        }
    }
}