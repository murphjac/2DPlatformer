using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller2D : RaycastController {
    const int LEFT = -1, RIGHT = 1, UP = 1, DOWN = -1;

    public float maxSlopeAngle = 80;
    public CollisionInfo collisions;

    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = RIGHT;
    }

    // Overload for Movement when no input is provided.
    public void Move(Vector2 moveAmount, bool standingOnPlatform = false)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.moveAmountOld = moveAmount;
        playerInput = input;

        if (moveAmount.y < 0) { DescendSlope(ref moveAmount); }
        if(moveAmount.x != 0) { collisions.faceDir = (int)Mathf.Sign(moveAmount.x); }

        HorizontalCollisions(ref moveAmount);
        if (moveAmount.y != 0) { VerticalCollisions(ref moveAmount); }

        transform.Translate(moveAmount);

        if (standingOnPlatform){ collisions.below = true; }
    }

    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
        int breakableHitCount = 0;

        if(Mathf.Abs(moveAmount.x) < skinWidth) { rayLength = 2 * skinWidth; }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == LEFT) ? raycastOrigins.botLeft : raycastOrigins.botRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.green);

            if (hit && hit.distance != 0)
            {
                // Hit a breakable object.
                if (hit.collider.tag == "Breakable")
                {
                    // Destroy a breakable object only if the player is only colliding with breakable objects.
                    if(++breakableHitCount == horizontalRayCount) { Destroy(hit.collider.gameObject); }
                    continue;
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // Hit a climbable slope.
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    // Descending a slope while about to ascend another.
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }

                    float distanceToSlopeStart = 0;
                    if(slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }

                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                // Not currently climbing a slope, or the slope angle is too steep to climb.
                if(!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;

                    if (collisions.climbingSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad * Mathf.Abs(moveAmount.x));
                    }

                    collisions.left = (directionX == LEFT);
                    collisions.right = (directionX == RIGHT);
                }
            }
        }
    }

    void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

        // Check for collisions with vertical raycasts.
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == DOWN) ? raycastOrigins.botLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.green);

            if (hit)
            {
                // Allow jumping/falling through appropriately tagged platforms.
                if(hit.collider.tag == "ThroughPlatform")
                {
                    // Any of these conditions determine a state where fall-through platforms should ignore collision.
                    if (directionY == UP || hit.distance == 0 || collisions.fallingThroughPlatform) { continue; }
                    
                    // If holding 'down' on a fall-through platform, the next jump is staged to fall through instead.
                    collisions.readyToFallThroughPlatform = (playerInput.y == DOWN);
                }

                rayLength = hit.distance;
                moveAmount.y = (rayLength - skinWidth) * directionY;

                if (collisions.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                collisions.below = (directionY == DOWN);
                collisions.above = (directionY ==  UP);
            }
        }

        // Handle transitioning between multiple slopes.
        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == LEFT) ? raycastOrigins.botLeft : raycastOrigins.botRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if(slopeAngle != collisions.slopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (moveAmount.y <= climbVelocityY)
        {
            moveAmount.y = climbVelocityY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            collisions.below = collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
    }

    void DescendSlope(ref Vector2 moveAmount)
    {
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.botLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.botRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);

        if(maxSlopeHitLeft ^ maxSlopeHitRight)
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }

        if (!collisions.slidingDownMaxSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            Vector2 rayOrigin = (directionX == LEFT) ? raycastOrigins.botRight : raycastOrigins.botLeft;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if  (
                    (slopeAngle != 0) &&
                    (slopeAngle <= maxSlopeAngle) &&
                    (Mathf.Sign(hit.normal.x) == directionX) &&
                    (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                    )
                {
                    float moveDistance = Mathf.Abs(moveAmount.x);
                    float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

                    moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                    moveAmount.y -= descendVelocityY;

                    collisions.slopeAngle = slopeAngle;
                    collisions.descendingSlope = true;
                    collisions.below = true;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeAngle > maxSlopeAngle)
            {
                moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }

    void ResetFallingThroughPlatform(){ collisions.fallingThroughPlatform = false; }

    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public bool climbingSlope, descendingSlope;
        public bool slidingDownMaxSlope;
        public bool fallingThroughPlatform;
        public bool readyToFallThroughPlatform;

        public float slopeAngle, slopeAngleOld;

        public Vector2 slopeNormal;
        public Vector2 moveAmountOld;

        public int faceDir;

        public void Reset()
        {
            above = below = left = right = false;
            climbingSlope = descendingSlope = false;
            slidingDownMaxSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
            slopeNormal = Vector2.zero;
        }
    }
}
