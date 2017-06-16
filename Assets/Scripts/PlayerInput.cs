﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerInput : MonoBehaviour {

    public KeyCode Jump, Dash;

    private Player player;

    void Start () {
        player = GetComponent<Player>();
    }

    // Update is called once per frame
    void Update () {
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        player.SetDirectionalInput(directionalInput);

        if (Input.GetKeyDown(Jump)) { player.OnJumpInputDown(); }
        if (Input.GetKeyUp(Jump)) { player.OnJumpInputUp(); }
        if (Input.GetKeyDown(Dash)) { player.OnDashInputDown(); }
    }

}