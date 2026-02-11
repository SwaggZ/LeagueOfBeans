using System.Reflection;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Applies address/port to common FishNet transports using reflection to avoid hard dependencies.
/// </summary>
public static class FishNetTransportConfigurator
{
    public static void Apply(Transport transport, string clientAddress, ushort port)
    {
        if (transport == null) return;

        // Try property-based configuration first.
        if (TrySetProperty(transport, "ClientAddress", clientAddress)) { }
        if (TrySetProperty(transport, "Port", port)) { }

        // Fall back to Tugboat private fields if needed.
        TrySetField(transport, "_clientAddress", clientAddress);
        TrySetField(transport, "_port", port);
    }

    private static bool TrySetProperty(object target, string propertyName, object value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null || !prop.CanWrite) return false;
        prop.SetValue(target, value);
        return true;
    }

    private static bool TrySetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return false;
        field.SetValue(target, value);
        return true;
    }
}
