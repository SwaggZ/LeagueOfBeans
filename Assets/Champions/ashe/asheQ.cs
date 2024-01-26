using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asheQ : MonoBehaviour
{
    public GameObject autoAttack;
    public GameObject cam;

    void Update()
    {
        // Check for input or trigger to activate the ability
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartCoroutine(RepeatWithDelay());
        }
    }

    void ActivateAbility()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = cam.transform.rotation;

        float randomX = Random.Range(-2f, 2f);
        float randomY = Random.Range(-2f, 2f);

        currentPosition.x += randomX;
        currentPosition.y += randomY;

        // Instantiate a new GameObject using the same position and rotation
        GameObject newObject = Instantiate(autoAttack, currentPosition, currentRotation);
    }

    IEnumerator RepeatWithDelay()
    {
        for (int i = 0; i < 10; i++)
        {
            
                ActivateAbility();

            // Wait for 0.3 seconds at the end of each loop
            yield return new WaitForSeconds(0.025f);
        }
    }
}
