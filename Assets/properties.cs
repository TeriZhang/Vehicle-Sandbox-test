using System.Collections.Generic;
using UnityEngine;

public class properties : MonoBehaviour
{   
    public string Category;
    public List<AttachPoint> AttachPoints = new List<AttachPoint>();
    public List<KeyCode> Control_Keybinds = new List<KeyCode>();
    public int Damage;
    public int Speed;
}
