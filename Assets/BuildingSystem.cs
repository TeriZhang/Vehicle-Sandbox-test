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
using Unity.Properties;
using System.ComponentModel.Design;

public class Global : MonoBehaviour
{
    public GameObject World;
        public Camera mainCamera;
    public string gameState = "Building"; // Building Combat


    public Assembly assembly;
    AttachPoint closestAttachPoint;
    GameObject closestPart;


    public string userActionState = "Idle"; // Build, Remove, Idle

    public string SelectedPart;

    public GameObject ghostPart;

    public GameObject BuildingUI;

    public bool CameraSyncToVehicle;

    public List<string> Inventory; // "What the player can build with" represented as a list of part names.

    public bool Keybind_CheckInput = false;

    public GameObject selected;

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
        temp.name = name + " " + assembly.PartList.Count;
        temp.transform.position = weldParent.transform.position + (weldParent.transform.rotation * attachPoint.offset);
        temp.GetComponent<properties>().ParentAttachpoint = attachPoint;
        temp.transform.rotation = Quaternion.Euler(new Vector3(0, 0, attachPoint.rotation + weldParent.transform.rotation.eulerAngles.z));
        temp.transform.parent = weldParent.transform;
        attachPoint.occupied = temp;
        
        properties props = temp.GetComponent<properties>();

        switch (props.PartType)
        { 
            case "Wheel":
                //first destroy the previous Hinge Joint.
                Destroy(temp.GetComponent<HingeJoint2D>());
                Destroy(temp.GetComponent<Rigidbody2D>());


                //Create new
                HingeJoint2D HingeJoint = assembly.root.AddComponent<HingeJoint2D>();
                HingeJoint.useMotor = true;
                JointMotor2D motor = HingeJoint.motor;
                motor.motorSpeed = 0;
                motor.maxMotorTorque = 3000;
                HingeJoint.motor = motor;
                HingeJoint.connectedBody = temp.transform.Find("Tire").gameObject.GetComponent<Rigidbody2D>();

                props.HingeRef = HingeJoint;

                break;
        }
        Destroy(temp.GetComponent<Rigidbody2D>());

        // Rigidbody2D rb = temp.GetComponent<Rigidbody2D>();
        // rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        assembly.Add(temp);

        return temp;
    }
    void newVehicle()
    {
        assembly = new Assembly();
        assembly.root = createRoot();

        // construct a template vehicle
        // GameObject Root = assembly.root;
        // newPart("Block", Root, Root.GetComponent<properties>().AttachPoints[3]);
        // GameObject topPart = newPart("Block", Root, Root.GetComponent<properties>().AttachPoints[1]);
        // newPart("Block", topPart, topPart.GetComponent<properties>().AttachPoints[1]);
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
        
        // Console.WriteLine("Hi Console");
        // Debug.Log("Hi Debug");
        // foreach (GameObject Part in assembly.PartList)
        // {
        //     Debug.Log(Part);
        // }
        // Debug.Log("---");
        foreach (GameObject Part in assembly.PartList)
        {
            if (!Part) {continue;}
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

    GameObject nearestPart()
    {
        Vector3 mousePos = getMousePos();
        double distance;
        double closestDist = int.MaxValue;
        GameObject nearest = null;

        //TASK: add a constraint that doesn't allow a part to overlap other parts

        foreach (GameObject Part in assembly.PartList)
        {
            if (!Part) {continue;}
            distance = (mousePos - Part.transform.position).magnitude;
            if (distance < 1 && distance < closestDist)
            {
                closestDist = distance;
                nearest = Part;
            }
        }
        return nearest;
    }

    // functions

    public GameObject getPhysical(GameObject PartP) // for HugeWheel bug. decided not to use for now.
    {
        return PartP.transform.Find("Physical").gameObject;
    }

    int removeiterate_iterations;
    public void removeIterate(GameObject PartP)
    {
        properties props = PartP.GetComponent<properties>();
        foreach (AttachPoint attachPoint in props.AttachPoints)
        {
            removeiterate_iterations++;
            if (removeiterate_iterations > assembly.PartList.Count) {break;}

            if (attachPoint.occupied)
            {
                removeIterate(attachPoint.occupied);
            }
        }
        assembly.Remove(PartP);
        props.ParentAttachpoint.occupied = null;
        Destroy(PartP);
    }

    
    // used with the buttons in the inventory.
    public void SelectPart(string PartName)
    {
        if (gameState == "Building")
        {
            if (ghostPart)
            {
                Destroy(ghostPart);
            }
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
        }
    }

    // side menu button
    public void CombatButtonPress()
    {
        switch (gameState)
        {
            case "Building":
                gameState = "Combat";
                Freeze(assembly.root, false);
                assembly.rb.AddForce(new Vector2(0, -100));
                CameraSyncToVehicle = true;
                // new Transform().Rotate(0, 90, 0, Space.World);
                break;
            case "Combat":
                gameState = "Building";
                assembly.root.transform.position = new Vector3(0, 2, 0);
                assembly.root.transform.rotation = new Quaternion(0, 0, 0, 1);
                CameraSyncToVehicle = false;
                Freeze(assembly.root, true);
                mainCamera.transform.position = new Vector3(0, 2, -10);
                break;

        }
    }

    // side menu button
    public void KeybindButtonPress()
    {
        if (gameState == "Building")
        {
            userActionState = "Keybind";
        }
    }

    // side menu button
    public void RotateButtonPress()
    {
        if (gameState == "Building")
        {
            userActionState = "Rotate";
        }
    }
    // side menu button
    public void OffButtonPress()
    {
        if (gameState == "Building")
        {
            userActionState = "Idle";
        }
    }

    //currently used to add parts, remove parts, and adjust parts.
    public void onLeftMouseDown(InputAction.CallbackContext ctx)
    {
        // Debug.Log("place_hit");
        if (ctx.performed &&  gameState == "Building")
        {
            
            properties props;
            switch (userActionState)
            {
                case "Build":
                    if (closestPart)
                    {
                        // Debug.Log("place_activate");
                        userActionState = "Idle";
                        Destroy(ghostPart);
                        closestAttachPoint.occupied = newPart(SelectedPart, closestPart, closestAttachPoint);
                    }
                    break;

                case "Remove":
                    selected = nearestPart();
                    if (selected && selected != assembly.root)
                    {
                        props = selected.GetComponent<properties>();
                        userActionState = "Idle";
                        props.ParentAttachpoint.occupied = null;

                        // List<GameObject> temp = new List<GameObject>();

                        removeIterate(selected);
                    }
                    break;
                case "Keybind":
                    selected = nearestPart();
                    if (selected)
                    {
                        props = selected.GetComponent<properties>();
                        if (props.Category == "Control")
                        {
                            // props.Control_Keybinds = ; // has to wait for Update()

                            Keybind_CheckInput = true;
                        }
                    }
                    break;

                case "Rotate":
                    if (nearestPart())
                    {
                        nearestPart().transform.rotation *= Quaternion.Euler(new Vector3(0, 0, 90));
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
                                ghostPart.transform.position = closestPart.transform.position + (closestPart.transform.rotation * closestAttachPoint.offset);
                                ghostPart.transform.rotation = Quaternion.Euler(new Vector3(0, 0, closestAttachPoint.rotation + closestPart.transform.rotation.eulerAngles.z));
                            }
                            else
                            {
                                ghostPart.transform.position = getMousePos();
                            }
                        }
                        break;
                    case "Keybind":
                        if (Keybind_CheckInput)
                        {
                            if (Input.anyKeyDown)
                            {
                                foreach(KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                                {
                                    if (Input.GetKeyDown(key))
                                    {
                                        properties props = selected.GetComponent<properties>();
                                        if (props.PartType == "Wheel")
                                        selected.GetComponent<properties>().Control_Keybinds[0] = key;
                                        Keybind_CheckInput = false;
                                        Debug.Log("Keybind set: " + key);
                                    }
                                }
                            }
                        }
                        break;
                }
                break;
            
            case "Combat":
                // allows player control of the vehicle(assembly) in combat
                // assembly.rb.AddForce(new Vector3(
                //     Input.GetAxis("Horizontal") * 5, 
                //     Input.GetAxis("Vertical") * 10, 
                //     0));

                // Control code
                foreach (GameObject Part in assembly.PartList)
                {
                    properties props = Part.GetComponent<properties>();
                    if (props.Category == "Control")
                    {
                        switch (props.PartType)
                        {
                            case "Wheel":
                                if (props.HingeRef != null)
                                {
                                    JointMotor2D m = props.HingeRef.motor;
                                    if (Input.GetKeyDown(props.Control_Keybinds[0]))
                                    {
                                        m.motorSpeed = -500f;
                                    }
                                    else
                                    {
                                        m.motorSpeed = -0f;
                                    }
                                    Part.GetComponent<HingeJoint2D>().motor = m;
                                }
                                break;
                        }
                    }
                }
                

                if (CameraSyncToVehicle)
                {
                    mainCamera.transform.position = new Vector3(assembly.root.transform.position.x, assembly.root.transform.position.y, -10);
                }
                break;
        }
    }
}
