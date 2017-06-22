using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerInput : MonoBehaviour {

    public KeyCode Jump, Dash, TransformEarth, TransformWind, TransformFire, TransformWater;

    private Player player;

    void Start () {
        player = GetComponent<Player>();
    }

    void Update () {
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        player.SetDirectionalInput(directionalInput);

        if (Input.GetKeyDown(Jump)) { player.OnJumpInputDown(); }
        if (Input.GetKeyUp(Jump)) { player.OnJumpInputUp(); }
        if (Input.GetKeyDown(Dash)) { player.OnDashInputDown(); }
        if (Input.GetKeyDown(TransformEarth)) { player.Transform(Player.EARTH); }
        if (Input.GetKeyDown(TransformWind)) { player.Transform(Player.WIND); }
        if (Input.GetKeyDown(TransformFire)) { player.Transform(Player.FIRE); }
        if (Input.GetKeyDown(TransformWater)) { player.Transform(Player.WATER); }
    }

}
