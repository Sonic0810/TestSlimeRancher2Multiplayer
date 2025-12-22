using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using SR2MP.Components;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(SRCharacterController), nameof(SRCharacterController.Awake))]
public static class OnPlayerLoadPatch
{
    public static void Postfix(SRCharacterController __instance)
    {
        var player = __instance.gameObject.AddComponent<NetworkPlayer>();
        // Temporary "HOST" because no fix
        player.ID = Main.Client.IsConnected ? Main.Client.OwnPlayerId : "HOST";
        player.IsLocal = true;
    }
}