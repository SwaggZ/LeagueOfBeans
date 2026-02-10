using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynAutoMovement : MonoBehaviour
{
    public float speed = 50f; // Speed of the bullet
    public float AutoTime = 5f; // Time before the bullet self-destructs
    public float damage = 20f; // Damage dealt by the bullet
    public bool isPlane = false; // Legacy: applies knockback + stun when true
    public bool applyKnockback = false; // Independent knockback toggle
    public bool applyStun = false; // Independent stun toggle
    public float knockbackDistance = 5f; // Distance the enemy moves backward
    public float knockbackSpeed = 20f; // Speed of the knockback movement
    public float stunDuration = 2f; // Duration of the stun effect
    public int piercing = 1; // Number of enemies the bullet can pierce through
    public float maxDistance = 0f; // 0 = unlimited distance

    private int enemiesHit = 0; // Tracks how many enemies have been hit
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        Invoke("DestroySelf", AutoTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, speed * Time.deltaTime))
        {
            Debug.Log($"Bullet collided with: {hit.collider.gameObject.name}");

            HealthSystem healthSystem = hit.collider.gameObject.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(damage);
                enemiesHit++; // Increment the hit counter
                Debug.Log($"Enemy hit: {hit.collider.gameObject.name}. Total hits: {enemiesHit}");
            }

            bool doKnockback = applyKnockback || isPlane;
            bool doStun = applyStun || isPlane;

            if (doKnockback || doStun)
            {
                GameObject enemyObj = ModifierUtils.ResolveTarget(hit.collider);

                Vector3 knockbackDir = hit.collider.transform.position - transform.position;

                DummyController dummyCtrl = enemyObj.GetComponent<DummyController>();
                if (dummyCtrl != null)
                {
                    if (doKnockback)
                    {
                        dummyCtrl.ApplyKnockback(knockbackDir, knockbackDistance, knockbackSpeed);
                    }
                    if (doStun)
                    {
                        dummyCtrl.Stun(stunDuration);
                    }
                }
                else
                {
                    CharacterControl enemyController = enemyObj.GetComponent<CharacterControl>();
                    if (enemyController != null)
                    {
                        if (doKnockback)
                        {
                            float stunForKnockback = doStun ? stunDuration : 0f;
                            enemyController.ApplyKnockback(knockbackDir, knockbackDistance, knockbackSpeed, stunForKnockback);
                        }
                        else if (doStun)
                        {
                            enemyController.Stun(stunDuration);
                        }
                    }
                }
            }

            // Destroy the bullet if it has hit the maximum number of enemies
            if (piercing > 0 && enemiesHit >= piercing)
            {
                Destroy(gameObject);
            }
        }

        if (maxDistance > 0f && Vector3.Distance(startPos, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
