using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ahriW : MonoBehaviour
{
    public GameObject foxFireContainerPrefab;
    private GameObject foxFireContainer;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CreateFoxFireContainer();
        }
    }

    void CreateFoxFireContainer()
    {
        foxFireContainer = Instantiate(foxFireContainerPrefab, transform.position, Quaternion.identity);
    }
}
