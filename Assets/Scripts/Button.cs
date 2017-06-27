using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Button : MonoBehaviour, UsableObject {

    public PlatformController targetPlatform;

    private Renderer rend;
    
    void Start () {
        rend = GetComponent<Renderer>();
        SetButtonColor();
    }

    void SetButtonColor()
    {
        string materialPath = targetPlatform.active ? "Materials/Player_Wind" : "Materials/Player_Fire";
        rend.sharedMaterial = Resources.Load(materialPath) as Material;
    }

    public void Use()
    {
        targetPlatform.active = !targetPlatform.active;
        SetButtonColor();
    }
}
