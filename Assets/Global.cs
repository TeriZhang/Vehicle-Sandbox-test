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
using UnityEditor.U2D.Aseprite;
using Unity.Mathematics;

public class Global : MonoBehaviour
{

    public GameObject World;
    public string gameState = "Building";


    public Assembly assembly;
    AttachPoint closestAttachPoint;
    GameObject closestPart;


    public string userActionState = "Idle"; // Build, Remove, Idle

    public string SelectedPart;

    public GameObject ghostPart;

    public GameObject BuildingUI;

    public List<string> Inventory; // "What the player can build with" represented as a list of part names.

    /// <summary>
    /// Completely freezes a part's RigidBody
    /// </summary>
    /// <param name="part"></param>
    /// <param name="freezeState"></param>
    void Freeze(GameObject part, bool freezeState)
    {
        if (freezeState)
            part.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        else
            part.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
    }

    /// <summary>
    /// Creates a root for the assembly (need to figure out how to put this function in the Assembly class)
    /// </summary>
    /// <returns></returns>
    GameObject createRoot()
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("Parts/Engine"), transform);
        temp.name = "Root";
        temp.transform.position = new Vector2(0, 2);
        temp.transform.parent = World.transform;

        Freeze(temp, true);

        assembly.AddRoot(temp);
        return temp;
    }
    public GameObject newPart(string name, GameObject weldParent, AttachPoint attachPoint)
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("Parts/" + name), transform);
        temp.name = name;
        Destroy(temp.GetComponent<Rigidbody2D>());
        temp.transform.position = weldParent.transform.position + attachPoint.offset;
        temp.transform.rotation = Quaternion.Euler(new Vector3(0, 0, attachPoint.rotation));
        temp.transform.parent = weldParent.transform;
        attachPoint.occupied = temp;

        Rigidbody2D rb = temp.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        assembly.Add(temp);

        return temp;
    }
    void newVehicle()
    {
        assembly = new Assembly();
        assembly.root = createRoot();

        // construct a template vehicle
        // GameObject Root = assembly.root;
        // newPart("Block", Root, new AttachPoint(-1, 0));
        // newPart("Block", Root, new AttachPoint(0, 1));
        // GameObject topPart = newPart("Block", Root, new AttachPoint(1, 0));

        // GameObject topBPart = newPart("Block", topPart, new AttachPoint(1, 0));
        // newPart("Block", topBPart, new AttachPoint(1, 0));
        // newPart("Block", topBPart, new AttachPoint(0, 1));
    }


    Vector2 getMousePos()
    {
        return Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    /// <summary>
    /// updates 'closestPart' and 'closestAttachPoint' relative to the mouse.
    /// used to snap to an attachment point on the body.
    /// </summary>
    void getSnap()
    {
        Vector3 mousePos = getMousePos();
        double distance;
        double closestDist = int.MaxValue;
        closestPart = null;

        //TASK: add a constraint that doesn't allow a part to overlap other parts

        foreach (GameObject Part in assembly.cachedParts)
        {
            foreach (AttachPoint AttachPoint in Part.GetComponent<properties>().AttachPoints)
            {
                if (!AttachPoint.occupied)
                {
                    distance = (mousePos - (AttachPoint.offset + Part.transform.position)).magnitude;
                    if (distance < 1 && distance < closestDist)
                    {
                        closestDist = distance;
                        closestAttachPoint = AttachPoint;
                        closestPart = Part;
                    }
                }
            }
        }
    }
    
    // used with the buttons in the inventory.
    public void SelectPart(string PartName)
    {
        if (gameState == "Building")
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
    }

    // side menu button
    public void RemoveButtonPress()
    {
        if (gameState == "Building")
        {
            userActionState = "Remove";
            Debug.Log(userActionState);
        }
    }

    // side menu button
    public void CombatButtonPress()
    {
        Debug.Log("dededede");
        switch (gameState)
        {
            case "Building":
                gameState = "Combat";
                Freeze(assembly.root, false);
                assembly.rb.AddForce(new Vector2(0, -100));
                break;
            case "Combat":
                gameState = "Building";
                assembly.root.transform.position = new Vector3(0, 2, 0);
                assembly.root.transform.rotation = new Quaternion(0, 0, 0, 1);
                Freeze(assembly.root, true);
                break;

        }
    }

    //currently used to add parts and remove parts
    public void onLeftMouseDown(InputAction.CallbackContext ctx)
    {
        // Debug.Log("place_hit");
        if (ctx.performed &&  gameState == "Building")
        {
        switch (userActionState)
        {
            case "Build":
                if (closestPart)
                {
                    // Debug.Log("place_activate");
                    userActionState = "Idle";
                    Destroy(ghostPart);
                    newPart(SelectedPart, closestPart, closestAttachPoint);
                }
                break;

            case "Remove":
                getSnap(); // TASK: change this to check for closest part, not attach point.
                if (closestPart && closestPart != assembly.root)
                {
                    userActionState = "Idle";
                    closestAttachPoint.occupied = null;
                    assembly.Remove(closestPart);
                    Destroy(closestPart);
                }
                break;
        }
        }
    }

    public void onLeftMouseUp(InputAction.CallbackContext ctx)
    {
    }

    void Start()
    {
        newVehicle();

        Inventory = new List<string>();

        Inventory.Add("Block");
        Inventory.Add("AngledBlock");
        Inventory.Add("Wheel");
        Inventory.Add("Spike");
        Inventory.Add("HugeWheel");

        //Creates the buttons in the inventory UI.
        for (int i = 0; i < Inventory.Count; i++)
        {
            string PartName = Inventory[i];
            GameObject Button = Instantiate(Resources.Load<GameObject>("UI/PartButton"));

            Button.transform.SetParent(BuildingUI.transform.Find("Panel").transform);
            Button.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240 + 60 * i, 20);
            Button.GetComponent<Image>().sprite = Resources.Load<Sprite>("Graphics/" + PartName);

            Button.GetComponent<Button>().onClick.AddListener(() => SelectPart(PartName));
        }
    }

    void Update()
    {
        
        switch (gameState)
        {
            case "Building":
                switch (userActionState)
                {
                    case "Idle":
                        break;
                    case "Build":
                        // when the player drags ghost parts onto the assembly
                        getSnap();
                        if (ghostPart)
                        {
                            if (closestPart)
                            {
                                ghostPart.transform.position = closestPart.transform.position + closestAttachPoint.offset;
                                ghostPart.transform.rotation = Quaternion.Euler(new Vector3(0, 0, closestAttachPoint.rotation));
                            }
                            else
                            {
                                ghostPart.transform.position = getMousePos();
                            }
                        }
                        break;
                }
                break;
            
            case "Combat":
                // allows player control of the vehicle(assembly) in combat
                assembly.rb.AddForce(new Vector3(
                    Input.GetAxis("Horizontal") * 5, 
                    Input.GetAxis("Vertical") * 5, 
                    0));
                break;
        }
    }
}
