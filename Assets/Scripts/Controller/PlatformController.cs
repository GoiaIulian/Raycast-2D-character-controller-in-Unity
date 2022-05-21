using System;
using UnityEngine;
using System.Collections.Generic;

namespace Controller
{
    public class PlatformController : RaycastController
    {
        public LayerMask passengerMask;

        public Vector3[] localWayPoints;
        private Vector3[] _globalWayPoints;

        public float speed;
        public bool cyclic;
        public float waitTime;
        [Range(0,2)]
        public float easeAmount;
        
        private int _fromWayPointIndex;
        private float _percentageBetweenWayPoints;
        private float _nextMoveTime;

        private List<PassengerMovement> _passengerMovements;
        private readonly Dictionary<Transform, Controller2D> _passengersDictionary = new Dictionary<Transform, Controller2D>();
        
        //========================================================================================//
        public override void Start()
        {
            base.Start();

            _globalWayPoints = new Vector3[localWayPoints.Length];
            for (var i = 0; i < localWayPoints.Length; i++)
            {
                _globalWayPoints[i] = localWayPoints[i] + transform.position;
            }
        }

        //========================================================================================//
        private void FixedUpdate()
        {
            UpdateRaycastOrigins();

            var velocity = CalculatePlatformMovement();
            
            CalculatePassengerMovement(velocity);
            
            MovePassengers(true);
            transform.Translate(velocity);
            MovePassengers(false);
        }

        //========================================================================================//
        private void MovePassengers(bool beforeMovePlatform)
        {
            foreach (var passengerMovement in _passengerMovements)
            {
                if (!_passengersDictionary.ContainsKey(passengerMovement.passengerTransform))
                {
                    _passengersDictionary.Add(passengerMovement.passengerTransform,
                        passengerMovement.passengerTransform.GetComponent<Controller2D>());
                }
                
                if (passengerMovement.moveBeforePlatform == beforeMovePlatform)
                {
                    _passengersDictionary[passengerMovement.passengerTransform]
                        .Move(passengerMovement.passengerVelocity, passengerMovement.standingOnPlatform);
                }
            }
        }
        
        //========================================================================================//
        private void CalculatePassengerMovement(Vector3 velocity)
        {
            var movedPassengers = new HashSet<Transform>();
            _passengerMovements = new List<PassengerMovement>();
            
            var directionX = Mathf.Sign(velocity.x);
            var directionY = Mathf.Sign(velocity.y);
            
            if (velocity.y != 0)
            {
                var rayLength = Mathf.Abs(velocity.y) + SkinWidth;

                for (var i = 0; i < verticalRayCount; i++)
                {
                    var rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                    rayOrigin += Vector2.right * (verticalRaySpacing * i);
                    var hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                    if (!hit || hit.distance == 0) continue;
                    if (movedPassengers.Contains(hit.transform)) continue;
                    
                    movedPassengers.Add(hit.transform);
                            
                    var pushX = directionY == 1 ? velocity.x : 0;
                    var pushY = velocity.y - (hit.distance - SkinWidth) * directionY;

                    _passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY),
                        directionY == 1, true));
                }
            }

            if (velocity.x != 0)
            {
                var rayLength = Mathf.Abs(velocity.x) + SkinWidth;

                for (var i = 0; i < horizontalRayCount; i++)
                {
                    var rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                    rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                    var hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                    if (!hit || hit.distance == 0) continue;
                    if (movedPassengers.Contains(hit.transform)) continue;
                    
                    movedPassengers.Add(hit.transform);
                            
                    var pushX = velocity.x - (hit.distance - SkinWidth) * directionX;
                    const float pushY = -SkinWidth;
                    _passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY),
                        false, true));
                }
            }

            if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
            {
                const float rayLength = SkinWidth * 2;

                for (var i = 0; i < verticalRayCount; i++)
                {
                    var rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                    var hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                    if (!hit || hit.distance == 0) continue;
                    if (movedPassengers.Contains(hit.transform)) continue;
                    
                    movedPassengers.Add(hit.transform);
                            
                    var pushX = velocity.x;
                    var pushY = velocity.y;
                    _passengerMovements.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY),
                        true, false));
                }
            }
        }
        
        //========================================================================================//
        private float Ease(float x)
        {
            var a = easeAmount + 1;
            return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
        }
        
        //========================================================================================//
        private Vector3 CalculatePlatformMovement()
        {
            if (Time.time < _nextMoveTime) return Vector3.zero;

            _fromWayPointIndex %= _globalWayPoints.Length;
            
            var toWayPointIndex = (_fromWayPointIndex + 1) % _globalWayPoints.Length;
            var distanceBetweenWayPoints =
                Vector3.Distance(_globalWayPoints[_fromWayPointIndex], _globalWayPoints[toWayPointIndex]);
            _percentageBetweenWayPoints += Time.deltaTime * speed / distanceBetweenWayPoints;
            _percentageBetweenWayPoints = Mathf.Clamp01(_percentageBetweenWayPoints);

            var easedPercentBetweenWayPoints = Ease(_percentageBetweenWayPoints);

            var newPosition = Vector3.Lerp(_globalWayPoints[_fromWayPointIndex], _globalWayPoints[toWayPointIndex],
                easedPercentBetweenWayPoints);

            if (_percentageBetweenWayPoints >= 1)
            {
                _percentageBetweenWayPoints = 0;
                _fromWayPointIndex++;
                if (!cyclic)
                {
                    if (_fromWayPointIndex >= _globalWayPoints.Length - 1)
                    {
                        _fromWayPointIndex = 0;
                        Array.Reverse(_globalWayPoints);
                    }
                }

                _nextMoveTime = Time.time + waitTime;
            }

            return newPosition - transform.position;
        }
        
        //========================================================================================//
        private void OnDrawGizmos()
        {
            if (localWayPoints == null) return;
            Gizmos.color = Color.magenta;
            const float size = .3f;

            for (var i = 0; i < localWayPoints.Length; i++)
            {
                var globalWayPointPos = Application.isPlaying
                    ? _globalWayPoints[i]
                    : localWayPoints[i] + transform.position;
                Gizmos.DrawLine(globalWayPointPos - Vector3.up * size, globalWayPointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWayPointPos - Vector3.right * size, globalWayPointPos + Vector3.right * size);
            }
        }

        //========================================================================================//
        private readonly struct PassengerMovement
        {
            public readonly Transform passengerTransform;
            public readonly Vector3 passengerVelocity;
            public readonly bool standingOnPlatform;
            public readonly bool moveBeforePlatform;

            public PassengerMovement(Transform t, Vector3 v, bool sop, bool mbp)
            {
                passengerTransform = t;
                passengerVelocity = v;
                standingOnPlatform = sop;
                moveBeforePlatform = mbp;
                moveBeforePlatform = mbp;
            }
        }
    }
}