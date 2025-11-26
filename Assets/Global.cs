using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.iOS;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Global : MonoBehaviour
{
    public GameObject Root;
    public GameObject topPart;
    public Rigidbody2D rb;
    public List<GameObject> listOfParts;

    public string gameState;

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
        temp.transform.position = new Vector2(0, 2);
        freeze(temp, true);
        return temp;
    }
    public GameObject newPart(string name, GameObject weldParent, Vector2 anchor)
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("basicPart"), transform);
        temp.name = "basicPart";
        temp.transform.position = (Vector2)weldParent.transform.position + anchor;
        temp.transform.parent = weldParent.transform;

        listOfParts.Add(temp);

        freeze(temp, true); // temporary
        return temp;
    }
    void CreateVehicle()
    {
        Root = newRoot();

        newPart("basicPart", Root, new Vector2(-1, 0));
        newPart("basicPart", Root, new Vector2(0, 1));
        topPart = newPart("basicPart", Root, new Vector2(1, 0));

        GameObject topBPart = newPart("basicPart", topPart, new Vector2(1, 0));
        newPart("basicPart", topBPart, new Vector2(1, 0));
        newPart("basicPart", topBPart, new Vector2(0, 1));
    }

    void Start()
    {
        CreateVehicle();

        //add force to vehicle distance
        Root.GetComponent<Rigidbody2D>().AddForce(new Vector2(100, 0));

        //Building system. Build parts onto attachment points, delete parts, adjusting parts.


        //select an attachment point on the body with the mouse.
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        double distance;
        double closestDist = int.MaxValue;
        Vector2 closestAttachPoint;

        Vector2 AttachPoint = new Vector2(0, 1); // temporary
        foreach (GameObject Part in listOfParts)
        {
            // foreach (Vector2 AttachPoint in Part)
            // {
            // }
            distance = (mousePos - AttachPoint).magnitude;
            if (distance < 3 && distance < closestDist)
            {
                closestDist = distance;
                closestAttachPoint = AttachPoint;
            }
        }

    }
    void Update()
    {
        
    }
}
