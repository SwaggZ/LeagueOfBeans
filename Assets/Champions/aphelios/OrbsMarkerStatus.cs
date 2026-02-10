using System.Collections;
using UnityEngine;

// Tracks enemies hit by Aphelios's orbs. Shows a visual orb marker above the enemy,
// applies a small slow, and can be stunned when Aphelios uses the orbs ability.
public class OrbsMarkerStatus : MonoBehaviour
{
    private CharacterControl _cc;
    private DummyController _dc;
    private float _baseSpeed;
    private float _slowAmount = 0.05f; // 5% slow
    private Coroutine _co;
    private GameObject _orbIndicator;
    private ModifierTracker _modifierTracker;
    private float _markerDuration;
    private HealthSystem _hp;
    private bool _isStunned = false;

    void Awake()
    {
        _cc = GetComponent<CharacterControl>();
        if (_cc == null) _cc = GetComponentInParent<CharacterControl>();
        
        _dc = GetComponent<DummyController>();
        if (_dc == null) _dc = GetComponentInParent<DummyController>();
        
        if (_cc != null) _baseSpeed = _cc.speed;
        
        _modifierTracker = GetComponent<ModifierTracker>();
        if (_modifierTracker == null) _modifierTracker = GetComponentInParent<ModifierTracker>();
        
        _hp = GetComponent<HealthSystem>();
        if (_hp == null) _hp = GetComponentInParent<HealthSystem>();
    }

    public void Apply(float duration)
    {
        _markerDuration = duration;
        
        // Try to find components again if not found in Awake
        if (_cc == null) _cc = GetComponentInParent<CharacterControl>();
        if (_dc == null) _dc = GetComponentInParent<DummyController>();
        if (_modifierTracker == null) _modifierTracker = GetComponentInParent<ModifierTracker>();
        
        if (_cc != null)
        {
            if (_baseSpeed <= 0f) _baseSpeed = _cc.speed;
            // Apply 5% slow via CharacterControl
            UpdateSpeed();
        }
        else if (_dc != null)
        {
            // Apply 5% slow via DummyController
            _dc.ApplySlow(1f - _slowAmount, duration);
        }

        // Stop previous coroutine if any
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Run(duration));

        // Create visual orb indicator if not already present
        if (_orbIndicator == null)
        {
            CreateOrbIndicator();
        }

        // Only add UI modifier if not using DummyController (which already adds its own)
        if (_dc == null)
        {
            Sprite icon = ModifiersIconLibrary.Instance != null ? ModifiersIconLibrary.Instance.SLOWNESS : null;
            if (CompareTag("Player") && ModifiersUIManager.Instance != null)
            {
                ModifiersUIManager.Instance.AddOrUpdate("StatusOrbsMarker", icon, "Orb Marked", Mathf.Max(0.01f, duration), 0);
            }
            if (!CompareTag("Player") && _modifierTracker != null)
            {
                _modifierTracker.AddOrUpdate("StatusOrbsMarker", icon, Mathf.Max(0.01f, duration), 0);
            }
        }
    }

    private void CreateOrbIndicator()
    {
        // Create a visual orb that floats above the enemy
        _orbIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _orbIndicator.name = "OrbsMarkerViz";
        
        // Remove collider from the indicator
        var col = _orbIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Style the orb: semi-transparent cyan/blue color
        var mr = _orbIndicator.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.3f, 0.7f, 1f, 0.6f); // cyan with slight transparency
            mr.material = mat;
        }

        _orbIndicator.transform.SetParent(transform);
        _orbIndicator.transform.localScale = Vector3.one * 0.25f;

        // Position above the enemy's head
        PositionOrbIndicator();
    }

    private void PositionOrbIndicator()
    {
        if (_orbIndicator == null) return;
        
        // Get enemy bounds to position orb above them
        float posY = 2f; // default height above ground
        var col = GetComponent<Collider>();
        if (col != null)
        {
            posY = col.bounds.size.y + 0.5f;
        }

        _orbIndicator.transform.localPosition = new Vector3(0, posY, 0);
    }

    void Update()
    {
        // Keep orb positioned above enemy
        if (_orbIndicator != null)
        {
            PositionOrbIndicator();
        }
    }

    IEnumerator Run(float dur)
    {
        float t = dur;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        // Cleanup
        RemoveMarker();
        _co = null;
    }

    private void RemoveMarker()
    {
        if (_orbIndicator != null)
        {
            Destroy(_orbIndicator);
            _orbIndicator = null;
        }

        // Reset speed if not stunned
        if (!_isStunned)
        {
            UpdateSpeed();
        }

        // Only remove modifier if we added it (not when DummyController handled it)
        if (_dc == null)
        {
            if (CompareTag("Player") && ModifiersUIManager.Instance != null)
            {
                ModifiersUIManager.Instance.Remove("StatusOrbsMarker");
            }
            if (!CompareTag("Player") && _modifierTracker != null)
            {
                _modifierTracker.Remove("StatusOrbsMarker");
            }
        }
    }

    /// <summary>
    /// Stun this enemy when Aphelios uses the orbs ability.
    /// </summary>
    public void StunFromOrbs(float stunDuration)
    {
        _isStunned = true;

        // Try to find components if not found
        if (_cc == null) _cc = GetComponentInParent<CharacterControl>();
        if (_dc == null) _dc = GetComponentInParent<DummyController>();
        if (_modifierTracker == null) _modifierTracker = GetComponentInParent<ModifierTracker>();

        // Apply stun via CharacterControl or DummyController
        if (_cc != null)
        {
            _cc.Stun(stunDuration);
        }
        else if (_dc != null)
        {
            // Clear the slow from orbs before applying stun
            _dc.ClearSlow();
            _dc.Stun(stunDuration); // This already shows stun modifier in DummyController
        }

        // Only add UI modifier if not using DummyController (Stun() already adds its own)
        if (_dc == null)
        {
            Sprite stunIcon = ModifiersIconLibrary.Instance != null ? ModifiersIconLibrary.Instance.STUN : null;
            if (CompareTag("Player") && ModifiersUIManager.Instance != null)
            {
                ModifiersUIManager.Instance.AddOrUpdate("StatusOrbsStun", stunIcon, "Stunned", stunDuration, 0);
            }
            if (!CompareTag("Player") && _modifierTracker != null)
            {
                _modifierTracker.AddOrUpdate("StatusOrbsStun", stunIcon, stunDuration, 0);
            }
        }

        // Clean up the marker visual when stunned
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
        RemoveMarker();
    }

    void UpdateSpeed()
    {
        if (_cc == null) return;
        // Only apply slow if marker is still active
        if (_orbIndicator != null && !_isStunned)
        {
            float slowMult = 1f - _slowAmount;
            _cc.speed = _baseSpeed * slowMult;
        }
        else
        {
            _cc.speed = _baseSpeed;
        }
    }

    void OnDisable()
    {
        if (_cc != null) _cc.speed = _baseSpeed;
        if (_orbIndicator != null)
        {
            Destroy(_orbIndicator);
            _orbIndicator = null;
        }
    }

    void OnDestroy()
    {
        if (_orbIndicator != null)
        {
            Destroy(_orbIndicator);
        }
    }
}
