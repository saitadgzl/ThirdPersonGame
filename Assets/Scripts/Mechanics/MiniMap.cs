using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMap : MonoBehaviour
{
    public Transform camera;
    public Transform player;

    void LateUpdate()
    {
        // follow players position
        Vector3 newPosition = player.position;
        newPosition.y = transform.position.y; 
        transform.position = newPosition;

        // follow cam's rotation
        transform.rotation = Quaternion.Euler(90f, camera.eulerAngles.y, 0f);
    }
}