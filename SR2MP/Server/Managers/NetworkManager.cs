using System.Net;
using System.Net.Sockets;

namespace SR2MP.Server.Managers;

public class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Thread? receiveThread;

    public event Action<byte[], IPEndPoint>? OnDataReceived;

    public bool IsRunning => isRunning;

    public void Start(int port)
    {
        if (isRunning)
        {
            SrLogger.LogSensitive("Server is already running!");
            SrLogger.Log("Server is already running!");
            return;
        }

        try
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            isRunning = true;

            SrLogger.LogSensitive($"Server started on port: {port}");
            SrLogger.Log($"Server started on port: {port}");

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Failed to start Server: {ex}");
            SrLogger.Error($"Failed to start Server: {ex}");
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.ErrorSensitive("Server is null in ReceiveLoop!");
            SrLogger.Error("Server is null in ReceiveLoop!");
            return;
        }

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);

                if (data.Length > 0)
                {
                    OnDataReceived?.Invoke(data, remoteEP);
                }
            }
            catch (SocketException)
            {
                if (!isRunning)
                    return;
            }
            catch (Exception ex)
            {
                SrLogger.ErrorSensitive($"ReceiveLoop error: {ex}");
                SrLogger.Error($"ReceiveLoop error: {ex}");
            }
        }
    }

    public void Send(byte[] data, IPEndPoint endPoint)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.WarnSensitive("Cannot send data: Server not running!");
            SrLogger.Warn("Cannot send data: Server not running!");
            return;
        }

        try
        {
            udpClient.Send(data, data.Length, endPoint);
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Failed to send data to {endPoint}: {ex}");
            SrLogger.Error($"Failed to send data to Client: {ex}");
        }
    }

    public void Stop()
    {
        if (!isRunning)
            return;

        isRunning = false;

        try
        {
            udpClient?.Close();

            if (receiveThread != null && receiveThread.IsAlive)
            {
                if (!receiveThread.Join(TimeSpan.FromSeconds(2)))
                {
                    SrLogger.WarnSensitive("Receive thread did not stop gracefully");
                    SrLogger.Warn("Receive thread did not stop gracefully");
                }
            }

            SrLogger.LogSensitive("Server stopped");
            SrLogger.Log("Server stopped");
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Error stopping Server: {ex}");
            SrLogger.Error($"Error stopping Server: {ex}");
        }
    }
}