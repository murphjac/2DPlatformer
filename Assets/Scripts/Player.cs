using System.Collections;
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
    private int numJumpsRemaining;
    private bool wallSliding;
    private bool dashing;
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

        // Reset the number of jumps remaining upon gaining traction on a surface.
        if (PlayerOnGround() || wallSliding) { numJumpsRemaining = currentProperties.numJumps; }
    }

    void UpdatePlayerProperties(MovementProperties mp)
    {
        currentProperties = mp;

        gravity = -(2 * currentProperties.maxJumpHeight) / Mathf.Pow(currentProperties.timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * currentProperties.timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * currentProperties.minJumpHeight);
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

        if (wallSliding)
        {
            // Wall jump velocity is dependent upon input direction relative to the wall.
            if (wallDirX == directionalInput.x){ velocity = currentProperties.wallJumpClimb; }
            else if (directionalInput.x == 0){ velocity = currentProperties.wallJumpOff; }
            else{ velocity = currentProperties.wallLeap; }

            // Ensure the jump is horizontally away from the wall.
            velocity.x *= -wallDirX;
        }

        if (PlayerOnGround() || numJumpsRemaining > 0)
        {
            if (controller.collisions.readyToFallThroughPlatform)
            {
                controller.collisions.readyToFallThroughPlatform = false;
                controller.collisions.fallingThroughPlatform = true;
                controller.Invoke("ResetFallingThroughPlatform", 0.5f);
            }
            else
            {
                numJumpsRemaining--;

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
            Invoke("Dash", currentProperties.dashFrameTime);
        }

        // Plan on ending this dash.
        Invoke("DashCancel", currentProperties.dashTime);
    }

    // Begin a new dash.
    void Dash()
    {
        if (!dashing)
        {
            dashing = true;
            currentProperties.moveSpeed *= currentProperties.dashSpeed;
            velocity.y = 0;
        }
    }

    // End a current dash.
    void DashCancel()
    {
        if (dashing)
        {
            dashing = false;
            currentProperties.moveSpeed /= currentProperties.dashSpeed;
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
        float targetVelocityX = directionalInput.x * currentProperties.moveSpeed;
        float accelerationTime = PlayerOnGround() ? currentProperties.gndAccelTime : currentProperties.airAccelTime;

        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, accelerationTime);
        if (!dashing) { velocity.y += gravity * Time.deltaTime; }
    }

    // Debug stuff
    void OnDrawGizmos()
    {
        Gizmos.color = dashing ? new Color(0, 1, 0, 0.7f) : new Color(0, 0, 1, 0.7f);
        Gizmos.DrawCube(transform.position, new Vector3(0.3f, 0.3f, 0.3f));
    }

    [System.Serializable]
    public struct MovementProperties
    {
        [Header("Movement")]
        public float moveSpeed;
        public float airAccelTime;
        public float gndAccelTime;

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
        public float wallSlideSpeedMax;
        public float wallStickTime;
    }
}
