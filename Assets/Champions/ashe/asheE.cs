using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asheE : MonoBehaviour
{
    public GameObject autoAttack;
    public GameObject cam;

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
        Vector3 currentPosition = transform.position;
        Quaternion centerRotation = cam.transform.rotation;

        // Instantiate a new GameObject at the center
        GameObject centerArrow = Instantiate(autoAttack, currentPosition, centerRotation);

        // Set the initial angle offset
        float angleOffset = 10f;

        // Create 8 more arrows with right and left angle offsets
        for (int i = 1; i <= 4; i++)
        {
            // Right angle offset
            Quaternion rightRotation = centerRotation * Quaternion.Euler(0, angleOffset * i, 0);
            Instantiate(autoAttack, currentPosition, rightRotation);

            // Left angle offset
            Quaternion leftRotation = centerRotation * Quaternion.Euler(0, -angleOffset * i, 0);
            Instantiate(autoAttack, currentPosition, leftRotation);
        }
    }
}
