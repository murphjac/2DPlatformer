using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TestUseBox : UsableObject {

    private Renderer rend;
    private bool useState = false;

    // Use this for initialization
    void Start () {
        rend = GetComponent<Renderer>();
    }

    public override void Use()
    {
        string materialFile = useState ? "Materials/Player_Momentum" : "Materials/Player_Inertia";
        rend.sharedMaterial = Resources.Load(materialFile) as Material;
        useState = !useState;
    }
}
