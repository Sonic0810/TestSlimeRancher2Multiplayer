using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Shared;
using UnityEngine;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(GadgetDirector), nameof(GadgetDirector.AddBlueprint))]
    public static class BlueprintPatch
    {
        public static void Postfix(GadgetDirector __instance, GadgetDefinition blueprint)
        {
            if (GlobalVariables.handlingPacket) return;

            string id = blueprint.name;
            
            Main.SendToAllOrServer(new BlueprintUnlockPacket(id));
        }
    }
}
