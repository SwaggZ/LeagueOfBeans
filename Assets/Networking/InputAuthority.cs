using UnityEngine;

public interface IInputAuthority
{
    bool HasInputAuthority { get; }
}

/// <summary>
/// Local-only input authority flag. Swap this to a network-backed authority later.
/// </summary>
public class InputAuthority : MonoBehaviour, IInputAuthority
{
    [Tooltip("When no networking is active, keep this true so input still works.")]
    [SerializeField] private bool hasInputAuthority = true;

    public bool HasInputAuthority => hasInputAuthority;

    public void SetHasInputAuthority(bool value)
    {
        hasInputAuthority = value;
    }
}
