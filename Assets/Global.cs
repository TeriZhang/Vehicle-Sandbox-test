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
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Global : MonoBehaviour
{
    public GameObject Root;
    public GameObject topPart;
    public Rigidbody2D rb;
    public List<GameObject> listOfParts;

    string gameState;

    string userActionState = "Idle"; // Build, Delete, Idle

    public string SelectedPart;

    public GameObject ghostPart;

    public GameObject BuildingUI;

    public GameObject BlockButton;

    void freeze(GameObject part, bool f)
    {
        if (f)
            part.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        else
            part.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
    }

    GameObject newRoot()
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("Parts/Block"), transform);
        temp.GetComponent<SpriteRenderer>().color = Color.pink;
        temp.name = "Root";
        temp.transform.position = new Vector2(0, 2);
        return temp;
    }
    public GameObject newPart(string name, GameObject weldParent, Vector2 anchor)
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("Parts/Block"), transform);
        temp.name = "Block";
        Destroy(temp.GetComponent<Rigidbody2D>());
        temp.transform.position = (Vector2)weldParent.transform.position + anchor;
        temp.transform.parent = weldParent.transform;

        listOfParts.Add(temp);

        return temp;
    }
    void CreateVehicle()
    {
        Root = newRoot();

        newPart("Block", Root, new Vector2(-1, 0));
        newPart("Block", Root, new Vector2(0, 1));
        topPart = newPart("Block", Root, new Vector2(1, 0));

        GameObject topBPart = newPart("Block", topPart, new Vector2(1, 0));
        newPart("Block", topBPart, new Vector2(1, 0));
        newPart("Block", topBPart, new Vector2(0, 1));
    }

    void snapToAttachPoint()
    {
                //select an attachment point on the body with the mouse.
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        double distance;
        double closestDist = int.MaxValue;
        Vector2 closestAttachPoint = new Vector2();
        GameObject closestPart;

        foreach (GameObject Part in listOfParts)
        {
            foreach (Vector2 AttachPoint in Part.GetComponent<properties>().AttachPoints)
            {
                distance = (mousePos - AttachPoint).magnitude;
                if (distance < 3 && distance < closestDist)
                {
                    closestDist = distance;
                    closestAttachPoint = AttachPoint;
                    closestPart = Part;
                }
            }
        }
        if (closestAttachPoint != new Vector2())
        {
            Debug.Log(closestAttachPoint);
        }
        else
        {
            Debug.Log("none");
        }
    }
    
    void SelectPart(string PartName)
    {
        userActionState = 
        SelectedPart = PartName;
        ghostPart = Resources.Load<GameObject>("Parts/" + PartName);
    }

    void onLeftMouseDown()
    {
        
    }
    void onLeftMouseUp()
    {
        
    }

    void Start()
    {
        CreateVehicle();

        //add force to vehicle distance
        Root.GetComponent<Rigidbody2D>().AddForce(new Vector2(100, 0));

        //Building system. Build parts onto attachment points, delete parts, adjusting parts.

        BlockButton.GetComponent<Button>().onClick.AddListener(() => SelectPart("Block"));


        
    }
    void Update()
    {
        
    }
}
