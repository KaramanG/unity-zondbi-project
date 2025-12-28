using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5.0f;
    [SerializeField] private float xSpeed = 120.0f;
    [SerializeField] private float ySpeed = 120.0f;

    [SerializeField] private float yMinLimit = -20f;
    [SerializeField] private float yMaxLimit = 80f;

    [SerializeField] private CharacterMovement characterMovement;
    [SerializeField] private MouseLock mouseLock;

    private float x = 0.0f;
    private float y = 0.0f;
    private float fixedYPosition; 

    void Start()
    {
        if (target == null)
        {
            enabled = false;
            return;
        }
        
        if (characterMovement == null)
        {
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        fixedYPosition = transform.position.y; 
    }

    void LateUpdate()
    {
        if (target)
        {
            if (!mouseLock.IsCursorVisible())
            {
                x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
                y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

                y = ClampAngle(y, yMinLimit, yMaxLimit); 
            }

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 targetPosition = target.position; 

            Vector3 position = rotation * negDistance + targetPosition;

            
            if (characterMovement.GetJumpState())
            {
                position.y = fixedYPosition; 
            }
            else
            {
                fixedYPosition = position.y; 
            }

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
