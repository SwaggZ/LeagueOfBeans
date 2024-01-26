using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbMovement : MonoBehaviour
{
    public Transform player;
    private Transform cam;
    private bool isReturning = false;
    public float speed = 10f;

    private Vector3 initialPosition;
    private Vector3 targetPosition;

    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        initialPosition = transform.position;
        targetPosition = transform.position + cam.forward * speed; // Adjust the distance as needed
    }

    void Update()
    {
        if(transform.position != targetPosition && isReturning == false)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, speed * 3f * Time.deltaTime);
        }

        if(transform.position == targetPosition)
        {
            isReturning = true;
        }

        if(isReturning && transform.position == player.position)
        {
            Destroy(gameObject);
        }
    }
}