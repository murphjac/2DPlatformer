﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Controller2D), typeof(MeshRenderer))]
public class Player : MonoBehaviour {

    [Header("Archetype Properties")]
    public MovementProperties momentumProperties;
    public MovementProperties inertiaProperties;

    private MovementProperties currentProperties;
    private float timeToWallUnstick;
    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private Vector3 velocity;
    private Controller2D controller;
    private MeshRenderer meshRenderer;
    private float velocityXSmoothing;
    private Vector2 directionalInput;
    private int wallDirX;
    private int numJumpsTaken;
    private int numAirDashesTaken;
    private bool wallSliding;
    private bool dashing;
    private bool dashStart = false;
    private bool isInertia = false;

    public bool PlayerOnGround() { return controller.collisions.below; }
    public void SetDirectionalInput(Vector2 input) { directionalInput = input; }

    void Start()
    {
        controller = GetComponent<Controller2D>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (isInertia) { UpdatePlayerProperties(inertiaProperties); }
        else { UpdatePlayerProperties(momentumProperties); }
    }

    void Update()
    {
        CalculateVelocity();
        if (currentProperties.canWallslide) { HandleWallSliding(); }

        controller.Move(velocity * Time.deltaTime, directionalInput);

        if (controller.collisions.above || PlayerOnGround())
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            }
            else { velocity.y = 0; }
        }

        // Reset the number of jumps and airdashes remaining upon gaining traction on a surface.
        if (PlayerOnGround() || wallSliding)
        {
            numJumpsTaken = 0;
            numAirDashesTaken = 0;
        }
    }

    void UpdatePlayerProperties(MovementProperties mp)
    {
        currentProperties = mp;

        gravity = -(2 * currentProperties.maxJumpHeight) / Mathf.Pow(currentProperties.timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * currentProperties.timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * currentProperties.minJumpHeight);

        currentProperties.moveSpeed = dashing ? currentProperties.dashSpeed : currentProperties.runSpeed;
    }

    public void Transform()
    {
        if (isInertia)
        {
            isInertia = false;
            meshRenderer.sharedMaterial = Resources.Load("Materials/Player_Momentum") as Material;
            UpdatePlayerProperties(momentumProperties);
        }
        else
        {
            isInertia = true;
            meshRenderer.sharedMaterial = Resources.Load("Materials/Player_Inertia") as Material;
            UpdatePlayerProperties(inertiaProperties);
        }
    }

    public void OnJumpInputDown()
    {
        // Cancel any dashes upon jumping during a dash.
        if (dashing) { DashCancel(); }

        if (wallSliding)
        {
            // Wall jump velocity is dependent upon input direction relative to the wall.
            if (wallDirX == directionalInput.x){ velocity = currentProperties.wallJumpClimb; }
            else if (directionalInput.x == 0){ velocity = currentProperties.wallJumpOff; }
            else{ velocity = currentProperties.wallLeap; }

            // Ensure the jump is horizontally away from the wall.
            velocity.x *= -wallDirX;
        }

        if (PlayerOnGround() || numJumpsTaken < currentProperties.numJumps)
        {
            if (controller.collisions.readyToFallThroughPlatform)
            {
                controller.collisions.readyToFallThroughPlatform = false;
                controller.collisions.fallingThroughPlatform = true;
                controller.Invoke("ResetFallingThroughPlatform", 0.5f);
            }
            else
            {
                numJumpsTaken++;

                if (controller.collisions.slidingDownMaxSlope)
                {
                    if(directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                    {
                        // not jumping against max slope
                        velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                        velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                    }
                }
                else
                {
                    velocity.y = maxJumpVelocity;
                }
            }
        }
    }

    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity) { velocity.y = minJumpVelocity; }
    }

    public void OnDashInputDown()
    {
        if( (currentProperties.canDash && PlayerOnGround()) || (!PlayerOnGround() && numAirDashesTaken < currentProperties.numAirDashes) )
        {
            if (!dashing)
            {
                // Start a new dash.
                Dash();
            }
            else
            {
                // Cancel any invoked dash methods.
                CancelInvoke("Dash");
                CancelInvoke("DashCancel");

                // Cancel the current dash.
                DashCancel();

                // Start up a new dash.
                Invoke("Dash", currentProperties.dashFrameTime);
            }

            // Plan on ending this dash.
            Invoke("DashCancel", currentProperties.dashTime);
        }
    }

    // Begin a new dash.
    void Dash()
    {
        if (!dashing)
        {
            dashStart = true;
            dashing = true;
            currentProperties.moveSpeed = currentProperties.dashSpeed;

            // Take an airdash if not on the ground.
            if (!PlayerOnGround()) { numAirDashesTaken++; }
        }
    }

    // End a current dash.
    void DashCancel()
    {
        if (dashing)
        {
            dashing = false;
            currentProperties.moveSpeed = currentProperties.runSpeed;
        }
    }

    void HandleWallSliding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;

        if ((controller.collisions.left || controller.collisions.right) && (!PlayerOnGround()) && velocity.y < 0)
        {
            wallSliding = true;

            if (velocity.y < -currentProperties.wallSlideSpeedMax) { velocity.y = -currentProperties.wallSlideSpeedMax; }

            if (timeToWallUnstick > 0)
            {
                velocity.x = 0;
                velocityXSmoothing = 0;

                if (directionalInput.x != wallDirX && directionalInput.x != 0) { timeToWallUnstick -= Time.deltaTime; }
                else { timeToWallUnstick = currentProperties.wallStickTime; }
            }
            else
            {
                timeToWallUnstick = currentProperties.wallStickTime;
            }
        }
    }

    void CalculateVelocity()
    {
        if (!dashing) { velocity.y += gravity * Time.deltaTime; }

        if (dashStart)
        {
            dashStart = false;
            velocity.y = 0;

            if (Mathf.Abs(velocity.x) < currentProperties.dashSpeed || Mathf.Sign(velocity.x) != Mathf.Sign(directionalInput.x))
            {
                velocity.x = directionalInput.x * currentProperties.dashSpeed;
                return;
            }
        }

        float accelerationTime = PlayerOnGround() ? currentProperties.gndAccelTime : currentProperties.airAccelTime;
        float targetVelocityX = directionalInput.x * currentProperties.moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTime);
    }

    // Debug stuff
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            float gizmoSize = 0.4f;
            Vector3 cubePos = transform.position + new Vector3(gizmoSize, 1.0f, 0f);
            Vector3 spherePos = transform.position + new Vector3(0.0f, 1.0f, 0.0f);

            // Blue cube above player if dash, green if not.
            Gizmos.color = dashing ? new Color(0, 1, 0, 1.0f) : new Color(0, 0, 1, 1.0f);
            Gizmos.DrawCube(cubePos, new Vector3(gizmoSize, gizmoSize, gizmoSize));

            // White sphere above player if on ground, black if not.
            Gizmos.color = PlayerOnGround() ? new Color(1, 1, 1, 1.0f) : new Color(0, 0, 0, 1.0f);
            Gizmos.DrawSphere(spherePos, gizmoSize/2);
        }
    }

    [System.Serializable]
    public struct MovementProperties
    {
        [HideInInspector]
        public float moveSpeed;

        [Header("Movement")]
        public float runSpeed;
        public float airAccelTime;
        public float gndAccelTime;

        [Header("Dashing")]
        public bool canDash;
        public int numAirDashes;
        public float dashTime;
        public float dashSpeed;
        public float dashFrameTime;

        [Header("Jumping")]
        public int numJumps;
        public float maxJumpHeight;
        public float minJumpHeight;
        public float timeToJumpApex;

        public Vector2 wallJumpClimb;
        public Vector2 wallJumpOff;
        public Vector2 wallLeap;

        [Header("Wall Sliding")]
        public bool canWallslide;
        public float wallSlideSpeedMax;
        public float wallStickTime;
    }
}
