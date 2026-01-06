using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class properties : MonoBehaviour
{   
    public string Category;
    public AttachPoint ParentAttachpoint;
    public List<AttachPoint> AttachPoints = new List<AttachPoint>();
    public List<Keybind> Control_Keybinds = new List<Keybind>();
    public int Damage;
    public int Health;
    public int Speed;

    public float editRotation;
    // public float[] possibleAngles;

    public float partOffsetRotation;
    public bool doesAutoRotate;

    public string hierPath = "";

    /// <summary>
    /// reference to the GameObject.
    /// </summary>
    public GameObject Object;

    public GameObject AssemblyRoot;

    public Vector2 PivotOffset; // unused
    
    public PartType partType;
    public HingeJoint2D HingeRef;

    public Vector3 getPosition() // unused
    {
        return PivotOffset;
    }

    public CollisionUnityEvent onCollisionEnterEvent = new CollisionUnityEvent();

    public void OnCollisionEnter2D(Collision2D collision)
    {
        onCollisionEnterEvent?.Invoke(collision);
    }

}
