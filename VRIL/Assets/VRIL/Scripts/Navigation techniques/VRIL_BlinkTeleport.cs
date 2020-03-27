using UnityEngine;
using Valve.VR;
using VRIL.ControllerActionEventArgs;

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

        [Header("Blink Settings")] [Tooltip("Turn scene off for a moment")]
        public bool SceneBlinksAway = true;

        [Tooltip("Set how many seconds should the scene be turned off")]
        public float SceneOffDuration = 1.0f;

        [Tooltip("Set how long the fade-in effect should take")]
        public float FadeInDuration = 1.0f;

        [Tooltip("Choose the color that will be shown while the scene is off")]
        public Color SceneOffColor = Color.black;


        // *************************************
        // private members
        // *************************************

        private bool SceneOff = false;
        private float SceneOffTimer = 0.0f;


        /// <summary>
        /// Called when the travel should be performed
        /// </summary>
        /// <param name="e">ControllerActionEventArgs</param>
        public override void OnTravel(VRIL_ControllerActionEventArgs e)
        {
            if (IsActivated && !DelayToNextTravel)
            {
                // if a valid position is selected, travel is triggered
                if (PositionSelected)
                {
                    PlayAudio();
                    SaveDistancesToViewpoint();
                    Viewpoint.transform.position = TargetPosition;
                    TransferSelectedObjects();
                    PositionSelected = false;
                    Timer = 0.0f;
                    DelayToNextTravel = true;

                    // blink scene away immediately
                    if (SceneBlinksAway)
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

                // in case travel does not disabled technique, selection mode is actived again
                if (!TravelDisablesTechnique)
                {
                    IsActivated = true;
                    StartCoroutine(SelectPosition(e));
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            if (SceneOff)
            {
                SceneOffTimer += Time.deltaTime;

                // after a certain time, the scene fades in with the provided duration
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