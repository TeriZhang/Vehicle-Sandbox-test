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
using System.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using Unity.Hierarchy;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.UIElements;

using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor.MPE;
using TMPro;
using UnityEngine.EventSystems;

public class Global : MonoBehaviour
{
    public GameObject World;
    public Camera mainCamera;
    public string gameState = "Building"; // Building Combat

    public delegate void userActionStateChange();
    public static event userActionStateChange onUserActionStateChanged;

    public Assembly assembly;
    
    public AssemblyStruct savedAssembly;
    AttachPoint closestAttachPoint;
    GameObject closestPart;


    public UserAction userAction = UserAction.Idle;

    public PartType SelectedPart;

    public GameObject ghostPart;

    public GameObject BuildingUI;

    public GameObject UIsleep;

    public bool CameraSyncToVehicle;

    public List<PartType> Inventory; // "What the player can build with" represented as a list of part names.

    public bool Keybind_CheckInput = false;

    public GameObject selected;

    public GameObject CombatButton;

    public Sprite CombatGo_Sprite;
    public Sprite CombatReturn_Sprite;
    
    public GameObject SelectionOutline;
    public GameObject PartKeybinds;

    public int KeyIndex;

    public TextMeshProUGUI GbindKey;

    public GameObject background;


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
    GameObject createRoot(Assembly thisAssembly)
    {
        GameObject temp = Instantiate(Resources.Load<GameObject>("Parts/Engine"), transform);
        temp.name = "Root";
        temp.transform.position = new Vector2(0, 3);
        temp.transform.parent = World.transform;

        properties props = temp.GetComponent<properties>();
        Freeze(temp, true);

        thisAssembly.AddRoot(temp);
        props.AssemblyRoot = temp;

        return temp;
    }

    public GameObject findwithPath(Assembly thisAssembly, string path)
    {
        GameObject selector = thisAssembly.root;
        foreach(char pointer in path)
        {
            int index = (int) Char.GetNumericValue(pointer);

            List<AttachPoint> liss = selector.GetComponent<properties>().AttachPoints;
            AttachPoint ff = liss[index];

            selector = liss[index].occupied;
        }
        return selector;
    }

    public Quaternion calculateRotation(properties props)
    {
        float extra = props.partOffsetRotation;
        AttachPoint attachPoint = props.ParentAttachpoint;
        if (props.doesAutoRotate)
        {
            extra += attachPoint.autoRotation;
        }
        return Quaternion.Euler(new Vector3(0, 0, 
        attachPoint.rotation + props.ParentAttachpoint.Parent.transform.rotation.eulerAngles.z + props.editRotation + extra));
    }

    public GameObject newPart(Assembly thisAssembly, PartStruct partStruct)
    {
        string name = partStruct.partType.ToString();

        GameObject weldParent;
        AttachPoint attachPoint;
        properties parentProps;

        if (partStruct.closestPart != null)
        {
            weldParent = partStruct.closestPart;
            parentProps = weldParent.GetComponent<properties>(); 
            attachPoint = partStruct.closestAttachPoint; 
        }
        else
        {
            string temp = partStruct.hierPath.Substring(0, partStruct.hierPath.Length - 1);
            weldParent = findwithPath(thisAssembly, temp);
            parentProps = weldParent.GetComponent<properties>(); 
            attachPoint = parentProps.AttachPoints[partStruct.occupiedIndex];
        }

        GameObject createdPart = Instantiate(Resources.Load<GameObject>("Parts/" + name), transform);
        properties props = createdPart.GetComponent<properties>();
        props.editRotation = partStruct.editRotation;
        props.ParentAttachpoint = attachPoint;
        if (thisAssembly.root)
        {
            props.AssemblyRoot = thisAssembly.root;
        }
        if (partStruct.closestPart != null)
        {
            props.hierPath = parentProps.hierPath + "" + props.ParentAttachpoint.Parent.GetComponent<properties>().AttachPoints.IndexOf(props.ParentAttachpoint);
        }
        else
        {
            props.Control_Keybinds = partStruct.Control_Keybinds;
            props.hierPath = parentProps.hierPath + "" + partStruct.occupiedIndex;
        }

        createdPart.transform.rotation = calculateRotation(props);

        createdPart.name = name + " " + thisAssembly.PartList.Count;
        createdPart.transform.position = weldParent.transform.position + (weldParent.transform.rotation * attachPoint.offset);

        createdPart.transform.parent = weldParent.transform;
        attachPoint.occupied = createdPart;

        switch (props.partType)
        { 
            case PartType.Wheel:
                //first destroy the previous Hinge Joint.
                changeWheelHinge(createdPart, thisAssembly);
                break;
        }
        Destroy(createdPart.GetComponent<Rigidbody2D>());

        // Rigidbody2D rb = temp.GetComponent<Rigidbody2D>();
        // rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        thisAssembly.Add(createdPart);

        return createdPart;
    }
    void newEmptyVehicle()
    {
        assembly = new Assembly();
        assembly.root = createRoot(assembly);

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

    public void setUserAction(UserAction action)
    {
        userAction = action;
        onUserActionStateChanged();
    }

    public GameObject getPhysical(GameObject PartP) // for HugeWheel bug. decided not to use for now.
    {
        return PartP.transform.Find("Physical").gameObject;
    }
    int hierarchyiterate_iterations = 0;
    public void hierarchyIteration(GameObject Part, List<GameObject> list)
    {
        properties props = Part.GetComponent<properties>();
        hierarchyiterate_iterations++;
        list.Add(Part);
        foreach (AttachPoint attachPoint in props.AttachPoints)
        {
            if (hierarchyiterate_iterations > assembly.PartList.Count + 10) {break;}

            if (attachPoint.occupied)
            {
                hierarchyIteration(attachPoint.occupied, list);
            }
        }
    }

    public PartStruct makePartStruct(GameObject Part)
    {
        PartStruct partStruct = new PartStruct();
        properties props = Part.GetComponent<properties>();

        partStruct.hierPath = props.hierPath;
        if (props.ParentAttachpoint != null && props.ParentAttachpoint.Parent != null)
        {
            partStruct.occupiedIndex = props.ParentAttachpoint.Parent.GetComponent<properties>().AttachPoints.IndexOf(props.ParentAttachpoint);
        }
        partStruct.partType = props.partType;
        partStruct.editRotation = props.editRotation;

        partStruct.Control_Keybinds = props.Control_Keybinds;

        foreach (AttachPoint attachPoint in props.AttachPoints)
        {
            if (attachPoint.occupied != null)
            {
                partStruct.Attached.Add(makePartStruct(attachPoint.occupied));
            }
            else
            {
                partStruct.Attached.Add(null);
            }
        }

        return partStruct;
    }

    public AssemblyStruct createAssemblyStruct(Assembly thisAssembly)
    {
        AssemblyStruct assemblyStruct = new AssemblyStruct();
        assemblyStruct.Root = makePartStruct(thisAssembly.root);

        return assemblyStruct;
    }

    public void hIteration(Assembly thisAssembly, PartStruct partStruct)
    {
        newPart(thisAssembly, partStruct);
        foreach(PartStruct thisPartStruct in partStruct.Attached)
        {
            if (thisPartStruct != null)
            {
                hIteration(thisAssembly, thisPartStruct);
            }
        }
    }

    public Assembly createVehicleFromStruct(AssemblyStruct assemblyStruct)
    {
        Assembly newAssembly = new Assembly();
        newAssembly.root = createRoot(newAssembly);
        foreach(PartStruct thisPartStruct in assemblyStruct.Root.Attached)
        {
            if (thisPartStruct != null)
            {
                hIteration(newAssembly, thisPartStruct);
            }
        }
        return newAssembly;
    }

    public List<GameObject> listedHierarchy(GameObject Part)
    {
        List<GameObject> list = new List<GameObject>();
        hierarchyiterate_iterations = 0;
        hierarchyIteration(Part, list);

        return list;
    }

    public void removeBody(GameObject PartP, bool destroy)
    {
        List<GameObject> list = listedHierarchy(PartP);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            removePart(list[i], destroy);
        }
    }

    public void removePart(GameObject Part, bool destroy)
    {
        properties props = Part.GetComponent<properties>();
        assembly.Remove(Part);
        props.ParentAttachpoint.occupied = null;
        if (destroy)
        {
            switch (props.partType)
            { 
                case PartType.Wheel:
                    Destroy(props.HingeRef);
                    if (Part.GetComponent<HingeJoint2D>())
                    {
                        Destroy(Part.GetComponent<HingeJoint2D>());
                    }
                    break;
            }
            Destroy(Part);
        }
    }

    void changeWheelHinge(GameObject PartP, Assembly assemblyP)
    {
        properties PartPprops = PartP.GetComponent<properties>();

        if (PartPprops.HingeRef != null)
        {
            PartPprops.HingeRef.enabled = false;
            Destroy(PartPprops.HingeRef);
        }
        if (PartP.GetComponent<HingeJoint2D>())
        {
            PartP.GetComponent<HingeJoint2D>().enabled = false;
            Destroy(PartP.GetComponent<HingeJoint2D>());
        }

        //Remove Part's Rigidbody.
        if (PartP.GetComponent<Rigidbody2D>() != null && assemblyP.root != PartP)
        {
            Destroy(PartP.GetComponent<Rigidbody2D>());
        }
        


        //Create new
        HingeJoint2D HingeJoint = assemblyP.root.AddComponent<HingeJoint2D>();
        HingeJoint.useMotor = true;
        JointMotor2D motor = HingeJoint.motor;
        motor.motorSpeed = 0;
        motor.maxMotorTorque = 3000;
        HingeJoint.motor = motor;
        HingeJoint.connectedBody = PartP.transform.Find("Tire").gameObject.GetComponent<Rigidbody2D>();

        HingeJoint.anchor = PartP.transform.position - assemblyP.root.transform.position;

        PartPprops.HingeRef = HingeJoint;
    }

    void DamagePart(Collision2D collision, properties attackProps)
    {
        GameObject gObject;
        if (collision.collider.gameObject.name == "Tire") // hardcode cuz refactor later
        {
            gObject = collision.collider.gameObject.transform.parent.gameObject;
        }
        else
        {
            gObject = collision.collider.gameObject;
        }
        properties props = gObject.GetComponent<properties>();
        if (props)
        {
            // damaging objects
            Debug.Log(attackProps.Damage);
            Debug.Log(props.Health);
            props.Health -= attackProps.Damage;
            if (props.Health <= 0) // destroy gObject
            {
                //detached objects
                gObject.transform.parent = World.transform;
                gObject.AddComponent<Rigidbody2D>();

                foreach (AttachPoint attachPoint in props.AttachPoints)
                {
                    GameObject Part = attachPoint.occupied;
                    if (Part != null)
                    {
                        removeBody(Part, false);
                        Part.transform.parent = World.transform;
                        attachPoint.occupied = null;

                        //make this part and its children a seperate assembly
                        
                        Assembly newAssembly = new Assembly();
                        newAssembly.PartList = listedHierarchy(Part);
                        newAssembly.root = Part;
                        Part.GetComponent<properties>().AssemblyRoot = newAssembly.root;
                        Part.AddComponent<Rigidbody2D>();

                        //fix the wheels n stuff
                        foreach (GameObject thisPart in newAssembly.PartList)
                        {
                            properties thisPartProps = thisPart.GetComponent<properties>();
                            thisPartProps.AssemblyRoot = newAssembly.root;
                            switch (thisPartProps.partType)
                            { 
                                case PartType.Wheel:
                                    changeWheelHinge(thisPart, newAssembly);
                                    break;
                            }
                        }
                        Debug.Log("finish");
                    }
                }
                removeBody(gObject, false);
                removePart(gObject, true);
            }
        }
    }

    // Block functions
    async void spikeTouch(Collision2D collision, GameObject Part)
    {
        properties propsB;
        if (collision.collider.gameObject.name == "Tire") // hardcode cuz refactor later
        {
            propsB = collision.collider.gameObject.transform.parent.GetComponent<properties>();
        }
        else
        {
            propsB = collision.collider.GetComponent<properties>();
        }
        properties props = Part.GetComponent<properties>();
        if (propsB != null && propsB.AssemblyRoot != props.AssemblyRoot)
        {
            DamagePart(collision, Part.GetComponent<properties>());

            Rigidbody2D rb = propsB.AssemblyRoot.GetComponent<Rigidbody2D>();

            rb.WakeUp();
            rb.AddForce(new Vector2(
                    math.sin(collision.otherCollider.transform.rotation.eulerAngles.z) * 600, 
                    math.cos(collision.otherCollider.transform.rotation.eulerAngles.z) * 600));

            Part.transform.Find("Blade").transform.localPosition = new Vector2(0.8f, 0f);
            await Task.Delay(1000);
            if (Part.transform.Find("Blade") != null)
            {
                Part.transform.Find("Blade").transform.localPosition = new Vector2(0.35f, 0f);
            }
        }
    }

    
    // used with the buttons in the inventory.
    public void SelectPart(PartType partType)
    {
        if (gameState == "Building")
        {
            if (ghostPart)
            {
                Destroy(ghostPart);
            }
            setUserAction(UserAction.Build);

            SelectedPart = partType;
            ghostPart = Instantiate(Resources.Load<GameObject>("Parts/" + partType.ToString()));
            ghostPart.transform.parent = World.transform;

            SpriteRenderer sr = ghostPart.GetComponent<SpriteRenderer>();
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);

            Rigidbody2D rb = ghostPart.GetComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            ghostPart.GetComponent<Collider2D>().enabled = false;
            ghostPart.GetComponent<Collider2D>().enabled = false;
        }
    }
    public void MoveButtonPress()
    {
        if (gameState == "Building")
        {
            setUserAction(UserAction.Move);
        }
    }
    public void RemoveButtonPress()
    {
        if (gameState == "Building")
        {
            setUserAction(UserAction.Remove);
        }
    }
    public void CombatButtonPress()
    {
        Destroy(PartKeybinds);
        SelectionOutline.transform.SetParent(UIsleep.transform);
        setUserAction(UserAction.Idle);

        switch (gameState)
        {
            case "Building":
                gameState = "Combat";

                savedAssembly = createAssemblyStruct(assembly);

                Freeze(assembly.root, false);
                assembly.rb.AddForce(new Vector2(0, -100));
                CameraSyncToVehicle = true;

                // apply spike stuff  0.35 0.8

                foreach (GameObject Part in assembly.PartList)
                {
                    properties props = Part.GetComponent<properties>();
                    switch (props.partType)
                    {
                        case PartType.Spike:
                            props.onCollisionEnterEvent.AddListener((collision) => spikeTouch(collision, Part));
                            break;
                    }
                }

                CombatButton.GetComponent<Image>().sprite = CombatReturn_Sprite;
                BuildingUI.transform.Find("CornerBack").GetComponent<RectTransform>().DOMoveX(-50, 0.5f, true);
                BuildingUI.transform.Find("Inventory").GetComponent<RectTransform>().DOMoveY(-80, 0.5f, true);
                break;
            case "Combat":
                gameState = "Building";

                Destroy(assembly.root);

                assembly = createVehicleFromStruct(savedAssembly);

                assembly.root.transform.position = new Vector3(0, 3, 0);
                assembly.root.transform.rotation = new Quaternion(0, 0, 0, 1);
                CameraSyncToVehicle = false;
                Freeze(assembly.root, true);
                mainCamera.transform.position = new Vector3(0, 1, -10);

                CombatButton.GetComponent<Image>().sprite = CombatGo_Sprite;
                BuildingUI.transform.Find("CornerBack").GetComponent<RectTransform>().DOMoveX(-10, 0.5f, true);
                BuildingUI.transform.Find("Inventory").GetComponent<RectTransform>().DOMoveY(80, 0.5f, true);

                break;

        }
    }
    public void KeybindButtonPress()
    {
        if (gameState == "Building")
        {
            setUserAction(UserAction.Keybind);
        }
    }
    public void RotateButtonPress()
    {
        if (gameState == "Building")
        {
            setUserAction(UserAction.Rotate);
        }
    }
    public void OffButtonPress()
    {
        if (gameState == "Building")
        {
            setUserAction(UserAction.Idle);
        }
    }

    public GameObject ClearPrompt;

    public void ClearButtonPress()
    {
        if (gameState == "Building")
        {
            setUserAction(UserAction.Clear);
            ClearPrompt.transform.SetParent(BuildingUI.transform);
        }
    }

    public void YesClearButtonPress()
    {
        if (gameState == "Building")
        {
            ClearPrompt.transform.SetParent(UIsleep.transform);
            removeBody(assembly.root, true);
            newEmptyVehicle();
        }
    }
    public void NoClearButtonPress()
    {
        if (gameState == "Building")
        {
            ClearPrompt.transform.SetParent(UIsleep.transform);
        }
    }

    public GameObject keybindSelected;

    public keybindData GbindData;
    public Keybind GKeybind;

    //currently used to add parts, remove parts, and adjust parts.
    public void onLeftMouseDown(InputAction.CallbackContext ctx)
    {
        // Debug.Log("place_hit");
        if (ctx.performed &&  gameState == "Building" && !EventSystem.current.IsPointerOverGameObject()) // click shouldn't register on UI
        {
            
            properties props;
            selected = nearestPart();
            if (selected == null && closestPart == null && userAction != UserAction.Keybind)
            {
                setUserAction(UserAction.Idle);
                return;
            }
            switch (userAction)
            {
                case UserAction.Build:
                    if (closestPart)
                    {
                        // Debug.Log("place_activate");
                        closestAttachPoint.occupied = newPart(assembly, new PartStruct(SelectedPart, closestPart, closestAttachPoint));
                    }
                    break;
                case UserAction.Remove:
                    if (selected && selected != assembly.root)
                    {
                        props = selected.GetComponent<properties>();
                        props.ParentAttachpoint.occupied = null;

                        removeBody(selected, true);
                    }
                    break;
                case UserAction.Keybind:
                    if (selected)
                    {
                        props = selected.GetComponent<properties>();
                        if (props.Category == "Control")
                        {
                            keybindSelected = selected;

                            //Keybind UI
                            if (PartKeybinds != null)
                            {
                                Destroy(PartKeybinds);
                            }
                            PartKeybinds = Instantiate<GameObject>(Resources.Load<GameObject>("UI/PartKeybinds"));
                            for (int i = 0; i < props.Control_Keybinds.Count; i++)
                            {
                                int index = i;
                                GameObject newKeybindBox = Instantiate<GameObject>(Resources.Load<GameObject>("UI/KeybindBox"));
                                newKeybindBox.transform.SetParent(PartKeybinds.transform);
                                TextMeshProUGUI bindKey = newKeybindBox.transform.Find("Key").Find("Text").GetComponent<TextMeshProUGUI>();
                                TextMeshProUGUI bindName = newKeybindBox.transform.Find("ControlName").Find("Text").GetComponent<TextMeshProUGUI>();

                                keybindData bindData = newKeybindBox.GetComponent<keybindData>();
                                bindData.keybind = props.Control_Keybinds[i];

                                newKeybindBox.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                                {
                                    Debug.Log("clicked");
                                    bindKey.text = "...";
                                    GbindKey = bindKey;
                                    Keybind_CheckInput = true;
                                    GKeybind = props.Control_Keybinds[index];
                                    GbindData = bindData;
                                });
                                bindKey.text = bindData.keybind.key.ToString();
                                bindName.text = bindData.keybind.name;
                            }
                            PartKeybinds.transform.SetParent(BuildingUI.transform);
                            PartKeybinds.GetComponent<RectTransform>().position = Camera.main.WorldToScreenPoint(keybindSelected.transform.position) - new Vector3(0, 40, 0);
                        }
                    }
                    break;
                case UserAction.Rotate:
                    if (selected)
                    {
                        properties selectedProps = selected.GetComponent<properties>();
                        selectedProps.editRotation += 90;

                        selected.transform.rotation = calculateRotation(selectedProps);
                    }
                    break;
            }
        }
    }

    public void onLeftMouseUp(InputAction.CallbackContext ctx)
    {
        
    }

    public GameObject PrimaryTools;

    void Start()
    {
        CombatGo_Sprite = Resources.Load<Sprite>("Graphics/Combat_Go");
        CombatReturn_Sprite = Resources.Load<Sprite>("Graphics/Combat_Return");

        newEmptyVehicle();

        Inventory = new List<PartType>
        {
            PartType.Block,
            PartType.AngledBlock,
            PartType.Wheel,
            PartType.Spike,
            PartType.HugeWheel,
        };

        //Creates the buttons in the inventory UI.
        for (int i = 0; i < Inventory.Count; i++)
        {
            PartType partType = Inventory[i];
            GameObject Button = Instantiate(Resources.Load<GameObject>("UI/PartButton"));

            Button.transform.SetParent(BuildingUI.transform.Find("Inventory").transform);
            Button.GetComponent<RectTransform>().anchoredPosition = new Vector2(-260 + 60 * i, 40);
            Button.GetComponent<Image>().sprite = Resources.Load<Sprite>("Graphics/" + partType.ToString());

            Button.GetComponent<Button>().onClick.AddListener(() => SelectPart(partType));
        }

        //bind
        onUserActionStateChanged += () => {
            keybindSelected = null;
            if (userAction != UserAction.Build)
            {
                Destroy(ghostPart);
            }
            if (userAction != UserAction.Keybind)
            {
                Destroy(PartKeybinds);
            }

            Transform buttonTransform = PrimaryTools.transform.Find(userAction.ToString() + "Button");
            if (buttonTransform)
            {
                SelectionOutline.transform.SetParent(buttonTransform.gameObject.transform);
                SelectionOutline.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                SelectionOutline.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            }
            else
            {
                SelectionOutline.transform.SetParent(UIsleep.transform);
            }
        };
    }

    GameObject highlight;
    GameObject oldhighlight;

    void Update()
    {
        switch (gameState)
        {
            case "Building":
                if (keybindSelected)
                {
                    highlight = keybindSelected;
                }
                else
                {
                    highlight = nearestPart();
                }
                if (oldhighlight)
                {
                    SpriteRenderer srr = oldhighlight.GetComponent<SpriteRenderer>();
                    float hh, ss, vv;
                    Color.RGBToHSV(srr.color, out hh, out ss, out vv);
                    srr.color = Color.HSVToRGB(hh, ss, 1f);
                    oldhighlight = highlight;
                }
                if (highlight && userAction != UserAction.Build)
                {
                    SpriteRenderer sr = highlight.GetComponent<SpriteRenderer>();
                    float h, s, v;
                    Color.RGBToHSV(sr.color, out h, out s, out v);
                    sr.color = Color.HSVToRGB(h, s, 1.25f);
                    oldhighlight = highlight;
                }

                switch (userAction)
                {
                    case UserAction.Idle:
                        break;
                    case UserAction.Build:
                        // visual for when the player drags ghost parts onto the assembly
                        getSnap();
                        Vector2 mousePos = Mouse.current.position.ReadValue();
                        if (ghostPart)
                        {
                            if (mousePos.x > 70 && mousePos.y > 160)
                            {
                                if (closestPart)
                                {
                                    // snapping
                                    properties props = ghostPart.GetComponent<properties>();
                                    props.ParentAttachpoint = closestAttachPoint;
                                    ghostPart.transform.position = closestPart.transform.position + (closestPart.transform.rotation * closestAttachPoint.offset);
                                    ghostPart.transform.rotation = calculateRotation(props);
                                }
                                else
                                {
                                    // dragging
                                    ghostPart.transform.rotation = new Quaternion(0, 0, 0, 1);
                                    ghostPart.transform.position = getMousePos();
                                }
                            }
                            else
                            {
                                ghostPart.transform.position = new Vector3(0, 0, 10000);
                            }
                        }
                        break;
                    case UserAction.Keybind:
                        if (Keybind_CheckInput)
                        {
                            Debug.Log("checking");
                            if (Input.anyKeyDown)
                            {
                                Debug.Log("set attempted");
                                foreach(KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                                {
                                    if (key == KeyCode.Mouse0)
                                    {
                                        Keybind_CheckInput = false;
                                        GbindKey.text = GbindData.keybind.key.ToString();
                                    }
                                    else if (Input.GetKeyDown(key))
                                    {
                                        properties props = keybindSelected.GetComponent<properties>();
                                        GKeybind.key = key;
                                        GbindKey.text = key.ToString();

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
                        switch (props.partType)
                        {
                            case PartType.Wheel:
                                if (props.HingeRef != null)
                                {
                                    JointMotor2D m = props.HingeRef.motor;
                                    if (Input.GetKey(props.Control_Keybinds[0].key))
                                    {
                                        m.motorSpeed = 1000f;
                                    }
                                    else if(Input.GetKey(props.Control_Keybinds[1].key))
                                    {
                                        m.motorSpeed = -1000f;
                                    }
                                    else
                                    {
                                        m.motorSpeed = -0f;
                                    }
                                    props.HingeRef.motor = m;
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
        Vector3 temp = mainCamera.transform.position / 4;
        temp.z = 1;
        background.transform.position = temp;
    }
}
