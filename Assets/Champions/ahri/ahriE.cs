using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ahriE : MonoBehaviour
{
    public GameObject Eheart;
    public GameObject cam;

    void Update()
    {
        // Check for input or trigger to activate the ability
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            ActivateAbility();
        }
    }

    void ActivateAbility()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        // Instantiate a new GameObject using the same position and rotation
        GameObject newObject = Instantiate(Eheart, currentPosition, currentRotation);
    }
}
