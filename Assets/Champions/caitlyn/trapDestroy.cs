using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class trapDestroy : MonoBehaviour
{
    public float AutoTime = 30f;

    void Start()
    {
        Invoke("Destroy", AutoTime);
    }

    void Destroy()
    {
        Destroy(gameObject);
    }
}
