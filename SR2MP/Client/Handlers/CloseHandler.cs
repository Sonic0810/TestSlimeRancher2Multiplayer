using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.Close)]
public class CloseHandler : BaseClientPacketHandler
{
    public CloseHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager)
    {
    }

    public override void Handle(byte[] data)
    {
        SrLogger.Log("Server closed, disconnecting!");
        SrLogger.LogSensitive("Server closed, disconnecting!");

        Client.Disconnect();
    }
}