using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ahriQ : MonoBehaviour
{
    public Transform orbPrefab;

    private Transform orbInstance;
    private Vector3 initialPosition;
    private Vector3 targetPosition;

    void Update()
    {
        // Check for input or trigger to activate the ability
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        // Set initial and target positions
        initialPosition = transform.position;

        // Spawn the orb
        orbInstance = Instantiate(orbPrefab, initialPosition, Quaternion.identity);
    }
}
