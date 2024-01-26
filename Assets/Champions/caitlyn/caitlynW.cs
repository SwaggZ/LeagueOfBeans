using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class caitlynW : MonoBehaviour
{
    public float throwForce = 10f;
    public float maxUpwardsModifier = 2f;
    public GameObject throwable;
    public GameObject cam;
    public float AutoTime = 10f;

    void Update()
    {
        // Check for input or trigger to activate the ability
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Throw();
        }
    }

    void Throw()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;
        // Instantiate the GameObject
        GameObject grenade = Instantiate(throwable, currentPosition, currentRotation);

        // Get the Rigidbody component
        Rigidbody rb = grenade.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Calculate the upwardsModifier based on the camera's pitch
            float pitch = cam.transform.eulerAngles.x;
            float normalizedPitch = Mathf.Clamp01(pitch / 90f); // Normalize pitch between 0 and 1
            float currentUpwardsModifier = Mathf.Lerp(1f, maxUpwardsModifier, normalizedPitch);

            // Apply the throw force
            rb.AddForce(Vector3.up * currentUpwardsModifier, ForceMode.Impulse);
            rb.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogError("Rigidbody component not found on the instantiated GameObject.");
        }
    }

    void Destroy()
    {
        Destroy(gameObject);
    }
}
