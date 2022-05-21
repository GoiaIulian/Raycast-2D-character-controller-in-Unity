using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

namespace Controller
{
    
    public class Controller2D : RaycastController
    {
        #region VARIABLES
        
        public CollisionInfo collisionStatus;

        private readonly float _maxSlopeAngle = 70;

        private Vector2 _playerInput;

        #endregion

        public override void Start()
        {
            base.Start();
            collisionStatus.faceDirection = 1;
        }
        
        //========================================================================================//
        public void Move(Vector2 moveAmount, bool standingOnPlatform = false)
        {
            Move(moveAmount, Vector2.zero, standingOnPlatform);
        }
        
        //========================================================================================//
        public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
        {
            UpdateRaycastOrigins();
            collisionStatus.Reset();
            collisionStatus.moveAmountOld = moveAmount;
            _playerInput = input;

            if (moveAmount.y < 0)
            {
                DescendSlope(ref moveAmount);
            }
            
            if (moveAmount.x != 0)
            {
                collisionStatus.faceDirection = (int) Mathf.Sign(moveAmount.x);
            }
            
            HorizontalCollisions(ref moveAmount);

            if (moveAmount.y != 0)
            {
                VerticalCollisions(ref moveAmount);
            }
            
            transform.Translate(moveAmount);

            if (standingOnPlatform)
            {
                collisionStatus.below = true;
            }
        }
        
        //========================================================================================//
        private void VerticalCollisions(ref Vector2 moveAmount)
        {
            var directionY = Mathf.Sign(moveAmount.y);
            var rayLength = Mathf.Abs(moveAmount.y) + SkinWidth;
            
            for (var i = 0; i < verticalRayCount; i++)
            {
                var rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
                var hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
                
                Debug.DrawRay(rayOrigin, Vector2.up * (directionY), Color.yellow);

                if (!hit) continue;

                if (hit.collider.CompareTag("Through"))
                {
                    if (directionY == 1 || hit.distance == 0) continue;

                    if (collisionStatus.fallingThroughPlatform) continue;

                    if (_playerInput.y == -1)
                    {
                        collisionStatus.fallingThroughPlatform = true;
                        Invoke(nameof(ResetFallingThroughPlatform), .5f);
                        continue;
                    }
                }
                
                moveAmount.y = (hit.distance - SkinWidth) * directionY;
                rayLength = hit.distance;

                if (collisionStatus.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisionStatus.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                collisionStatus.below = directionY == -1;
                collisionStatus.above = directionY == 1;
            }

            if (!collisionStatus.climbingSlope) return;
            {
                var directionX = Mathf.Sign(moveAmount.x);
                rayLength = Mathf.Abs(moveAmount.x) + SkinWidth;
                var rayOrigin = (directionX == -1 ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) +
                                Vector2.up * moveAmount.y;
                var hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
                
                if (!hit) return;

                var slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                
                if (slopeAngle == collisionStatus.slopeAngle) return;
                
                moveAmount.x = (hit.distance - SkinWidth) * directionX;
                collisionStatus.slopeAngle = slopeAngle;
                collisionStatus.slopeNormal = hit.normal;
            }
        }
        
        //========================================================================================//
        private void HorizontalCollisions(ref Vector2 moveAmount)
        {
            var directionX = collisionStatus.faceDirection;
            var rayLength = Mathf.Abs(moveAmount.x) + SkinWidth;

            if (Mathf.Abs(moveAmount.x) < SkinWidth)
            {
                rayLength = 2 * SkinWidth;
            }
            
            for (var i = 0; i < horizontalRayCount; i++)
            {
                var rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                var hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
                
                Debug.DrawRay(rayOrigin, Vector2.right * (directionX), Color.yellow);

                if (!hit) continue;

                if (hit.distance == 0) continue;

                var slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (i == 0 && slopeAngle <= _maxSlopeAngle)
                {
                    var distanceToSlopeStart = 0f;

                    if (slopeAngle != collisionStatus.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - SkinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                if (collisionStatus.climbingSlope && !(slopeAngle > _maxSlopeAngle)) continue;

                if (collisionStatus.descendingSlope)
                {
                    collisionStatus.descendingSlope = false;
                    moveAmount = collisionStatus.moveAmountOld;
                }
                
                moveAmount.x = (hit.distance - SkinWidth) * directionX;
                rayLength = hit.distance;

                if (collisionStatus.climbingSlope)
                {
                    moveAmount.y = Mathf.Tan(collisionStatus.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                }

                collisionStatus.left = directionX == -1;
                collisionStatus.right = directionX == 1;
            }   
        }
        
        //========================================================================================//
        private void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
        {
            var moveDistance = Mathf.Abs(moveAmount.x);
            var climbMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

            if (moveAmount.y > climbMoveAmountY)
                return;
            
            moveAmount.y = climbMoveAmountY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);

            collisionStatus.below = true;
            collisionStatus.climbingSlope = true;
            collisionStatus.slopeAngle = slopeAngle;
            collisionStatus.slopeNormal = slopeNormal;
        }
        
        //========================================================================================//
        private void DescendSlope(ref Vector2 moveAmount)
        {
            var maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down,
                Mathf.Abs(moveAmount.y) + SkinWidth, collisionMask);
            var maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down,
                Mathf.Abs(moveAmount.y) + SkinWidth, collisionMask);

            if (maxSlopeHitLeft ^ maxSlopeHitRight)
            {
                SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
                SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
            }
            
            if (collisionStatus.slidingDownMaxSlope) return;
            
            var directionX = Mathf.Sign(moveAmount.x);
            var rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            var hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);
            
            if (!hit) return;

            var slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            
            if (slopeAngle == 0 || !(slopeAngle <= _maxSlopeAngle)) return;
            
            if (Mathf.Sign(hit.normal.x) != directionX) return;
                
            if (!(hit.distance - SkinWidth <=
                  Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))) 
                return;
                    
            var moveDistance = Mathf.Abs(moveAmount.x);
            var descendMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            moveAmount.y -= descendMoveAmountY;

            collisionStatus.slopeAngle = slopeAngle;
            collisionStatus.descendingSlope = true;
            collisionStatus.below = true;
            collisionStatus.slopeNormal = hit.normal;
        }
        
        //========================================================================================//
        private void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
        {
            if (!hit) return;
            
            var slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (!(slopeAngle > _maxSlopeAngle)) return;
            
            moveAmount.x = hit.normal.x * (Mathf.Abs(moveAmount.y) - hit.distance) /
                           Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

            collisionStatus.slopeAngle = slopeAngle;
            collisionStatus.slidingDownMaxSlope = true;
            collisionStatus.slopeNormal = hit.normal;
        }
        
        //========================================================================================//
        private void ResetFallingThroughPlatform()
        {
            collisionStatus.fallingThroughPlatform = false;
        }
        
        //========================================================================================//
        public struct CollisionInfo
        {
            public bool above, below;
            public bool left, right;

            public Vector2 moveAmountOld;

            public bool climbingSlope;
            public bool descendingSlope;
            public bool slidingDownMaxSlope;
            public bool fallingThroughPlatform;
            
            public float slopeAngle;
            public float slopeAngleOld;

            public Vector2 slopeNormal;
            
            public int faceDirection;

            public void Reset()
            {
                above = below = left = right = false;
                climbingSlope = false;
                descendingSlope = false;
                slidingDownMaxSlope = false;

                slopeNormal = Vector2.zero;
                
                slopeAngleOld = slopeAngle;
                slopeAngle = 0;
            }
        }
    }
}
