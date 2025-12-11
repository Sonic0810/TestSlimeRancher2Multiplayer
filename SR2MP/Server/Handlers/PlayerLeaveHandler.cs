using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.S2C;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerLeave)]
public class PlayerLeaveHandler : BasePacketHandler
{
    private readonly ClientManager clientManager;

    public PlayerLeaveHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager)
    {
        this.clientManager = clientManager;
    }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        reader.Skip(1);

        string playerId = reader.ReadString();

        string clientInfo = $"{senderEndPoint.Address}:{senderEndPoint.Port}";

        SrLogger.LogSensitive($"Player leave request from {clientInfo} (PlayerId: {playerId})");
        SrLogger.Log($"Player leave request received (PlayerId: {playerId})");

        if (clientManager.RemoveClient(clientInfo))
        {
            var leavePacket = new BroadcastPlayerLeavePacket
            {
                Type = (byte)PacketType.BroadcastPlayerLeave,
                PlayerId = playerId
            };

            BroadcastToAll(leavePacket);

            SrLogger.LogSensitive($"Player {playerId} left from {clientInfo}");
            SrLogger.Log($"Player {playerId} left the server");
        }
        else
        {
            SrLogger.WarnSensitive($"Attempted to remove unknown client: {clientInfo}");
            SrLogger.Warn($"Player leave request from unknown client (PlayerId: {playerId})");
        }
    }
}