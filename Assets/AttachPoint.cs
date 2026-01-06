using System;
using UnityEngine;

[System.Serializable]
public class AttachPoint
{
    public GameObject Parent;
    public Vector3 offset;
    public float rotation;
    public float autoRotation;
    public GameObject occupied;

    public AttachPoint()
    {
    }
    public AttachPoint(float x, float y)
    {
        offset = new Vector3(x, y, 0);
    }
}
