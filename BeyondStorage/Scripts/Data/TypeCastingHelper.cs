using System;
using System.Collections.Generic;
using System.Linq;

namespace BeyondStorage.Scripts.Data;

public static class TypeCastingHelper
{
    private static readonly Dictionary<object, CastDiagnostic> s_latestCastErrors = new();
    private static readonly LinkedList<object> s_objectAccessOrder = new();
    private static readonly object s_lockObject = new();
    private const int MaxTrackedObjects = 10;

    public class CastDiagnostic
    {
        public DateTime Timestamp { get; set; }
        public string ActualTypeName { get; set; }
        public string TargetTypeName { get; set; }
        public string ErrorReason { get; set; }
        public string InheritanceChain { get; set; }
        public string[] ImplementedInterfaces { get; set; }
        public bool IsActualTypeSealed { get; set; }
        public bool IsTargetTypeInterface { get; set; }
        public bool IsTargetTypeAbstract { get; set; }
        public bool IsActualTypeGeneric { get; set; }
        public bool IsTargetTypeGeneric { get; set; }
        public string AssemblyMismatch { get; set; }
        public bool IsBoxingIssue { get; set; }
        public string SuggestedFix { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] {ErrorReason} - Actual: {ActualTypeName}, Target: {TargetTypeName}";
        }
    }

    public static T TryCastWithDiagnostics<T>(object obj) where T : class
    {
        T result = obj as T;

        if (result == null && obj != null) // obj is null is a valid scenario, not an error
        {
            Type actualType = obj.GetType();
            Type targetType = typeof(T);

            var diagnostic = BuildDiagnostic(actualType, targetType, obj);

            // Store only the latest error with thread safety
            lock (s_lockObject)
            {
                // Update access order
                UpdateObjectAccessOrder(obj);

                // Store only the most recent error (overwrites previous)
                s_latestCastErrors[obj] = diagnostic;

                // Maintain only the latest 10 objects
                MaintainObjectLimit();
            }
        }

        return result;
    }

    private static CastDiagnostic BuildDiagnostic(Type actualType, Type targetType, object obj)
    {
        var diagnostic = new CastDiagnostic
        {
            Timestamp = DateTime.Now,
            ActualTypeName = actualType.FullName,
            TargetTypeName = targetType.FullName,
            IsActualTypeSealed = actualType.IsSealed,
            IsTargetTypeInterface = targetType.IsInterface,
            IsTargetTypeAbstract = targetType.IsAbstract,
            IsActualTypeGeneric = actualType.IsGenericType,
            IsTargetTypeGeneric = targetType.IsGenericType,
            ImplementedInterfaces = actualType.GetInterfaces().Select(i => i.FullName).ToArray(),
            InheritanceChain = BuildInheritanceChain(actualType)
        };

        // Determine specific error reason and suggested fix
        diagnostic.ErrorReason = DetermineErrorReason(actualType, targetType, obj);
        diagnostic.AssemblyMismatch = CheckAssemblyMismatch(actualType, targetType);
        diagnostic.IsBoxingIssue = IsBoxingIssue(actualType, targetType);
        diagnostic.SuggestedFix = SuggestFix(actualType, targetType, diagnostic);

        return diagnostic;
    }

    private static string DetermineErrorReason(Type actualType, Type targetType, object obj)
    {
        // Check for common casting issues
        if (actualType.IsValueType && targetType.IsClass)
        {
            return "VALUE_TYPE_TO_REFERENCE_TYPE";
        }

        if (targetType.IsInterface && !actualType.GetInterfaces().Contains(targetType))
        {
            return "INTERFACE_NOT_IMPLEMENTED";
        }

        if (targetType.IsClass && !targetType.IsAssignableFrom(actualType))
        {
            if (actualType.IsSubclassOf(typeof(object)) && targetType.IsSubclassOf(typeof(object)))
            {
                return "INCOMPATIBLE_REFERENCE_TYPES";
            }
        }

        if (actualType.Assembly != targetType.Assembly)
        {
            return "CROSS_ASSEMBLY_CAST";
        }

        if (actualType.IsGenericType && targetType.IsGenericType)
        {
            return "GENERIC_TYPE_MISMATCH";
        }

        if (obj is System.Runtime.Remoting.Proxies.RealProxy)
        {
            return "PROXY_OBJECT_CAST";
        }

        if (actualType.FullName.Contains("AnonymousType"))
        {
            return "ANONYMOUS_TYPE_CAST";
        }

        return "GENERAL_CAST_FAILURE";
    }

    private static string CheckAssemblyMismatch(Type actualType, Type targetType)
    {
        if (actualType.Assembly != targetType.Assembly)
        {
            return $"Actual: {actualType.Assembly.FullName} | Target: {targetType.Assembly.FullName}";
        }
        return null;
    }

    private static bool IsBoxingIssue(Type actualType, Type targetType)
    {
        return actualType.IsValueType && targetType == typeof(object);
    }

    private static string BuildInheritanceChain(Type type)
    {
        var chain = new List<string>();
        Type current = type;

        while (current != null && current != typeof(object))
        {
            chain.Add(current.Name);
            current = current.BaseType;
        }

        return string.Join(" -> ", chain);
    }

    private static string SuggestFix(Type actualType, Type targetType, CastDiagnostic diagnostic)
    {
        switch (diagnostic.ErrorReason)
        {
            case "INTERFACE_NOT_IMPLEMENTED":
                return $"Consider implementing {targetType.Name} on {actualType.Name}";

            case "VALUE_TYPE_TO_REFERENCE_TYPE":
                return "Use boxing or create a wrapper class";

            case "INCOMPATIBLE_REFERENCE_TYPES":
                var commonBase = FindCommonBaseType(actualType, targetType);
                return commonBase != null ? $"Cast to common base: {commonBase.Name}" : "No common base type found";

            case "GENERIC_TYPE_MISMATCH":
                return "Check generic type parameters match exactly";

            case "CROSS_ASSEMBLY_CAST":
                return "Verify both types are from expected assemblies";

            case "PROXY_OBJECT_CAST":
                return "Use proxy-aware casting or unwrap proxy";

            case "ANONYMOUS_TYPE_CAST":
                return "Cannot cast anonymous types - use dynamic or reflection";

            default:
                return "Verify type compatibility and inheritance hierarchy";
        }
    }

    private static Type FindCommonBaseType(Type type1, Type type2)
    {
        var type1Hierarchy = GetTypeHierarchy(type1);
        var type2Hierarchy = GetTypeHierarchy(type2);

        return type1Hierarchy.Intersect(type2Hierarchy).FirstOrDefault();
    }

    private static List<Type> GetTypeHierarchy(Type type)
    {
        var hierarchy = new List<Type>();
        Type current = type;

        while (current != null)
        {
            hierarchy.Add(current);
            current = current.BaseType;
        }

        return hierarchy;
    }

    private static void UpdateObjectAccessOrder(object obj)
    {
        // Remove object from current position if it exists
        var node = s_objectAccessOrder.Find(obj);
        if (node != null)
        {
            s_objectAccessOrder.Remove(node);
        }

        // Add to the end (most recent)
        s_objectAccessOrder.AddLast(obj);
    }

    private static void MaintainObjectLimit()
    {
        // Remove oldest objects if we exceed the limit
        while (s_objectAccessOrder.Count > MaxTrackedObjects)
        {
            var oldestObject = s_objectAccessOrder.First.Value;
            s_objectAccessOrder.RemoveFirst();
            s_latestCastErrors.Remove(oldestObject);
        }
    }

    /// <summary>
    /// Gets the most recent cast diagnostic for a specific object
    /// </summary>
    /// <param name="obj">The object to get the latest diagnostic for</param>
    /// <returns>The most recent diagnostic, or null if no error exists</returns>
    public static CastDiagnostic GetLatestDiagnostic(object obj)
    {
        lock (s_lockObject)
        {
            return s_latestCastErrors.TryGetValue(obj, out var diagnostic) ? diagnostic : null;
        }
    }

    /// <summary>
    /// Gets the most recent cast error message for a specific object
    /// </summary>
    /// <param name="obj">The object to get the latest error for</param>
    /// <returns>The most recent error message, or null if no error exists</returns>
    public static string GetLatestError(object obj)
    {
        var diagnostic = GetLatestDiagnostic(obj);
        return diagnostic?.ToString();
    }

    /// <summary>
    /// Gets all objects that have had cast errors (up to 10 most recent)
    /// </summary>
    /// <returns>Collection of objects with cast errors, ordered by most recent access</returns>
    public static IEnumerable<object> GetTrackedObjects()
    {
        lock (s_lockObject)
        {
            return s_objectAccessOrder.ToList();
        }
    }

    /// <summary>
    /// Clears all stored cast errors
    /// </summary>
    public static void ClearAllErrors()
    {
        lock (s_lockObject)
        {
            s_latestCastErrors.Clear();
            s_objectAccessOrder.Clear();
        }
    }

    /// <summary>
    /// Clears cast error for a specific object
    /// </summary>
    /// <param name="obj">The object to clear error for</param>
    public static void ClearErrorFor(object obj)
    {
        lock (s_lockObject)
        {
            s_latestCastErrors.Remove(obj);
            s_objectAccessOrder.Remove(obj);
        }
    }

    /// <summary>
    /// Gets a summary of all cast errors for tracked objects
    /// </summary>
    /// <returns>Dictionary with error reasons as keys and counts as values</returns>
    public static Dictionary<string, int> GetErrorSummary()
    {
        lock (s_lockObject)
        {
            return s_latestCastErrors.Values
                .GroupBy(d => d.ErrorReason)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    /// <summary>
    /// Gets the total number of tracked objects
    /// </summary>
    /// <returns>Number of objects currently being tracked</returns>
    public static int GetTrackedObjectCount()
    {
        lock (s_lockObject)
        {
            return s_objectAccessOrder.Count;
        }
    }

    /// <summary>
    /// Gets all latest diagnostics for all tracked objects
    /// </summary>
    /// <returns>Dictionary mapping objects to their most recent diagnostics</returns>
    public static Dictionary<object, CastDiagnostic> GetAllLatestDiagnostics()
    {
        lock (s_lockObject)
        {
            return new Dictionary<object, CastDiagnostic>(s_latestCastErrors);
        }
    }

    /// <summary>
    /// Gets detailed diagnostic report for all tracked objects
    /// </summary>
    /// <returns>Formatted string with detailed diagnostic information</returns>
    public static string GetDetailedDiagnosticReport()
    {
        lock (s_lockObject)
        {
            if (s_latestCastErrors.Count == 0)
            {
                return "No cast errors recorded.";
            }

            var report = new List<string> { "=== Cast Diagnostic Report ===" };

            foreach (var kvp in s_latestCastErrors)
            {
                var diagnostic = kvp.Value;
                report.Add($"\nObject: {kvp.Key?.GetType().Name ?? "null"}");
                report.Add($"Error: {diagnostic.ErrorReason}");
                report.Add($"Inheritance: {diagnostic.InheritanceChain}");
                report.Add($"Interfaces: {string.Join(", ", diagnostic.ImplementedInterfaces.Take(3))}");
                report.Add($"Suggestion: {diagnostic.SuggestedFix}");

                if (!string.IsNullOrEmpty(diagnostic.AssemblyMismatch))
                {
                    report.Add($"Assembly Issue: {diagnostic.AssemblyMismatch}");
                }
            }

            return string.Join("\n", report);
        }
    }
}
