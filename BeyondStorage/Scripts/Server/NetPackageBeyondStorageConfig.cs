﻿using System.IO;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.Server;

public class NetPackageBeyondStorageConfig : NetPackage
{
    private const ushort ConfigVersion = 2;

    // TODO: Update number if more options being sent
    private const ushort BoolCount = 11;

    public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

    public override void write(PooledBinaryWriter _writer)
    {
        LogUtil.DebugLog($"Sending config, version {ConfigVersion}, to client.");

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
        LogUtil.DebugLog($"Received config from server. Version {configVersion}; sentBoolCount {sentBoolCount}; localBoolCount {BoolCount}.");
        // check if we got the same, newer, or older version of the config.
        switch (configVersion)
        {
            case > ConfigVersion:
                LogUtil.Warning("Newer configuration version received from server! You might be missing features present on the server and is advised to use the same version.");
                break;
            case < ConfigVersion:
                // TODO: maybe extract what we can from server settings
                LogUtil.Error(
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
        LogUtil.DebugLog($"ModConfig.ServerConfig.range {ModConfig.ServerConfig.range}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.onlyStorageCrates {ModConfig.ServerConfig.onlyStorageCrates}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.pullFromVehicleStorage {ModConfig.ServerConfig.pullFromVehicleStorage}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.pullFromWorkstationOutputs {ModConfig.ServerConfig.pullFromWorkstationOutputs}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.pullFromDewCollectors {ModConfig.ServerConfig.pullFromDewCollectors}");

        LogUtil.DebugLog($"ModConfig.ServerConfig.enableForBlockRepair {ModConfig.ServerConfig.enableForBlockRepair}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.enableForBlockUpgrade {ModConfig.ServerConfig.enableForBlockUpgrade}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.enableForGeneratorRefuel {ModConfig.ServerConfig.enableForGeneratorRefuel}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.enableForItemRepair {ModConfig.ServerConfig.enableForItemRepair}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.enableForReload {ModConfig.ServerConfig.enableForReload}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.enableForVehicleRefuel {ModConfig.ServerConfig.enableForVehicleRefuel}");
        LogUtil.DebugLog($"ModConfig.ServerConfig.enableForVehicleRepair {ModConfig.ServerConfig.enableForVehicleRepair}");
#endif
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        LogUtil.DebugLog("Updated client config to use server settings.");
    }

    public override int GetLength()
    {
        // save room for 6 more bytes (future boolean options)
        // kept it 6 after introducing pullFromWorkstationOutputs
        // kept it 6 after introducing pullFromDewCollectors
        const int futureReservedSpace = 6;
        const int ushortSize = 2;
        const int floatSize = 4;
        // Future Space + ConfigVersion + BoolCount + Range + (Bool(1) * Count)
        return futureReservedSpace + ushortSize + ushortSize + floatSize + BoolCount;
    }
}