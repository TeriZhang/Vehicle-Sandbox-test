using UnityEngine;

public class AttachPoint
{
    public GameObject Parent;
    public Vector3 offset;
    public float rotation;
    public bool occupied;

    public AttachPoint()
    {
    }
    public AttachPoint(float x, float y)
    {
        offset = new Vector3(x, y, 0);
    }
}
