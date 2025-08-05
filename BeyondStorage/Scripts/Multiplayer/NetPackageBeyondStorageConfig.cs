using System.IO;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Multiplayer;

public class NetPackageBeyondStorageConfig : NetPackage
{
    // this is a ushort for unknown reasons, and it can't be changed
    // before v2.2.0, ConfigVersion was either 1 (the 1.0 series) or 2 (the updated for 2.0 series)
    // for v2.2.0, ConfigVersion == 3
    private const ushort ConfigVersion = 3;


    // IMPORTANT: Update number if more options being sent
    private const ushort BoolCount = 13;  // 13 as of v2.2.0, which introduces pullFromDrones and enableForBlockTexture

    public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

    public override void write(PooledBinaryWriter _writer)
    {
        ModLogger.DebugLog($"Sending config, version {ConfigVersion}, to client.");

        base.write(_writer);

        var binaryWriter = ((BinaryWriter)_writer);

        binaryWriter.Write(ConfigVersion);
        // #if DEBUG
        //         // Testing backwards compatibility
        //         binaryWriter.Write((ushort)(BoolCount + 5));
        // #else
        binaryWriter.Write(BoolCount);
        // #endif

        // do not change the order of these
        binaryWriter.Write(ModConfig.ClientConfig.range);
        binaryWriter.Write(ModConfig.ClientConfig.enableForBlockRepair);
        binaryWriter.Write(ModConfig.ClientConfig.enableForBlockUpgrade);
        binaryWriter.Write(ModConfig.ClientConfig.enableForGeneratorRefuel);
        binaryWriter.Write(ModConfig.ClientConfig.enableForItemRepair);
        binaryWriter.Write(ModConfig.ClientConfig.enableForReload);
        binaryWriter.Write(ModConfig.ClientConfig.enableForVehicleRefuel);
        binaryWriter.Write(ModConfig.ClientConfig.enableForVehicleRepair);
        binaryWriter.Write(ModConfig.ClientConfig.onlyStorageCrates);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromVehicleStorage);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromWorkstationOutputs);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromDewCollectors);
        binaryWriter.Write(ModConfig.ClientConfig.enableForBlockTexture);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromDrones);

        // #if DEBUG
        //         // testing backwards compatibility if we are sending more than expecting to receive (EX: newer config sent by server to client running older mod version)
        //         binaryWriter.Write(ModConfig.ClientConfig.pullFromVehicleStorage);
        //         binaryWriter.Write(ModConfig.ClientConfig.pullFromVehicleStorage);
        //         binaryWriter.Write(ModConfig.ClientConfig.pullFromVehicleStorage);
        //         binaryWriter.Write(ModConfig.ClientConfig.pullFromVehicleStorage);
        //         binaryWriter.Write(ModConfig.ClientConfig.pullFromVehicleStorage);
        // #endif
    }

    public override void read(PooledBinaryReader reader)
    {
        var configVersion = reader.ReadUInt16();
        var sentBoolCount = reader.ReadUInt16();
        ModLogger.DebugLog($"Received config from server. Version {configVersion}; sentBoolCount {sentBoolCount}; localBoolCount {BoolCount}.");
        // check if we got the same, newer, or older version of the config.
        switch (configVersion)
        {
            case > ConfigVersion:
                ModLogger.Warning("Newer configuration version received from server! You might be missing features present on the server and is advised to use the same version.");
                break;
            case < ConfigVersion:
                // TODO: maybe extract what we can from server settings
                ModLogger.Error(
                    "Older configuration version received from server, failed to sync server settings! Either downgrade client mod to the version on the server OR have the server upgrade to client's mod version.");
                return;
        }

        // update server config (or set if it's first time)
        // do not change the order of these
        ModConfig.ServerConfig.range = reader.ReadSingle();
        ModConfig.ServerConfig.enableForBlockRepair = reader.ReadBoolean();
        ModConfig.ServerConfig.enableForBlockUpgrade = reader.ReadBoolean();
        ModConfig.ServerConfig.enableForGeneratorRefuel = reader.ReadBoolean();
        ModConfig.ServerConfig.enableForItemRepair = reader.ReadBoolean();
        ModConfig.ServerConfig.enableForReload = reader.ReadBoolean();
        ModConfig.ServerConfig.enableForVehicleRefuel = reader.ReadBoolean();
        ModConfig.ServerConfig.enableForVehicleRepair = reader.ReadBoolean();
        ModConfig.ServerConfig.onlyStorageCrates = reader.ReadBoolean();
        ModConfig.ServerConfig.pullFromVehicleStorage = reader.ReadBoolean();
        ModConfig.ServerConfig.pullFromWorkstationOutputs = reader.ReadBoolean();
        ModConfig.ServerConfig.pullFromDewCollectors = reader.ReadBoolean();
        ModConfig.ServerConfig.enableForBlockTexture = reader.ReadBoolean();
        ModConfig.ServerConfig.pullFromDrones = reader.ReadBoolean();

        // Set HasServerConfig = true
        ServerUtils.HasServerConfig = true;

        if (sentBoolCount > BoolCount)
        {
            for (var i = 0; i < sentBoolCount - BoolCount; i++)
            {
                // read/discard remaining booleans if more than expected
                // this is for older clients
                reader.ReadBoolean();
            }
        }

#if DEBUG
        ModLogger.DebugLog($"ModConfig.ServerConfig.range {ModConfig.ServerConfig.range}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromDewCollectors {ModConfig.ServerConfig.pullFromDrones}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromDewCollectors {ModConfig.ServerConfig.pullFromDewCollectors}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromWorkstationOutputs {ModConfig.ServerConfig.pullFromWorkstationOutputs}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.onlyStorageCrates {ModConfig.ServerConfig.onlyStorageCrates}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromVehicleStorage {ModConfig.ServerConfig.pullFromVehicleStorage}");

        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForBlockRepair {ModConfig.ServerConfig.enableForBlockRepair}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForBlockTexture {ModConfig.ServerConfig.enableForBlockTexture}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForBlockUpgrade {ModConfig.ServerConfig.enableForBlockUpgrade}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForGeneratorRefuel {ModConfig.ServerConfig.enableForGeneratorRefuel}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForItemRepair {ModConfig.ServerConfig.enableForItemRepair}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForReload {ModConfig.ServerConfig.enableForReload}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForVehicleRefuel {ModConfig.ServerConfig.enableForVehicleRefuel}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.enableForVehicleRepair {ModConfig.ServerConfig.enableForVehicleRepair}");
#endif
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        ModLogger.DebugLog("Updated client config to use server settings.");
    }

    public override int GetLength()
    {
        // save room for 6 more bytes (future boolean options)
        // kept it 6 after introducing pullFromWorkstationOutputs
        // kept it 6 after introducing pullFromDewCollectors
        // kept it 6 after introducing enableForBlockTexture
        // kept it 6 after introducing pullfromDrones
        const int futureReservedSpace = 6;
        const int ushortSize = 2;
        const int floatSize = 4;
        // Future Space + ConfigVersion + BoolCount + Range + (Bool(1) * LRU_SUBFILTER_DISPLAY_MAX)
        return futureReservedSpace + ushortSize + ushortSize + floatSize + BoolCount;
    }
}