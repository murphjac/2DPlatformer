using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Controller2D))]
public class Player : MonoBehaviour {

    [Header("Movement")]
    public float moveSpeed = 6;

    public float dashTime = 0.2f;
    public float dashSpeed = 3;
    public float dashFrameTime = 0.05f;

    [Header("Jumping")]
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = 0.4f;

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    [Header("Wall Sliding")]
    public float wallSlideSpeedMax = 3;
    public float wallStickTime = 0.25f;


    private float timeToWallUnstick;
    private float accelerationTimeAirborne = 0.2f;
    private float accelerationTimeGrounded = 0.1f;
    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private Vector3 velocity;
    private Controller2D controller;
    private float velocityXSmoothing;
    private Vector2 directionalInput;
    private bool wallSliding;
    private int wallDirX;
    private bool dashing;

    public bool PlayerOnGround() { return controller.collisions.below; }
    public void SetDirectionalInput(Vector2 input) { directionalInput = input; }

    void Start()
    {
        controller = GetComponent<Controller2D>();

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    void Update()
    {
        CalculateVelocity();
        HandleWallSliding();

        controller.Move(velocity * Time.deltaTime, directionalInput);

        if (controller.collisions.above || PlayerOnGround())
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            }
            else { velocity.y = 0; }
        }
    }

    public void OnJumpInputDown()
    {
        if (wallSliding)
        {
            // Wall jump velocity is dependent upon input direction relative to the wall.
            if (wallDirX == directionalInput.x){ velocity = wallJumpClimb; }
            else if (directionalInput.x == 0){ velocity = wallJumpOff; }
            else{ velocity = wallLeap; }

            // Ensure the jump is horizontally away from the wall.
            velocity.x *= -wallDirX;
        }

        if (PlayerOnGround())
        {
            if (controller.collisions.readyToFallThroughPlatform)
            {
                controller.collisions.readyToFallThroughPlatform = false;
                controller.collisions.fallingThroughPlatform = true;
                controller.Invoke("ResetFallingThroughPlatform", 0.5f);
            }
            else
            {
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
            Invoke("Dash", dashFrameTime);
        }

        // Plan on ending this dash.
        Invoke("DashCancel", dashTime);
    }

    // Begin a new dash.
    void Dash()
    {
        if (!dashing)
        {
            dashing = true;
            moveSpeed *= dashSpeed;
        }
    }

    // End a current dash.
    void DashCancel()
    {
        if (dashing)
        {
            dashing = false;
            moveSpeed /= dashSpeed;
        }
    }

    void HandleWallSliding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;

        if ((controller.collisions.left || controller.collisions.right) && (!PlayerOnGround()) && velocity.y < 0)
        {
            wallSliding = true;

            if (velocity.y < -wallSlideSpeedMax) { velocity.y = -wallSlideSpeedMax; }

            if (timeToWallUnstick > 0)
            {
                velocity.x = 0;
                velocityXSmoothing = 0;

                if (directionalInput.x != wallDirX && directionalInput.x != 0) { timeToWallUnstick -= Time.deltaTime; }
                else { timeToWallUnstick = wallStickTime; }
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }
    }

    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        float accelerationTime = PlayerOnGround() ? accelerationTimeGrounded : accelerationTimeAirborne;

        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTime);
        velocity.y += gravity * Time.deltaTime;
    }

    // Debug stuff
    void OnDrawGizmos()
    {
        Gizmos.color = dashing ? new Color(0, 1, 0, 0.7f) : new Color(0, 0, 1, 0.7f);
        Gizmos.DrawCube(transform.position, new Vector3(0.3f, 0.3f, 0.3f));
    }
}
