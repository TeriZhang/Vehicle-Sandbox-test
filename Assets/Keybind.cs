using UnityEngine;

[System.Serializable]
public class Keybind
{
    public Keybind(string nameP, KeyCode keyP)
    {
        name = nameP;
        key = keyP;
    }
    public string name;
    public KeyCode key;
}
