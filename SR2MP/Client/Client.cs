using System.Net;
using System.Net.Sockets;
using SR2MP.Client.Managers;
using SR2MP.Client.Models;
using SR2MP.Packets.C2S;
using SR2MP.Packets.S2C;
using SR2MP.Packets.Utils;

namespace SR2MP.Client;

public sealed class Client
{
    private UdpClient? udpClient;
    private IPEndPoint? serverEndPoint;
    private Thread? receiveThread;
    private Timer? heartbeatTimer;
    private volatile bool isConnected;

    private readonly ClientPacketManager packetManager;
    private readonly RemotePlayerManager playerManager;

    public string OwnPlayerId { get; private set; } = string.Empty;
    public bool IsConnected => isConnected;

    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnPlayerLeft;
    public event Action<string, RemotePlayer>? OnPlayerUpdate;

    public Client()
    {
        playerManager = new RemotePlayerManager();
        packetManager = new ClientPacketManager(this, playerManager);

        playerManager.OnPlayerAdded += (playerId) => OnPlayerJoined?.Invoke(playerId);
        playerManager.OnPlayerRemoved += (playerId) => OnPlayerLeft?.Invoke(playerId);
        playerManager.OnPlayerUpdated += (playerId, player) => OnPlayerUpdate?.Invoke(playerId, player);
    }

    public void Connect(string serverIp, int port, string playerId)
    {
        if (isConnected)
        {
            SrLogger.LogSensitive("You are already connected to a Server!");
            SrLogger.Log("You are already connected to a Server!");
            return;
        }

        try
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), port);

            OwnPlayerId = playerId;

            packetManager.RegisterHandlers();

            isConnected = true;
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            var connectPacket = new ConnectPacket
            {
                Type = (byte)PacketType.Connect,
                PlayerId = playerId
            };

            SendPacket(connectPacket);

            SrLogger.LogSensitive($"Connecting to {serverIp}:{port} as {playerId}...");
            SrLogger.Log("Connecting to the Server...");
        }
        catch (Exception ex)
        {

            SrLogger.ErrorSensitive($"Error connecting to the Server: {ex}");
            SrLogger.Error($"Error connecting to the Server: {ex}");
            isConnected = false;
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogSensitive("UDP client is null in ReceiveLoop!");
            SrLogger.Log("UDP client is null in ReceiveLoop!");
            return;
        }

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isConnected)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);

                if (data.Length > 0)
                {
                    packetManager.HandlePacket(data);
                }
            }
            catch (SocketException)
            {
                if (!isConnected)
                    return;
            }
            catch (Exception ex)
            {
                SrLogger.ErrorSensitive($"ReceiveLoop error: {ex}");
                SrLogger.Error($"ReceiveLoop error: {ex}");
            }
        }
    }

    public void SendPlayerUpdate(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
    {
        if (!isConnected || string.IsNullOrEmpty(OwnPlayerId))
            return;

        var updatePacket = new PlayerUpdatePacket
        {
            Type = (byte)PacketType.PlayerUpdate,
            PlayerId = OwnPlayerId,
            Position = position,
            Rotation = rotation
        };

        SendPacket(updatePacket);
    }

    internal void StartHeartbeat()
    {
        heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private void SendHeartbeat(object? state)
    {
        if (!isConnected)
            return;

        var heartbeatPacket = new HeartbeatPacket
        {
            Type = (byte)PacketType.Heartbeat
        };

        SendPacket(heartbeatPacket);
    }

    internal void SendPacket(IPacket packet)
    {
        if (udpClient == null || serverEndPoint == null || !isConnected)
        {
            SrLogger.WarnSensitive("Cannot send packet: Not connected to a Server!");
            SrLogger.Warn("Cannot send packet: Not connected to a Server!");
            return;
        }

        try
        {
            using var writer = new PacketWriter();
            packet.Serialise(writer);
            byte[] data = writer.ToArray();

            udpClient.Send(data, data.Length, serverEndPoint);
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Failed to send packet: {ex}");
            SrLogger.Error($"Failed to send packet: {ex}");
        }
    }

    public void Disconnect()
    {
        if (!isConnected)
            return;

        try
        {
            var leavePacket = new PlayerLeavePacket
            {
                Type = (byte)PacketType.PlayerLeave,
                PlayerId = OwnPlayerId
            };

            SendPacket(leavePacket);

            heartbeatTimer?.Dispose();
            heartbeatTimer = null;

            isConnected = false;
            udpClient?.Close();

            if (receiveThread != null && receiveThread.IsAlive)
            {
                if (!receiveThread.Join(TimeSpan.FromSeconds(2)))
                {
                    SrLogger.WarnSensitive("Receive thread did not stop gracefully");
                    SrLogger.Warn("Receive thread did not stop gracefully");
                }
            }

            playerManager.Clear();

            SrLogger.LogSensitive("Disconnected from server");
            SrLogger.Log("Disconnected from server");
            OnDisconnected?.Invoke();
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Error during disconnect: {ex}");
            SrLogger.Error($"Error during disconnect: {ex}");
        }
    }

    internal void NotifyConnected()
    {
        OnConnected?.Invoke(OwnPlayerId);
    }

    public RemotePlayer? GetRemotePlayer(string playerId)
    {
        return playerManager.GetPlayer(playerId);
    }

    public List<RemotePlayer> GetAllRemotePlayers()
    {
        return playerManager.GetAllPlayers();
    }
}