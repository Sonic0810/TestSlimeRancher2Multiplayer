using System.Reflection;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Managers;

public class ClientPacketManager
{
    private readonly Dictionary<byte, IClientPacketHandler> handlers = new();
    private readonly Client client;
    private readonly RemotePlayerManager playerManager;

    public ClientPacketManager(Client client, RemotePlayerManager playerManager)
    {
        this.client = client;
        this.playerManager = playerManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                     && typeof(IClientPacketHandler).IsAssignableFrom(type)
                     && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null) continue;

            try
            {
                var handler = Activator.CreateInstance(type, client, playerManager) as IClientPacketHandler;

                if (handler != null)
                {
                    handlers[attribute.PacketType] = handler;
                    SrLogger.LogSensitive($"Registered client handler: {type.Name} for packet type {attribute.PacketType}");
                    SrLogger.Log($"Registered client handler: {type.Name} for packet type {attribute.PacketType}");
                }
            }
            catch (Exception ex)
            {
                SrLogger.WarnSensitive($"Failed to register client handler {type.Name}: {ex}");
                SrLogger.Warn($"Failed to register client handler {type.Name}: {ex}");
            }
        }

        SrLogger.LogSensitive($"Total client handlers registered: {handlers.Count}");
        SrLogger.Log($"Total cliend handlers registered: {handlers.Count}");
    }

    public void HandlePacket(byte[] data)
    {
        if (data.Length < 1)
        {
            SrLogger.LogSensitive("Received empty packet");
            SrLogger.Log("Received empty packet");
            return;
        }

        byte packetType = data[0];

        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                handler.Handle(data);
            }
            catch (Exception ex)
            {
                SrLogger.ErrorSensitive($"Error handling packet type {packetType}: {ex}");
                SrLogger.Error($"Error handling packet type {packetType}: {ex}");
            }
        }
        else
        {
            SrLogger.ErrorSensitive($"No client handler found for packet type: {packetType}");
            SrLogger.Error($"No client handler found for packet type: {packetType}");
        }
    }
}