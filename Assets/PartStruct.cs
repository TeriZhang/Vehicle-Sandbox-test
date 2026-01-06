using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Animations;
using Unity.Mathematics;

public class PartStruct
{
    public PartStruct()
    {
        
    }

    public PartStruct(PartType partTypeP, GameObject closestPartP, AttachPoint closestAttachPointP)
    {
        partType = partTypeP;
        closestPart = closestPartP;
        closestAttachPoint = closestAttachPointP;
    }
    public GameObject closestPart;
    public AttachPoint closestAttachPoint;


    public string hierPath;
    public int occupiedIndex;
    public PartType partType;

    public float editRotation;

    public PartStruct parent;

    public List<PartStruct> Attached = new List<PartStruct>();
    public List<Keybind> Control_Keybinds = new List<Keybind>();
}
