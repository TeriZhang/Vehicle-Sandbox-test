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

public class Assembly 
{
    
    public List<GameObject> PartList;
    

    public GameObject root;
    public Rigidbody2D rb;

    public Assembly()
    {
        PartList = new List<GameObject>();
    }

    public void Add(GameObject PartP)
    {
        PartList.Add(PartP);
    }
    // GameObject createRoot()
    // {
    //     GameObject temp = Instantiate(Resources.Load<GameObject>("Parts/Engine"), transform);
    //     temp.name = "Root";
    //     temp.transform.position = new Vector2(0, 2);
    //     temp.transform.parent = World.transform;

    //     Freeze(temp, true);

    //     assembly.AddRoot(temp);
    //     return temp;
    // }
    public void AddRoot(GameObject rootP)
    {
        // GameObject enginePrefab = Resources.Load<GameObject>("Parts/Engine");
        // root = GameObject.Instantiate(enginePrefab);

        // root.name = "Root";
        // root.transform.position = new Vector2(0, 2);
        // root.transform.parent = World.transform;

        // Freeze(root, true);

        rb = rootP.GetComponent<Rigidbody2D>();
        Add(rootP);
    }
    public void Remove(GameObject PartP)
    {
        PartList.Remove(PartP);
    }
}
