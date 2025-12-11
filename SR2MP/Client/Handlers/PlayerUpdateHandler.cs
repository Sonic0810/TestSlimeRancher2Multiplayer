using SR2MP.Client.Managers;
using SR2MP.Packets.C2S;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerUpdate)]
public class PlayerUpdateHandler : BaseClientPacketHandler
{
    public PlayerUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager)
    {
    }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerUpdatePacket>();

        // this should not even happen but just in case
        if (packet.PlayerId == Client.OwnPlayerId)
            return;

        PlayerManager.UpdatePlayer(packet.PlayerId, packet.Position, packet.Rotation);
    }
}