using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Bridges movement to FishNet components when available without hard dependencies.
/// Assign FishNet NetworkObject/NetworkTransform in the inspector once installed.
/// </summary>
public class NetworkTransformProxy : MonoBehaviour
{
    [Tooltip("Optional: assign FishNet NetworkObject when available.")]
    [SerializeField] private Component networkObject;

    [Tooltip("Optional: assign FishNet NetworkTransform when available.")]
    [SerializeField] private Component networkTransform;

    [Tooltip("When true, movement runs even when no network object is present.")]
    [SerializeField] private bool simulateWhenNoNetwork = true;

    private PropertyInfo _isOwnerProp;
    private PropertyInfo _isServerProp;
    private MethodInfo _setPositionMethod;
    private MethodInfo _setRotationMethod;
    private MethodInfo _setPosRotMethod;
    private MethodInfo _teleportMethod;

    private void Awake()
    {
        CacheNetworkMembers();
    }

    private void CacheNetworkMembers()
    {
        if (networkObject != null)
        {
            var type = networkObject.GetType();
            _isOwnerProp = type.GetProperty("IsOwner");
            _isServerProp = type.GetProperty("IsServer");
        }

        if (networkTransform != null)
        {
            var t = networkTransform.GetType();
            _setPosRotMethod = t.GetMethod("SetPositionAndRotation", new[] { typeof(Vector3), typeof(Quaternion) });
            _teleportMethod = t.GetMethod("Teleport", new[] { typeof(Vector3), typeof(Quaternion) })
                ?? t.GetMethod("Teleport", new[] { typeof(Vector3) });
            _setPositionMethod = t.GetMethod("SetPosition", new[] { typeof(Vector3) });
            _setRotationMethod = t.GetMethod("SetRotation", new[] { typeof(Quaternion) });
        }
    }

    public bool CanSimulate
    {
        get
        {
            if (networkObject == null) return simulateWhenNoNetwork;
            if (_isOwnerProp != null && _isOwnerProp.PropertyType == typeof(bool))
            {
                return (bool)_isOwnerProp.GetValue(networkObject, null);
            }
            if (_isServerProp != null && _isServerProp.PropertyType == typeof(bool))
            {
                return (bool)_isServerProp.GetValue(networkObject, null);
            }
            return simulateWhenNoNetwork;
        }
    }

    public void ApplyPosition(Vector3 position)
    {
        ApplyPositionAndRotation(position, transform.rotation);
    }

    public void ApplyPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        if (networkTransform != null)
        {
            if (_setPosRotMethod != null)
            {
                _setPosRotMethod.Invoke(networkTransform, new object[] { position, rotation });
                return;
            }
            if (_teleportMethod != null)
            {
                var parameters = _teleportMethod.GetParameters();
                if (parameters.Length == 2)
                    _teleportMethod.Invoke(networkTransform, new object[] { position, rotation });
                else
                    _teleportMethod.Invoke(networkTransform, new object[] { position });
                return;
            }
            if (_setPositionMethod != null)
            {
                _setPositionMethod.Invoke(networkTransform, new object[] { position });
                if (_setRotationMethod != null)
                {
                    _setRotationMethod.Invoke(networkTransform, new object[] { rotation });
                }
                return;
            }
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    public void RefreshBindings(Component newNetworkObject, Component newNetworkTransform)
    {
        networkObject = newNetworkObject;
        networkTransform = newNetworkTransform;
        _isOwnerProp = null;
        _isServerProp = null;
        _setPositionMethod = null;
        _setRotationMethod = null;
        _setPosRotMethod = null;
        _teleportMethod = null;
        CacheNetworkMembers();
    }
}
