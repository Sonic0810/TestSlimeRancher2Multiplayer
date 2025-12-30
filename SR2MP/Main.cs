using Il2CppTMPro;
using MelonLoader;
using SR2E.Expansion;
using SR2MP.Components.FX;
using SR2MP.Components.Player;
using SR2MP.Components.Time;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.Script.UI.Pause;
using SR2MP.Server;
using SR2MP.Client;

namespace SR2MP;

public sealed class Main : SR2EExpansionV3
{
    public static string Username = "Player";
    public static bool IsLoadingMultiplayerSave = false;
    public static bool PacketSizeLogging = false;
    public static Server.Server Server { get; private set; }
    public static Client.Client Client { get; private set; }

    public override void OnInitializeMelon()
    {
        Server = new Server.Server();
        Client = new Client.Client();
        SrLogger.LogMessage("SR2MP Initialized");
    }

    public static void SendToAllOrServer<T>(T packet) where T : IPacket
    {
        if (Client != null && Client.IsConnected)
        {
            Client.SendPacket(packet);
        }

        if (Server != null && Server.IsRunning())
        {
            Server.SendToAll(packet);
        }
    }
    
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "SystemCore":
                MainThreadDispatcher.Initialize();

                var forceTimeScale = new GameObject("SR2MP_TimeScale").AddComponent<ForceTimeScale>();
                Object.DontDestroyOnLoad(forceTimeScale.gameObject);
                break;

            case "MainMenuEnvironment":
                playerPrefab = new GameObject("PLAYER");
                playerPrefab.SetActive(false);
                playerPrefab.transform.localScale = Vector3.one * 0.85f;

                var audio = playerPrefab.AddComponent<SECTR_PointSource>();
                audio.instance = new SECTR_AudioCueInstance();

                var networkComponent = playerPrefab.AddComponent<NetworkPlayer>();

                var playerModel = Object.Instantiate(GameObject.Find("BeatrixMainMenu")).transform;
                playerModel.parent = playerPrefab.transform;
                playerModel.localPosition = Vector3.zero;
                playerModel.localRotation = Quaternion.identity;
                playerModel.localScale = Vector3.one;

                var name = new GameObject("Username")
                {
                    transform = { parent = playerPrefab.transform, localPosition = Vector3.up * 3 }
                };

                var textComponent = name.AddComponent<TextMeshPro>();

                networkComponent.usernamePanel = textComponent;

                var footstepFX = new GameObject("Footstep") { transform = { parent = playerPrefab.transform } };
                playerPrefab.AddComponent<NetworkPlayerFootstep>().spawnAtTransform = footstepFX.transform;

                Object.DontDestroyOnLoad(playerPrefab);
                break;
        }

        // Debug logging for join finalization
        SrLogger.LogMessage($"OnSceneWasLoaded: scene={sceneName}, IsLoadingMultiplayerSave={IsLoadingMultiplayerSave}, Client.PendingJoin={(Client?.PendingJoin != null ? "SET" : "NULL")}");

        if (IsLoadingMultiplayerSave && !sceneName.Equals("MainMenuEnvironment") && !sceneName.Equals("SystemCore") && !sceneName.Equals("LoadScene"))
        {
            if (Client != null && Client.PendingJoin != null)
            {
                SrLogger.LogMessage($"Multiplayer Save Loaded in {sceneName}! Finalizing Join...", SrLogger.LogTarget.Both);
                IsLoadingMultiplayerSave = false;

                var pd = Client.PendingJoin;
                
                // Restore Currency
                if(GameContext.Instance.LookupDirector != null && SceneContext.Instance.PlayerState != null)
                {
                    SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>(), pd.Money);
                    SceneContext.Instance.PlayerState._model.SetCurrency(GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>(), pd.RainbowMoney);
                }

                // Spawn Players
                foreach (var player in pd.OtherPlayers)
                {
                    // Filter out the local player ID so we don't spawn a ghost of ourselves
                    if (player == pd.PlayerId)
                    {
                        SrLogger.LogMessage($"Skipping local player proxy spawn: {player}", SrLogger.LogTarget.Both);
                        continue;
                    }

                    var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
                    playerObject.gameObject.SetActive(true);
                    playerObject.ID = player;
                    playerObject.gameObject.name = player;
                    playerObjects[player] = playerObject.gameObject; // Use indexer to add or update
                    playerManager.AddPlayer(player);
                    Object.DontDestroyOnLoad(playerObject);
                }

                // Send Join Packet
                var joinPacket = new PlayerJoinPacket
                {
                    Type = (byte)PacketType.PlayerJoin,
                    PlayerId = pd.PlayerId,
                    PlayerName = Username // Username is from SR2EExpansionV3
                };

                Client.SendPacket(joinPacket);
                Client.StartHeartbeat();
                Client.NotifyConnected();

                Client.PendingJoin = null;
                
                // Teleport player to valid spawn point after a short delay
                // This fixes the player spawning under the map due to save loading
                MelonLoader.MelonCoroutines.Start(TeleportPlayerAfterDelay());
            }
        }
    }
    
    private static System.Collections.IEnumerator TeleportPlayerAfterDelay()
    {
        // Wait for the world to stabilize
        yield return new WaitForSeconds(3f);
        
        SRCharacterController controller = null;
        bool wasControllerEnabled = false;

        try
        {
            // Force disable scene load in progress if it got stuck (via reflection)
            var sceneLoader = SystemContext.Instance?.SceneLoader;
            if (sceneLoader != null)
            {
                foreach (var field in sceneLoader.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.Name.ToLower().Contains("sceneloadinprogress"))
                    {
                        field.SetValue(sceneLoader, false);
                        SrLogger.LogMessage($"Forced field {field.Name} to false on SceneLoader.", SrLogger.LogTarget.Both);
                    }
                }
            }

            var player = SceneContext.Instance?.Player;
            if (player != null)
            {
                controller = player.GetComponent<SRCharacterController>();
                wasControllerEnabled = controller != null && controller.enabled;

                // Temporarily disable the character controller to ensure teleport works consistently
                if (controller != null) controller.enabled = false;

                // Teleport to spawn point
                var spawnPoint = GameObject.Find("PlayerSpawnPoint");
                if (spawnPoint != null)
                {
                    player.transform.position = spawnPoint.transform.position + Vector3.up * 2f;
                    SrLogger.LogMessage($"Teleported player to spawn point: {player.transform.position}", SrLogger.LogTarget.Both);
                }
                else
                {
                    player.transform.position = new Vector3(-70f, 15f, 2f);
                    SrLogger.LogMessage($"Teleported player to fallback position: {player.transform.position}", SrLogger.LogTarget.Both);
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Teleport Error: {ex}", SrLogger.LogTarget.Both);
        }

        // Wait a frame then re-enable (OUTSIDE try-catch)
        yield return null;

        try
        {
            if (controller != null && wasControllerEnabled) controller.enabled = true;

            // Fix input/paused state
            var inputDirector = GameContext.Instance?.InputDirector;
            if (inputDirector != null)
            {
                // Disable all pause-related maps
                if (inputDirector._paused?.Map != null)
                {
                    inputDirector._paused.Map.Disable();
                    SrLogger.LogMessage("Disabled pause input map.", SrLogger.LogTarget.Both);
                }

                // Deactivate potential blocking UIs
                string[] uiNames = { "PauseMenu", "PauseMenuDirector", "LoadingOverlay", "LoadScene", "LoadingCanvas", "ModSMLoadingScreen" };
                foreach (var name in uiNames)
                {
                    var ui = GameObject.Find(name);
                    if (ui != null)
                    {
                        ui.SetActive(false);
                        SrLogger.LogMessage($"Deactivated UI: {name}", SrLogger.LogTarget.Both);
                    }
                }

                // Attempt to find and enable a gameplay-like map via reflection
                SrLogger.LogMessage("Searching for gameplay input maps...", SrLogger.LogTarget.Both);
                var allFields = inputDirector.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var allProperties = inputDirector.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in allFields)
                {
                    TryEnableMapFromObject(field.GetValue(inputDirector), field.Name, "Field");
                }
                foreach (var prop in allProperties)
                {
                    try { TryEnableMapFromObject(prop.GetValue(inputDirector), prop.Name, "Property"); } catch { }
                }
            }

            UnityEngine.Time.timeScale = 1f;

            // Final safety check: if we were stuck loading, the above reflection should have fixed it, 
            // but let's log the final state.
            SrLogger.LogMessage($"Final Join Phase: TimeScale={UnityEngine.Time.timeScale}", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Post-Teleport Error: {ex}", SrLogger.LogTarget.Both);
        }
    }

    private static void TryEnableMapFromObject(object obj, string memberName, string memberType)
    {
        if (obj == null) return;
        
        string nameLower = memberName.ToLower();
        // Keywords that usually indicate the main gameplay/player input map
        if (nameLower.Contains("gameplay") || nameLower.Contains("default") || nameLower.Contains("player") || nameLower.Contains("ingame"))
        {
            try
            {
                var mapProperty = obj.GetType().GetProperty("Map") ?? obj.GetType().GetProperty("map");
                if (mapProperty != null)
                {
                    var map = mapProperty.GetValue(obj);
                    if (map != null)
                    {
                        var enableMethod = map.GetType().GetMethod("Enable");
                        if (enableMethod != null)
                        {
                            enableMethod.Invoke(map, null);
                            SrLogger.LogMessage($"SUCCESS: Enabled input map via reflection: [{memberType}] {memberName}", SrLogger.LogTarget.Both);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to enable map on {memberName}: {ex.Message}");
            }
        }
    }






    public override void AfterGameContext(GameContext gameContext)
    {
        actorManager.Initialize(gameContext);
    }
}