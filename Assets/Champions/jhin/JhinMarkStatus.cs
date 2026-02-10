using System.Collections;
using UnityEngine;

// Tracks enemies hit by Jhin's abilities. Shows a visual mark above the enemy.
// When Jhin uses RMB, marked enemies are stunned.
public class JhinMarkStatus : MonoBehaviour
{
    private Coroutine _co;
    private GameObject _markIndicator;
    private ModifierTracker _modifierTracker;
    private float _markDuration;

    void Awake()
    {
        _modifierTracker = GetComponent<ModifierTracker>();
        if (_modifierTracker == null) _modifierTracker = GetComponentInParent<ModifierTracker>();
    }

    public void Apply(float duration)
    {
        _markDuration = duration;

        if (_modifierTracker == null) _modifierTracker = GetComponentInParent<ModifierTracker>();

        // Stop previous coroutine if any
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Run(duration));

        // Create visual mark indicator if not already present
        if (_markIndicator == null)
        {
            CreateMarkIndicator();
        }

        // No modifier on healthbar - only the visual diamond indicator
        // Stun modifier will be shown when RMB is used
    }

    private void CreateMarkIndicator()
    {
        // Create a diamond shape above the enemy
        _markIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _markIndicator.name = "JhinMarkViz";

        // Remove collider
        var col = _markIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Style: rotated cube looks like diamond, purple/pink color
        var mr = _markIndicator.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.9f, 0.3f, 0.5f, 0.8f); // Pink
            mr.material = mat;
        }

        _markIndicator.transform.SetParent(transform);
        _markIndicator.transform.localScale = Vector3.one * 0.2f;
        _markIndicator.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

        PositionMarkIndicator();
    }

    private void PositionMarkIndicator()
    {
        if (_markIndicator == null) return;

        float posY = 2.2f;
        var col = GetComponent<Collider>();
        if (col != null)
        {
            posY = col.bounds.size.y + 0.6f;
        }

        _markIndicator.transform.localPosition = new Vector3(0, posY, 0);
    }

    void Update()
    {
        if (_markIndicator != null)
        {
            PositionMarkIndicator();
            // Rotate the mark for visual effect
            _markIndicator.transform.Rotate(Vector3.up, 90f * Time.deltaTime);
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

        RemoveMark();
        _co = null;
    }

    private void RemoveMark()
    {
        if (_markIndicator != null)
        {
            Destroy(_markIndicator);
            _markIndicator = null;
        }
        // No modifier to remove - we only show the visual diamond
    }

    /// <summary>
    /// Stun this enemy when Jhin uses RMB ability.
    /// </summary>
    public void StunFromMark(float stunDuration)
    {
        // Apply stun via DummyController or CharacterControl
        var dc = GetComponent<DummyController>();
        if (dc == null) dc = GetComponentInParent<DummyController>();

        if (dc != null)
        {
            dc.Stun(stunDuration);
        }
        else
        {
            var cc = GetComponent<CharacterControl>();
            if (cc == null) cc = GetComponentInParent<CharacterControl>();
            if (cc != null)
            {
                cc.Stun(stunDuration);
            }
        }

        // Clean up mark when stunned
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
        RemoveMark();
    }

    void OnDisable()
    {
        if (_markIndicator != null)
        {
            Destroy(_markIndicator);
            _markIndicator = null;
        }
    }

    void OnDestroy()
    {
        if (_markIndicator != null)
        {
            Destroy(_markIndicator);
        }
    }
}
