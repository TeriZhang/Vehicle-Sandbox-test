using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

public class Global : MonoBehaviour
{
    public GameObject Root;
    public GameObject topPart;

    public GameObject World;
    public List<GameObject> listOfParts;

    public string gameState = "Building";

    public string userActionState = "Idle"; // Build, Remove, Idle

    public string SelectedPart;

    public GameObject ghostPart;

    public GameObject BuildingUI;

    public GameObject BlockButton;

    AttachPoint closestAttachPoint;
    GameObject closestPart;

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

        listOfParts.Add(temp);
        return temp;
    }
    public GameObject newPart(string name, GameObject weldParent, AttachPoint attachPoint)
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("Parts/Block"), transform);
        temp.name = "Block";
        Destroy(temp.GetComponent<Rigidbody2D>());
        temp.transform.position = weldParent.transform.position + attachPoint.offset;
        temp.transform.rotation = new Quaternion(); // 
        temp.transform.parent = weldParent.transform;

        Rigidbody2D rb = temp.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        listOfParts.Add(temp);

        return temp;
    }
    void CreateVehicle()
    {
        Root = newRoot();

        newPart("Block", Root, new AttachPoint(-1, 0));
        newPart("Block", Root, new AttachPoint(0, 1));
        topPart = newPart("Block", Root, new AttachPoint(1, 0));

        GameObject topBPart = newPart("Block", topPart, new AttachPoint(1, 0));
        newPart("Block", topBPart, new AttachPoint(1, 0));
        newPart("Block", topBPart, new AttachPoint(0, 1));
    }

    Vector2 getMousePos()
    {
        return Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    void getSnap()
    {
        //snap to an attachment point on the body with the mouse.
        Vector3 mousePos = getMousePos();
        double distance;
        double closestDist = int.MaxValue;
        closestPart = null;

        foreach (GameObject Part in listOfParts)
        {
            foreach (AttachPoint AttachPoint in Part.GetComponent<properties>().AttachPoints)
            {
                distance = (mousePos - (AttachPoint.offset + Part.transform.position)).magnitude;
                if (distance < 1 && distance < closestDist) // distance constraint: distance < 3 &&
                {
                    Debug.Log(distance);
                    closestDist = distance;
                    closestAttachPoint = AttachPoint;
                    closestPart = Part;
                }
            }
        }
    }
    
    void SelectPart(string PartName)
    {
        userActionState = "Build";
        SelectedPart = PartName;
        ghostPart = Instantiate(Resources.Load<GameObject>("Parts/" + PartName));
        ghostPart.transform.parent = World.transform;

        SpriteRenderer sr = ghostPart.GetComponent<SpriteRenderer>();
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);

        Rigidbody2D rb = ghostPart.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        ghostPart.GetComponent<Collider2D>().enabled = false;

    }

    public void onLeftMouseDown(InputAction.CallbackContext ctx)
    {
        Debug.Log("place_hit");
        if (ctx.performed && userActionState == "Build" && closestPart)
        {
            Debug.Log("place_activate");
            userActionState = "Idle";
            Destroy(ghostPart);
            newPart("Block", closestPart, closestAttachPoint);
        }
    }
    public void onLeftMouseUp(InputAction.CallbackContext ctx)
    {
    }

    void Start()
    {
        CreateVehicle();

        Rigidbody2D rb = Root.GetComponent<Rigidbody2D>();

        //add force to vehicle
        rb.AddForce(new Vector2(100, 0));
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        //Building system. Build parts onto attachment points, delete parts, adjusting parts.

        BlockButton.GetComponent<Button>().onClick.AddListener(() => SelectPart("Block"));
    }
    void Update()
    {
        switch (userActionState)
        {
            case "Idle":
                break;
            case "Build":
                getSnap();
                if (closestPart)
                {
                    ghostPart.transform.position = closestPart.transform.position + closestAttachPoint.offset;
                }
                else
                {
                    ghostPart.transform.position = getMousePos();
                }
                break;
            case "Remove":
                break;
        }
    }
}
