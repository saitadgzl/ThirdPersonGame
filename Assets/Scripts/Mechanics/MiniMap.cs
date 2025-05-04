using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMap : MonoBehaviour
{
    public Transform camera;

    void LateUpdate()
    {
        Vector3 newPosition = camera.position;
        newPosition.y = transform.position.y;
        transform.position = newPosition;

        transform.rotation = Quaternion.Euler(90f, camera.eulerAngles.y, 0f);
    }
}
