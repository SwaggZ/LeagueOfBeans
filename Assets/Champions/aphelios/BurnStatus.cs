using System.Collections;
using UnityEngine;

// Applies damage over time to the host. Stacking: each apply adds a stack up to a cap.
// Each stack increases DPS. Duration refreshes on each application.
public class BurnStatus : MonoBehaviour
{
    public float baseDamagePerSecond = 2f;
    public int maxStacks = 5;
    
    private int _currentStacks = 0;
    private float _remaining;
    private float _duration;
    private Coroutine _co;
    private HealthSystem _hp;
    private ModifierTracker _modifierTracker;

    void Awake()
    {
        _hp = GetComponent<HealthSystem>();
        _modifierTracker = GetComponent<ModifierTracker>();
    }

    // Apply or add burn stack. Stacking up to maxStacks.
    public void Apply(float duration, float dps)
    {
        if (_hp == null) return;

        baseDamagePerSecond = dps;
        _duration = duration;
        _remaining = duration;
        
        // Add a stack (up to cap)
        _currentStacks = Mathf.Min(_currentStacks + 1, maxStacks);

        if (_co == null)
            _co = StartCoroutine(Run());

        UpdateUI();
    }
    
    private void UpdateUI()
    {
        float totalDps = baseDamagePerSecond * _currentStacks;
        var iconLibrary = FindObjectOfType<ModifiersIconLibrary>(true);
        Sprite icon = iconLibrary != null ? iconLibrary.DMGBURN : null;

        var modifiersUi = FindObjectOfType<ModifiersUIManager>(true);
        if (CompareTag("Player") && modifiersUi != null)
        {
            modifiersUi.AddOrUpdate(
                "StatusBurn",
                icon,
                $"Burning ({totalDps} DPS)",
                Mathf.Max(0.01f, _remaining),
                _currentStacks);
        }

        if (!CompareTag("Player") && _modifierTracker != null)
        {
            _modifierTracker.AddOrUpdate("StatusBurn", icon, Mathf.Max(0.01f, _remaining), _currentStacks);
        }
    }

    IEnumerator Run()
    {
        while (_remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            if (_hp == null) break;
            
            // Calculate damage based on current stacks
            int dmgPerTick = Mathf.Max(0, Mathf.RoundToInt(baseDamagePerSecond * _currentStacks));
            if (dmgPerTick > 0) _hp.TakeDamage(dmgPerTick);
            
            _remaining -= 1f;
            UpdateUI();
        }
        
        // Reset stacks when burn expires
        _currentStacks = 0;
        _co = null;
        var modifiersUi = FindObjectOfType<ModifiersUIManager>(true);
        if (CompareTag("Player") && modifiersUi != null)
        {
            modifiersUi.Remove("StatusBurn");
        }
        if (!CompareTag("Player") && _modifierTracker != null)
        {
            _modifierTracker.Remove("StatusBurn");
        }
    }
}
