using SR2MP.Client.Managers;
using UnityEngine;

namespace SR2MP;

public static class GlobalVariables
{
    internal static GameObject playerPrefab;

    public static readonly Dictionary<string, GameObject> playerObjects = new();

    public static RemotePlayerManager playerManager = new RemotePlayerManager();
}