using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Diagnostics;
internal static class NameLookups
{
    public readonly struct TypeNameInfo(string abbrev, string name)
    {
        public string Abbrev { get; } = abbrev;
        public string Name { get; } = name;
    }

    private static Dictionary<Type, TypeNameInfo> s_nonPluralNames;
    private static Dictionary<Type, TypeNameInfo> NonPluralNames
    {
        get
        {
            if (s_nonPluralNames == null)
            {
                InitializeTypeNames();
            }
            return s_nonPluralNames;
        }
    }

    private const string NM_UNKNOWN_TYPE_ABBREV = "NM:UT";
    private const string NM_UNKNOWN_TYPE_NAME = "NM:UnknownType";
    private static readonly TypeNameInfo s_unknownTypeNameInfo = new(NM_UNKNOWN_TYPE_ABBREV, NM_UNKNOWN_TYPE_NAME);

    private static void InitializeTypeNames()
    {
        s_nonPluralNames = new Dictionary<Type, TypeNameInfo>
        {
            { typeof(EntityDrone), new TypeNameInfo("DR", "Drone") },
            { typeof(TileEntityDewCollector), new TypeNameInfo("DC", "Dew Collector") },
            { typeof(TileEntityWorkstation), new TypeNameInfo("WS", "Workstation") },
            { typeof(ITileEntityLootable), new TypeNameInfo("LC", "Lootable Container") },
            { typeof(EntityVehicle), new TypeNameInfo("VH", "Vehicle") },
        };
    }

    public static TypeNameInfo GetNameInfo(Type type)
    {
        if (type == null)
        {
            var nullError = StackTraceProvider.AppendStackTrace("Cannot get the name of a null type.");
            ModLogger.DebugLog(nullError);
            return s_unknownTypeNameInfo;
        }

        if (TypeMatchingHelper.TryFindMatch(type, NonPluralNames, out var exactMatch, out var inheritanceMatch))
        {
            // For structs, check if the exact match has meaningful content
            if (!string.IsNullOrEmpty(exactMatch.Abbrev))
            {
                return exactMatch;
            }

            return inheritanceMatch;
        }

        var error = StackTraceProvider.AppendStackTrace($"Cannot lookup name of unknown type: ({type.Name})");
        ModLogger.DebugLog(error);
        return s_unknownTypeNameInfo;
    }

    public static string GetFullName(TypeNameInfo nameInfo)
    {
        return $"({nameInfo.Abbrev}: {nameInfo.Name})";
    }

    public static string GetFullName(Type type)
    {
        var nameInfo = GetNameInfo(type);
        return GetFullName(nameInfo);
    }

    public static string GetFullName(object o)
    {
        return GetFullName(o?.GetType());
    }

    public static string GetName(Type type)
    {
        return GetNameInfo(type).Name;
    }

    public static string GetAbbrev(Type type)
    {
        return GetNameInfo(type).Abbrev;
    }
}
