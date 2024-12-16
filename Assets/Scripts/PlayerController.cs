using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class <c>PlayerController</c> models a player controller for the camera
/// </summary>
public class PlayerController : MonoBehaviour
{
    public int speed = 10;
    [SerializeField] private Camera playerCamera;
    public bool lockX, lockY, lockZ;
    private Vector3 startRotation;

    // Start is called before the first frame update
    void Start()
    {
        playerCamera = Camera.main;
        startRotation = playerCamera.transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        CameraMove();
    }

    // Happens after the udpate function
    // https://stackoverflow.com/questions/53035700/how-to-lock-z-rotation-in-unity3d
    void LateUpdate()
    {
        Vector3 newRotation = playerCamera.transform.rotation.eulerAngles;
        playerCamera.transform.rotation = Quaternion.Euler(
            lockX ? startRotation.x : newRotation.x,
            lockY ? startRotation.y : newRotation.y,
            lockZ ? startRotation.z : newRotation.z
        );
    }

    /// <summary>
    /// Function <f>CameraMove</f> allows for the movement of the camera
    /// </summary>
    private void CameraMove()
    {
        // float xSpeed;
        // float zSpeed;
        // // Detect the input and update camera position
        // float cameraRotationX = playerCamera.transform.rotation.eulerAngles.x;
        // if (cameraRotationX > 180)
        // {
        //     xSpeed = (float)Math.Sin(cameraRotationX) * speed;
        //     zSpeed = (float)Math.Cos(cameraRotationX) * speed;
        // }
        // else
        // {
        //     xSpeed = -(float)Math.Sin(cameraRotationX) * speed;
        //     zSpeed = -(float)Math.Cos(cameraRotationX) * speed;
        // }

        // float newCamx = Input.GetAxis("Horizontal") * xSpeed * Time.deltaTime;
        // float newCamX = playerCamera.transform.position.x;
        // float newCamz = Input.GetAxis("Vertical") * zSpeed * Time.deltaTime;
        // float newCamZ = playerCamera.transform.position.z;
        // float newCamY = playerCamera.transform.position.y;
        
        float newCamX = playerCamera.transform.position.x + Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float newCamZ = playerCamera.transform.position.z + Input.GetAxis("Vertical") * speed * Time.deltaTime;
        float newCamY = playerCamera.transform.position.y;
        
        playerCamera.transform.position = new Vector3(newCamX, newCamY, newCamZ);

        if (Input.mouseScrollDelta.y < 0)
            playerCamera.fieldOfView++;
        

        if (Input.mouseScrollDelta.y > 0)
            playerCamera.fieldOfView--;
        
    }
    
    // Using a combination of classic Unity input management and new input management
    // void OnLook(InputValue value)
    // {
    //     Vector3 rotationValue = new Vector3(-value.Get<Vector2>().y, value.Get<Vector2>().x, 0);
    //     Vector3 newRotation = playerCamera.transform.rotation * rotationValue;
    //     if (newRotation.y < -87)
    //     {
    //         newRotation.y = -87;
    //     } else if (newRotation.y > 87)
    //     {
    //         newRotation.y = 87;
    //     }
    //     playerCamera.transform.rotation *= Quaternion.Euler(rotationValue);
    // }
    
}
    
