using Il2Cpp;
using Mono.Cecil;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler(((byte)PacketType.InitialPlayerUpgrades))]
public class PlayerUpgradesLoadHandler : BaseClientPacketHandler
{
    public PlayerUpgradesLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<UpgradesPacket>();

        var upgradeInd = 0;
        var upgradesClient = GameContext.Instance.LookupDirector._upgradeDefinitions;
        foreach (var upgradeLvl in packet.Upgrades)
        {
            var upgrade = upgradesClient.items._items.FirstOrDefault(x => x._uniqueId == upgradeLvl.Key);

            SceneContext.Instance.PlayerState._model.upgradeModel.SetUpgradeLevel(upgrade, upgradeLvl.Value);
            
            upgradeInd++;
        }
    }
}