using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPan : MonoBehaviour
{
    const float minRadians = 0f;
    const float maxRadians = Mathf.PI / 2;
    const float deltaRadians = 0.02f;
    private float radius, radians, percentArc;
    private float x, y;

    void Start()
    {
        x = 9f; // 9.5f
        y = 7f; // 7.15f
        radians = Mathf.Atan2(y, x);
        percentArc = radians / maxRadians;

        radius = Mathf.Sqrt(Mathf.Pow(y, 2) + Mathf.Pow(x, 2));

        MoveCamera();
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            radians = Mathf.Clamp(radians + deltaRadians, minRadians, maxRadians); // convert to 0-100 perc
            percentArc = radians / maxRadians;
            MoveCamera();
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            radians = Mathf.Clamp(radians - deltaRadians, minRadians, maxRadians);
            percentArc = radians / maxRadians;
            MoveCamera();
        }
    }

    void MoveCamera()
    {
        x = radius * Mathf.Cos(radians);
        y = radius * Mathf.Sin(radians) + 3f;

        float angle = 0.75f - (percentArc * 3f);
        transform.position = new Vector3(0, y, -x);
        transform.LookAt(new Vector3(0, angle, 0));

        // GameManager.Players[0].GetComponent<Human>().setView();
        foreach (GameObject tile in GameManager.Players[0].Hand)
        {
            // use lookrotation and put computation outside forloop
            tile.transform.LookAt(new Vector3(
                tile.transform.position.x, 
                transform.position.y - 2f, 
                transform.position.z),
            tile.transform.rotation * Vector3.up);

            Vector3 hoverHeight = tile.transform.position;
            hoverHeight.y = 0.157f - (percentArc * 0.157f);

            tile.transform.position = hoverHeight;

            tile.GetComponent<DragTile>().UpdateBasePosition(tile.transform.position, tile.transform.rotation);

            // tile.transform.position.y = 

            // set baserotation set hoveroffset
        }
    }
}
