using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class properties : MonoBehaviour
{   
    public string Category;
    public AttachPoint ParentAttachpoint;
    public List<AttachPoint> AttachPoints = new List<AttachPoint>();
    public List<KeyCode> Control_Keybinds = new List<KeyCode>();
    public int Damage;
    public int Health;
    public int Speed;

    /// <summary>
    /// reference to the GameObject.
    /// </summary>
    public GameObject Object;

    public GameObject AssemblyRoot;

    public Vector2 PivotOffset;
    
    public string PartType;
    public HingeJoint2D HingeRef;

    public Vector3 getPosition()
    {
        return PivotOffset;
    }

    public CollisionUnityEvent onCollisionEnterEvent = new CollisionUnityEvent();

    public void OnCollisionEnter2D(Collision2D collision)
    {
        onCollisionEnterEvent?.Invoke(collision);
    }

}
