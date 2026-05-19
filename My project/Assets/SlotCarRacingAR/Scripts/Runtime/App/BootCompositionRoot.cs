using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotCarRacingAR.Runtime.App
{
    /// <summary>
    /// Boot scene composition root. Initializes global prerequisites
    /// and routes into the next flow state (Lobby).
    /// </summary>
    public sealed class BootCompositionRoot : MonoBehaviour
    {
        private void Start()
        {
            InitializePrerequisites();
            RouteToLobby();
        }

        private void InitializePrerequisites()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ConfigureScreenOrientation();
            // Future: telemetry initialization, config loading, etc.
        }

        private void ConfigureScreenOrientation()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        private void RouteToLobby()
        {
            SceneManager.LoadScene("Lobby");
        }
    }
}
