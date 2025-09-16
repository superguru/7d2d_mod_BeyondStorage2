using System;
using System.IO;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Multiplayer;

public class NetPackageBeyondStorageConfig : NetPackage
{
    // History:
    // this was a ushort, for unknown reasons, in 1_0 series, which had 3.0.2 as it's release version
    // before v2.2.0, NetConfigVersion was either 1 (the 1_0 series) or 2 (the updated for 2.0 series)
    // for v2.2.0, NetConfigVersion == 220, and is an int (size 2 to size 5). that takes 2 bytes out of the future reserved space
    // Starting with v2.3.0, we now use the actual ModInfo.Version string for versioning to align with the new config system

    // Legacy version constants for backward compatibility
    private const uint LegacyV220 = 0x02020001;

    /// <summary>
    /// Current network config version - now uses ModInfo.Version for consistency with config system
    /// </summary>
    private static string CurrentNetConfigVersion => ConfigVersioning.CurrentVersion;

    // IMPORTANT: Update number if more options being sent
    private const ushort BoolCount = 13;  // 13 as of v2.2.0, which introduces pullFromDrones and enableForBlockTexture

    public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

    public override void write(PooledBinaryWriter _writer)
    {
        ModLogger.DebugLog($"Sending config version {CurrentNetConfigVersion} to client.");

        base.write(_writer);

        var binaryWriter = ((BinaryWriter)_writer);

        // Write version string instead of uint for v2.3.0+
        binaryWriter.Write(CurrentNetConfigVersion);
        binaryWriter.Write(BoolCount);

        // do not change the order of these
        binaryWriter.Write(ModConfig.ClientConfig.range);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromDrones);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromDewCollectors);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromWorkstationOutputs);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromPlayerCraftedNonCrates);
        binaryWriter.Write(ModConfig.ClientConfig.pullFromVehicleStorage);
    }

    private bool ReadBool(PooledBinaryReader reader)
    {
        // The purpose of this is to handle some value we don't expect, like an older client
        // Read a boolean value from the reader
        var byteIn = reader.ReadByte();
        return byteIn != 0; // Convert byte to boolean (0 = false, non-zero = true)
    }

    public override void read(PooledBinaryReader reader)
    {
        // Try to determine if this is a legacy (uint) or new (string) version format
        string serverConfigVersion;
        bool isLegacyVersion = false;

        try
        {
            // Peek at first 4 bytes to check if it's a legacy uint version
            var position = reader.BaseStream.Position;
            var possibleUint = reader.ReadUInt32();
            reader.BaseStream.Position = position; // Reset position

            if (possibleUint == LegacyV220)
            {
                // Legacy version format
                isLegacyVersion = true;
                var legacyVersion = reader.ReadUInt32();
                serverConfigVersion = "2.2.0"; // Map legacy version to known version string
                ModLogger.Info($"Received legacy network config version {legacyVersion:X8}, treating as v2.2.0");
            }
            else
            {
                // New string-based version format (v2.3.0+)
                serverConfigVersion = reader.ReadString();
                ModLogger.DebugLog($"Received string-based network config version: {serverConfigVersion}");
            }
        }
        catch (Exception e)
        {
            ModLogger.Error($"Failed to read network config version: {e.Message}", e);
            return;
        }

        var sentBoolCount = reader.ReadUInt16();
        ModLogger.DebugLog($"Received config from server. Version {serverConfigVersion}; sentBoolCount {sentBoolCount}; localBoolCount {BoolCount}.");

        // Parse versions for comparison
        if (!System.Version.TryParse(serverConfigVersion, out var serverVersion) ||
            !System.Version.TryParse(CurrentNetConfigVersion, out var clientVersion))
        {
            ModLogger.Warning($"Unable to parse versions for comparison. Server: {serverConfigVersion}, Client: {CurrentNetConfigVersion}");
        }
        else
        {
            // Version compatibility check
            switch (serverVersion.CompareTo(clientVersion))
            {
                case > 0:
                    ModLogger.Warning("Newer configuration version received from server! You might be missing features present on the server and is advised to use the same version.");
                    break;
                case < 0:
                    ModLogger.Error(
                        "Older configuration version received from server, failed to sync server settings! Either downgrade client mod to the version on the server OR have the server upgrade to client's mod version.");
                    return;
            }
        }

        // Apply server config migration if needed
        if (isLegacyVersion || serverConfigVersion != CurrentNetConfigVersion)
        {
            ModLogger.Info($"Migrating server config from version {serverConfigVersion} to {CurrentNetConfigVersion}");
        }

        // update server config (or set if it's first time)
        // do not change the order of these
        ModConfig.ServerConfig.range = reader.ReadSingle();
        ModConfig.ServerConfig.pullFromDrones = ReadBool(reader);
        ModConfig.ServerConfig.pullFromDewCollectors = ReadBool(reader);
        ModConfig.ServerConfig.pullFromWorkstationOutputs = ReadBool(reader);
        ModConfig.ServerConfig.pullFromPlayerCraftedNonCrates = ReadBool(reader);
        ModConfig.ServerConfig.pullFromVehicleStorage = ReadBool(reader);

        // Apply config versioning and migration to server config
        ModConfig.ServerConfig.version = CurrentNetConfigVersion;

        // Set HasServerConfig = true
        ServerUtils.HasServerConfig = true;

        if (sentBoolCount > BoolCount)
        {
            for (var i = 0; i < sentBoolCount - BoolCount; i++)
            {
                // read/discard remaining booleans if more than expected
                // this is for older clients
                _ = reader.ReadBoolean();
            }
        }

        ModLogger.Info($"Successfully applied server config version {serverConfigVersion}");

#if DEBUG
        ModLogger.DebugLog($"ModConfig.ServerConfig.version {ModConfig.ServerConfig.version}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.range {ModConfig.ServerConfig.range}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromDrones {ModConfig.ServerConfig.pullFromDrones}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromDewCollectors {ModConfig.ServerConfig.pullFromDewCollectors}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromWorkstationOutputs {ModConfig.ServerConfig.pullFromWorkstationOutputs}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.onlyStorageCrates {ModConfig.ServerConfig.pullFromPlayerCraftedNonCrates}");
        ModLogger.DebugLog($"ModConfig.ServerConfig.pullFromVehicleStorage {ModConfig.ServerConfig.pullFromVehicleStorage}");
#endif
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        ModLogger.DebugLog("Updated client config to use server settings with version compatibility.");
    }

    public override int GetLength()
    {
        // save room for 6 more bytes (future boolean options)
        const int futureReservedSpace = 6;

        // Calculate length for string-based version (v2.3.0+)
        // String length + string bytes + BoolCount + Range + (Bool * Count)
        var versionStringBytes = System.Text.Encoding.UTF8.GetByteCount(CurrentNetConfigVersion);
        var stringLengthPrefix = sizeof(int); // .NET string serialization includes length prefix

        return futureReservedSpace + stringLengthPrefix + versionStringBytes + sizeof(ushort) + sizeof(float) + sizeof(bool) * BoolCount;
    }
}