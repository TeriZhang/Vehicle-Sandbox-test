using System.Numerics;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem.iOS;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Global : MonoBehaviour
{
    public GameObject Root;
    public GameObject topPart;
    public GameObject Ground;
    public Rigidbody2D rb;

    void freeze(GameObject part, bool f)
    {
        if (f)
            part.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        else
            part.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
    }

    GameObject newRoot()
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("basicPart"), transform);
        temp.GetComponent<SpriteRenderer>().color = Color.pink;
        temp.name = "Root";
        temp.transform.position = new Vector2(0, 0);
        Destroy(temp.GetComponent<FixedJoint2D>());
        // freeze(temp);
        return temp;
    }
    GameObject newPart(string name, GameObject weldParent, Vector2 anchor)
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("basicPart"), transform);
        temp.transform.position = (Vector2) weldParent.transform.position + anchor;
        FixedJoint2D joint = temp.GetComponent<FixedJoint2D>();
        joint.anchor = anchor;
        joint.connectedBody = weldParent.GetComponent<Rigidbody2D>();
        return temp;
    }
    void Start()
    {
        Root = newRoot();

        topPart = newPart("basicPart", Root, new Vector2(1, 0));
        newPart("basicPart", Root, new Vector2(-1, 0));
        newPart("basicPart", Root, new Vector2(0, 1));

        GameObject topBPart = newPart("basicPart", topPart, new Vector2(1, 0));
        newPart("basicPart", topBPart, new Vector2(1, 0));
        newPart("basicPart", topBPart, new Vector2(0, 1));


        // Root.GetComponent<Rigidbody2D>().AddForce(new Vector2(100, 0));


    }
    void Update()
    {
        
    }
}
