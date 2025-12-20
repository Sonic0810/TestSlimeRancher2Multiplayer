using MelonLoader;
using MelonLoader.Utils;
using SR2E.Expansion;
using SR2MP.Shared.Utils;

namespace SR2MP;

public sealed class Main : SR2EExpansionV3
{
    public static Client.Client Client { get; private set; }
    public static Server.Server Server { get; private set; }
    static MelonPreferences_Category prefs;
    public static string username => prefs.GetEntry<string>("username").Value;

    public override void OnLateInitializeMelon()
    {
        prefs = MelonPreferences.CreateCategory("SR2MP");
        prefs.CreateEntry("username", "Player");

        Client = new Client.Client();
        Server = new Server.Server();
    }
}