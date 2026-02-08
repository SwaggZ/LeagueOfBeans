using System.Collections;
using UnityEngine;

// Applies damage over time to the host. Non-stackable: re-applying refreshes duration and keeps the current DPS.
public class BurnStatus : MonoBehaviour
{
    public float damagePerSecond = 2f;
    private float _remaining;
    private Coroutine _co;
    private HealthSystem _hp;

    void Awake()
    {
        _hp = GetComponent<HealthSystem>();
    }

    // Apply or refresh burn. Non-stackable.
    public void Apply(float duration, float dps)
    {
        if (_hp == null) return;
        damagePerSecond = dps;
        _remaining = duration;
        if (_co == null)
            _co = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // Tick whole-number damage once per second
        int dmgPerTick = Mathf.Max(0, Mathf.RoundToInt(damagePerSecond));
        while (_remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            if (_hp == null) break;
            if (dmgPerTick > 0) _hp.TakeDamage(dmgPerTick);
            _remaining -= 1f;
        }
        _co = null;
        // Optionally remove component; keep it lightweight
        // Destroy(this);
    }
}
