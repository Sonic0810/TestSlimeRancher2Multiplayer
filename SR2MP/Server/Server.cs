using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.S2C;

namespace SR2MP.Server;

public sealed class Server
{
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;
    private readonly PacketManager packetManager;
    private Timer? timeoutTimer;
    public int GetClientCount() => clientManager.ClientCount;
    public bool IsRunning() => networkManager.IsRunning;

    public Server()
    {
        networkManager = new NetworkManager();
        clientManager = new ClientManager();
        packetManager = new PacketManager(networkManager, clientManager);

        networkManager.OnDataReceived += OnDataReceived;
        clientManager.OnClientRemoved += OnClientRemoved;
    }

    public void Start(int port)
    {
        if (networkManager.IsRunning)
        {
            SrLogger.LogSensitive("Server is already running!");
            SrLogger.Log("Server is already running!");
            return;
        }

        try
        {
            packetManager.RegisterHandlers();
            networkManager.Start(port);

            timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            SrLogger.LogSensitive($"Server started successfully on port {port}");
            SrLogger.Log($"Server started successfully on port {port}");
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Failed to start server: {ex}");
            SrLogger.Error($"Failed to start server: {ex}");
        }
    }

    private void OnDataReceived(byte[] data, System.Net.IPEndPoint clientEP)
    {
        packetManager.HandlePacket(data, clientEP);
    }

    private void OnClientRemoved(Models.ClientInfo client)
    {
        var leavePacket = new BroadcastPlayerLeavePacket
        {
            Type = (byte)PacketType.BroadcastPlayerLeave,
            PlayerId = client.PlayerId
        };

        using var writer = new PacketWriter();
        leavePacket.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var otherClient in clientManager.GetAllClients())
        {
            networkManager.Send(data, otherClient.EndPoint);
        }

        SrLogger.LogSensitive($"Player left broadcast sent for: {client.PlayerId}");
        SrLogger.Log($"Player left broadcast sent for: {client.PlayerId}");
    }

    private void CheckTimeouts(object? state)
    {
        try
        {
            clientManager.RemoveTimedOutClients();
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Error checking timeouts: {ex}");
            SrLogger.Error($"Error checking timeouts: {ex}");
        }
    }

    public void Close()
    {
        if (!networkManager.IsRunning)
            return;

        try
        {
            timeoutTimer?.Dispose();
            timeoutTimer = null;

            var closePacket = new ClosePacket
            {
                Type = (byte)PacketType.Close
            };

            using var writer = new PacketWriter();
            closePacket.Serialise(writer);
            byte[] data = writer.ToArray();

            foreach (var client in clientManager.GetAllClients())
            {
                try
                {
                    networkManager.Send(data, client.EndPoint);
                }
                catch (Exception ex)
                {
                    SrLogger.WarnSensitive($"Failed to send close packet to client: {client.GetClientInfo()}: {ex}");
                    SrLogger.Warn($"Failed to notify specific client of server shutdown: {ex}");
                }
            }
            clientManager.Clear();
            networkManager.Stop();

            SrLogger.LogSensitive("Server closed");
            SrLogger.Log("Server closed");
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Error during server shutdown: {ex}");
            SrLogger.Error($"Error during server shutdown: {ex}");
        }
    }
}