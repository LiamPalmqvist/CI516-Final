using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        transform.localEulerAngles = new Vector3(90, playerCamera.transform.eulerAngles.y-180, 0);
        transform.position = new Vector3(transform.localScale.x / 2, transform.position.y, transform.position.z);
    }
}
