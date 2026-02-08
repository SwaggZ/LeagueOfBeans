using UnityEngine;

public static class DamagePopupSpawner
{
    public static void Spawn(Vector3 worldPosition, float amount, bool crit = false)
    {
        Spawn(worldPosition, Mathf.RoundToInt(amount).ToString(), new Color(1f, 0.93f, 0.3f, 1f), crit);
    }

    public static void Spawn(Vector3 worldPosition, string text, Color color, bool crit = false)
    {
        var go = new GameObject("DamagePopup");
        // Slight random offset
        go.transform.position = worldPosition + new Vector3(
            Random.Range(-0.1f, 0.1f),
            Random.Range(0.0f, 0.15f),
            Random.Range(-0.1f, 0.1f));

        var popup = go.AddComponent<DamagePopup>();
        popup.Init(text, color, Camera.main, crit);
    }
}
