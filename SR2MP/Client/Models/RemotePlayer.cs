using UnityEngine;

namespace SR2MP.Client.Models;

public class RemotePlayer
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public DateTime LastUpdate { get; set; }
    public RemotePlayer(string playerId)
    {
        PlayerId = playerId;
        Position = Vector3.zero;
        Rotation = Quaternion.identity;
        LastUpdate = DateTime.UtcNow;
    }
}