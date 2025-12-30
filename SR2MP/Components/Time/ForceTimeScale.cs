using MelonLoader;

namespace SR2MP.Components.Time;

[RegisterTypeInIl2Cpp(false)]
public sealed class ForceTimeScale : MonoBehaviour
{
    public float timeScale = 1f;
    public float loadingTimeScale = 0f;
    
    private float stuckTimer = 0f;
    private bool hasLoggedStuck = false;

    private void Update()
    {
        if (Main.Server.IsRunning() || Main.Client.IsConnected)
        {
            if (GameContext.Instance.InputDirector._paused.Map.enabled)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            var loading = SystemContext.Instance.SceneLoader.IsSceneLoadInProgress;
            
            // Track if loading is stuck
            if (loading)
            {
                stuckTimer += UnityEngine.Time.unscaledDeltaTime;
                if (stuckTimer > 10f && !hasLoggedStuck)
                {
                    MelonLoader.MelonLogger.Warning($"ForceTimeScale: IsSceneLoadInProgress has been true for 10+ seconds. Forcing time scale to 1.");
                    hasLoggedStuck = true;
                }
                
                // Force time scale to 1 if stuck loading for too long
                if (stuckTimer > 10f)
                {
                    UnityEngine.Time.timeScale = timeScale;
                    return;
                }
            }
            else
            {
                stuckTimer = 0f;
                hasLoggedStuck = false;
            }
            
            UnityEngine.Time.timeScale = loading ? loadingTimeScale : timeScale;
        }
    }
}
