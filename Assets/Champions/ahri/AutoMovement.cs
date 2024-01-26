using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoMovement : MonoBehaviour
{
    public float speed = 10f;
    public float AutoTime = 5f;

    void Start()
    {
        Invoke("Destroy", AutoTime);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void Destroy()
    {
        Destroy(gameObject);
    }
}
