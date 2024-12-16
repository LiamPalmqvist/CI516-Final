using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinnerControl : MonoBehaviour
{
    public int spinSpeed = 30;

    [SerializeField] private bool isSet = false;

    [SerializeField] public GameObject assignedObject = null;

    public Vector2 lastClicked;
    // Start is called before the first frame update
    void Start()
    {
        transform.Rotate(-90, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        Vector3 rotation = new Vector3(0 , 0, spinSpeed * Time.deltaTime);
        transform.Rotate(rotation);
        
        //Vector3 position = transform.position;
        //position += new Vector3(0, (float)Math.Sin(Time.deltaTime), 0);
        //transform.position = position;

        if (isSet)
        {
            transform.position = new Vector3(assignedObject.transform.position.x, assignedObject.transform.position.y + 2f, assignedObject.transform.position.z);
        }
    }

    public bool IsSpinnerSet()
    {
        return isSet;
    }

    public void SetSpinner(bool set)
    {
        isSet = set;
    }
}
