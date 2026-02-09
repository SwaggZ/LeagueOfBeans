using System.Collections;
using UnityEngine;

// Applies damage over time to the host. Non-stackable: re-applying refreshes duration and keeps the current DPS.
public class BurnStatus : MonoBehaviour
{
    public float damagePerSecond = 2f;
    private float _remaining;
    private Coroutine _co;
    private HealthSystem _hp;
    private ModifierTracker _modifierTracker;

    void Awake()
    {
        _hp = GetComponent<HealthSystem>();
        _modifierTracker = GetComponent<ModifierTracker>();
    }

    // Apply or refresh burn. Non-stackable.
    public void Apply(float duration, float dps)
    {
        if (_hp == null) return;

        damagePerSecond = dps;
        _remaining = duration;

        if (_co == null)
            _co = StartCoroutine(Run());

        // Player UI (unchanged)
        if (CompareTag("Player") && ModifiersUIManager.Instance != null)
        {
            Sprite icon = ModifiersIconLibrary.Instance != null
                ? ModifiersIconLibrary.Instance.DMGBURN
                : null;

            ModifiersUIManager.Instance.AddOrUpdate(
                "StatusBurn",
                icon,
                "Burning",
                Mathf.Max(0.01f, duration),
                0
            );
        }

        // Dummy / enemy modifier icon
        if (!CompareTag("Player") && _modifierTracker != null && ModifiersIconLibrary.Instance != null)
        {
            Sprite icon = ModifiersIconLibrary.Instance.DMGBURN;
            _modifierTracker.AddOrUpdate("StatusBurn", icon, duration);
        }
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
        if (CompareTag("Player") && ModifiersUIManager.Instance != null)
        {
            ModifiersUIManager.Instance.Remove("StatusBurn");
        }
        // Optionally remove component; keep it lightweight
        // Destroy(this);
    }
}
