using System.IO;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Multiplayer;

public class NetPackageBeyondStorageConfig : NetPackage
{
    // History:
    // this was a ushort, for unknown reasons, in 1_0 series, which had 3.0.2 as it's release version
    // before v2.2.0, ConfigVersion was either 1 (the 1_0 series) or 2.x (the updated for 2.0 series)
    // for v2.2.0, ConfigVersion == 220, and is an int (size 2 to size 5). that takes 2 bytes out of the future reserved space
    //   but we can now use a structured versioning system in 4 bytes:
    //     major.minor.patch:
    //        major mask = 0xFF00-0000
    //        minor mask = 0x00FF-0000
    //        patch mask = 0x0000-FFFF

    // Masks for extracting version components
    //      public const int MAJOR_MASK = 0xFF000000;
    //      public const int MINOR_MASK = 0x00FF0000;
    //      public const int PATCH_MASK = 0x0000FFFF;

    // Bit shifts for positioning (encode is shifted left, decode is shifted right)
    //      public const int MAJOR_SHIFT = 24;
    //      public const int MINOR_SHIFT = 16;
    //      public const int PATCH_SHIFT = 0;  // for patch, no shift needed, just AND the patch mask
    private const uint ConfigVersion = 0x02020001;

    // IMPORTANT: Update number if more options being sent
    private const ushort BoolCount = 13;  // 13 as of v2.2.0, which introduces pullFromDrones and enableForBlockTexture

    public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

    public override void write(PooledBinaryWriter _writer)
    {
        ModLogger.DebugLog($"Sending config, version {ConfigVersion}, to client.");

        base.write(_writer);

        var binaryWriter = ((BinaryWriter)_writer);

        binaryWriter.Write(ConfigVersion);
        binaryWriter.Write(BoolCount);

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
    }

    public override void read(PooledBinaryReader reader)
    {
        var configVersion = reader.ReadUInt32();
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

        // Future Space + ConfigVersion + BoolCount + Range + (Bool(1) * Count)
        return futureReservedSpace + sizeof(uint) + sizeof(ushort) + sizeof(float) + sizeof(bool) * BoolCount;
    }
}