using System.Collections;
using UnityEngine;

// Simple per-target slow status that reduces CharacterControl.speed by (1 - slowPct) for a duration.
// Subsequent Apply() calls refresh duration and keep the strongest slow.
public class SlowStatus : MonoBehaviour
{
    private CharacterControl _cc;
    private float _baseSpeed;
    private float _activePct = 0f; // 0.3 = 30% slow
    private Coroutine _co;

    void Awake()
    {
        _cc = GetComponent<CharacterControl>();
        if (_cc != null) _baseSpeed = _cc.speed;
    }

    public void Apply(float slowPercent, float duration)
    {
        if (_cc == null) return;
        if (_baseSpeed <= 0f) _baseSpeed = _cc.speed;
        // keep the stronger slow
        _activePct = Mathf.Max(_activePct, Mathf.Clamp01(slowPercent));
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Run(duration));
        UpdateSpeed();
    }

    IEnumerator Run(float dur)
    {
        float t = dur;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        _activePct = 0f;
        UpdateSpeed();
        _co = null;
    }

    void UpdateSpeed()
    {
        if (_cc == null) return;
        float mult = 1f - _activePct;
        _cc.speed = _baseSpeed * mult;
    }

    void OnDisable()
    {
        if (_cc != null) _cc.speed = _baseSpeed;
    }
}
