using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TestUseBox : MonoBehaviour, UsableObject {

    private Renderer rend;
    private bool useState = false;

    void Start () {
        rend = GetComponent<Renderer>();
    }

    public void Use()
    {
        string materialFile = useState ? "Materials/Player_Momentum" : "Materials/Player_Inertia";
        rend.sharedMaterial = Resources.Load(materialFile) as Material;
        useState = !useState;
    }
}
