using System.Net;
using System.Reflection;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Managers;

public class PacketManager
{
    private readonly Dictionary<byte, IPacketHandler> handlers = new();
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;

    public PacketManager(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                     && typeof(IPacketHandler).IsAssignableFrom(type)
                     && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null) continue;

            try
            {
                var handler = Activator.CreateInstance(type, networkManager, clientManager) as IPacketHandler;

                if (handler != null)
                {
                    handlers[attribute.PacketType] = handler;
                    SrLogger.LogSensitive($"Registered handler: {type.Name} for packet type {attribute.PacketType}");
                    SrLogger.Log($"Registered handler: {type.Name} for packet type {attribute.PacketType}");
                }
            }
            catch (Exception ex)
            {
                SrLogger.ErrorSensitive($"Failed to register handler {type.Name}: {ex}");
                SrLogger.Error($"Failed to register handler {type.Name}: {ex}");
            }
        }

        SrLogger.LogSensitive($"Total handlers registered: {handlers.Count}");
        SrLogger.Log($"Total handlers registered: {handlers.Count}");
    }

    public void HandlePacket(byte[] data, IPEndPoint clientEP)
    {
        if (data.Length < 1)
        {
            SrLogger.WarnSensitive("Received empty packet");
            SrLogger.Warn("Received empty packet");
            return;
        }

        byte packetType = data[0];

        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                handler.Handle(data, clientEP);
            }
            catch (Exception ex)
            {
                SrLogger.ErrorSensitive($"Error handling packet type {packetType}: {ex}");
                SrLogger.Error($"Error handling packet type {packetType}: {ex}");
            }
        }
        else
        {
            SrLogger.WarnSensitive($"No handler found for packet type: {packetType}");
            SrLogger.Warn($"No handler found for packet type: {packetType}");
        }
    }
}