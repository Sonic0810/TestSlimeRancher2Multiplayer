using SR2E.Expansion;

namespace SR2MP;

public sealed class Main : SR2EExpansionV2
{
    public static Client.Client Client { get; private set; }

    public static class BuildInfo
    {
        public const string Name = "Slime Rancher 2 Multiplayer Mod";
        public const string Description = "Adds Multiplayer to Slime Rancher 2";
        public const string Author = "Shark";
        public const string CoAuthors = "";
        public const string Contributors = "Gopher, Artur, AlchlcSystm";
        public const string Company = "";
        public const string Version = "0.1.0.1";
        public const string DownloadLink = "";
        public const string SourceCode = "https://github.com/pyeight/SlimeRancher2Multiplayer";
        public const string Nexus = "";
        public const string Discord = "https://discord.com/invite/a7wfBw5feU";
        public const bool UsePrism = false;
    }

    public override void OnSceneWasLoaded(int _, string sceneName)
    {
    }

    public override void OnLateInitializeMelon()
    {
        Client = new Client.Client();
    }
}