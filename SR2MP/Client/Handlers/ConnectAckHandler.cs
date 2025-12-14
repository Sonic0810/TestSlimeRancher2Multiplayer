using SR2MP.Client.Managers;
using SR2MP.Packets.S2C;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

public sealed class ConnectAckHandler : BaseClientPacketHandler
{
    public ConnectAckHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager)
    {
    }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ConnectAckPacket>();
        packet.Deserialise(reader);

        if (Client.IsConnected)
        {
            SrLogger.LogWarning("Received duplicate ConnectAckPacket, ignoring.", SrLogger.LogTarget.Both);
            return;
        }

        // Client.StartHeartbeat();
        Client.NotifyConnected();

        SrLogger.LogMessage($"Connection acknowledged by server! (PlayerId: {packet.PlayerId})", SrLogger.LogTarget.Both);
    }
}