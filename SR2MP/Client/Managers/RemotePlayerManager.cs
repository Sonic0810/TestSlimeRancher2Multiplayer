using System.Collections.Concurrent;
using SR2MP.Client.Models;

namespace SR2MP.Client.Managers;

public class RemotePlayerManager
{
    private readonly ConcurrentDictionary<string, RemotePlayer> players = new();

    public event Action<string>? OnPlayerAdded;
    public event Action<string>? OnPlayerRemoved;
    public event Action<string, RemotePlayer>? OnPlayerUpdated;

    public int PlayerCount => players.Count;

    public RemotePlayer? GetPlayer(string playerId)
    {
        players.TryGetValue(playerId, out var player);
        return player;
    }

    public RemotePlayer AddPlayer(string playerId)
    {
        var player = new RemotePlayer(playerId);

        if (players.TryAdd(playerId, player))
        {
            SrLogger.LogSensitive($"Remote player added: {playerId}");
            SrLogger.Log($"Remote player added: {playerId}");
            OnPlayerAdded?.Invoke(playerId);
            return player;
        }
        else
        {
            SrLogger.LogSensitive($"Remote player already exists: {playerId}");
            SrLogger.Log($"Remote player already exists: {playerId}");
            return players[playerId];
        }
    }

    public bool RemovePlayer(string playerId)
    {
        if (players.TryRemove(playerId, out var player))
        {
            SrLogger.LogSensitive($"Remote player removed: {playerId}");
            SrLogger.Log($"Remote player removed: {playerId}");
            OnPlayerRemoved?.Invoke(playerId);
            return true;
        }
        return false;
    }

    public void UpdatePlayer(string playerId, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
    {
        if (players.TryGetValue(playerId, out var player))
        {
            player.Position = position;
            player.Rotation = rotation;
            player.LastUpdate = DateTime.UtcNow;

            OnPlayerUpdated?.Invoke(playerId, player);
        }
    }

    public List<RemotePlayer> GetAllPlayers()
    {
        return players.Values.ToList();
    }

    public void Clear()
    {
        var allPlayers = players.Keys.ToList();
        players.Clear();

        foreach (var playerId in allPlayers)
        {
            OnPlayerRemoved?.Invoke(playerId);
        }

        SrLogger.LogSensitive("All remote players cleared!");
        SrLogger.Log("All remote players cleared!");
    }
}