using SR2E;

namespace SR2MP;

// We should separate the commands from this file later - if possible
// Done - Az

public class HostCommand : SR2ECommand
{
    private static Server.Server? server;

    public override string ID => "host";
    public override string Usage => "host <port>";

    public override bool Execute(string[] args)
    {
        server = new Server.Server();
        server.Start(args.Length == 1 ? int.Parse(args[0]) : 1919);
        return true;
    }

    public override List<string> GetAutoComplete(int argIndex, string[] args)
    {
        if (argIndex == 0)
            return new List<string>
            {
                "1900",
                "1901",
                "1902",
                "1903",
                "1904",
                "1905",
                "1906",
                "1907",
                "1908",
                "1909",
                "1910",
                "1911",
                "1912",
                "1913",
                "1914",
                "1915",
                "1916",
                "1917",
                "1918",
                "1919"
            };
        return null;
    }
}

public class JoinCommand : SR2ECommand
{
    public override bool Execute(string[] args)
    {
        // MultiplayerManager.Instance.Connect(args[0]); <- Tarr's code
        return true;
    }

    public override string ID => "join";
    public override string Usage => "join <code>";
}