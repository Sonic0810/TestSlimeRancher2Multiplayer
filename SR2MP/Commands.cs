using SR2E;
using SR2E.Utils;

namespace SR2MP;

// We should separate the commands from this file later - if possible
// Done - Az

public sealed class HostCommand : SR2ECommand
{
    private static Server.Server? server;

    public override string ID => "host";
    public override string Usage => "host <port>";

    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();
        server = Main.Server;
        server.Start(int.Parse(args[0]), true);
        return true;
    }
}

public sealed class ConnectCommand : SR2ECommand
{
    public override string ID => "connect";
    public override string Usage => "connect <ip> <port>";

    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();

        if (args.Length < 2 || !int.TryParse(args[1], out var port))
        {
            return false;
        }

        string ip = args[0];

        // todo: Can probably be removed with the IPv6 stuff (or fixed)
        if (ip.StartsWith("[") && ip.EndsWith("]"))
        {
            ip = ip[1..^1];
        }

        Main.Client.Connect(ip, port);
        return true;
    }
}