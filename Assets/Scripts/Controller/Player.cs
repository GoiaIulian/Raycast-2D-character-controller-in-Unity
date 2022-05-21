using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Controller
{
    [RequireComponent(typeof(Controller2D))]
    public class Player : MonoBehaviour
    {
        #region VARIABLES
        [SerializeField]
        private float maxJumpHeight = 4f;
        [SerializeField]
        private float minJumpHeight = 1f;
        [SerializeField]
        private float timeToJumpApex = .4f;
        
        private float _gravity;
        private float _maxJumpVelocity;
        private float _minJumpVelocity;

        private const float AccelerationTimeAirborne = .2f;
        private const float AccelerationTimeGrounded = .1f;
        private float _velocityXSmoothing;

        public float wallStickTime = .25f;
        private float _timeToWallUnstick;

        private Vector2 _directionalInput;

        public Vector2 walJumpClimb;
        public Vector2 wallJumpHop;
        public Vector2 wallJumpLeap;
        
        private Vector3 _velocity;
        public float moveSpeed = 8;
        public float wallSlideSpeedMax = 3;
        
        private Controller2D _controller2D;

        private bool _wallSliding;
        private int _wallDirectionX;
        #endregion
        
        //========================================================================================//
        private void Start()
        {
            _controller2D = GetComponent<Controller2D>();

            _gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
            _maxJumpVelocity = Mathf.Abs(_gravity) * timeToJumpApex;
            _minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(_gravity) * minJumpHeight);
            _directionalInput = Vector2.zero;
        }
        
        //========================================================================================//
        private void FixedUpdate()
        {
            CalculateVelocity();
            HandleWallSliding();

            _controller2D.Move(_velocity * Time.deltaTime, _directionalInput);

            if (!_controller2D.collisionStatus.below && !_controller2D.collisionStatus.above) 
                return;
            
            if (_controller2D.collisionStatus.slidingDownMaxSlope)
            {
                _velocity.y += _controller2D.collisionStatus.slopeNormal.y * -_gravity * Time.deltaTime;
            }
            else
            {
                _velocity.y = 0;
            }
        }
        
        //========================================================================================//
        private void HandleWallSliding()
        {
            _wallDirectionX = _controller2D.collisionStatus.left ? -1 : 1;
            _wallSliding = false;
            if ((_controller2D.collisionStatus.left || _controller2D.collisionStatus.right)
                && !_controller2D.collisionStatus.below
                && _velocity.y < 0)
            {
                _wallSliding = true;
                if (_velocity.y < -wallSlideSpeedMax)
                {
                    _velocity.y = -wallSlideSpeedMax;
                }

                if (_timeToWallUnstick > 0)
                {
                    _velocityXSmoothing = 0;
                    _velocity.x = 0;
                    if (_directionalInput.x != _wallDirectionX && _directionalInput.x != 0)
                    {
                        _timeToWallUnstick -= Time.deltaTime;
                    }
                    else
                    {
                        _timeToWallUnstick = wallStickTime;
                    }
                }
                else
                {
                    _timeToWallUnstick = wallStickTime;
                }
            }
        }

        //========================================================================================//
        private void CalculateVelocity()
        {
            var targetVelocityX = _directionalInput.x * moveSpeed;
            _velocity.x = Mathf.SmoothDamp(_velocity.x, targetVelocityX, ref _velocityXSmoothing,
                (_controller2D.collisionStatus.below ? AccelerationTimeGrounded : AccelerationTimeAirborne));
            _velocity.y += _gravity * Time.deltaTime;
        }

        //========================================================================================//
        public void SetDirectionalInput(Vector2 input)
        {
            _directionalInput = input;
        }
        
        //========================================================================================//
        public void OnJumpInputDown()
        {
            if (_wallSliding)
            {
                if (_wallDirectionX == _directionalInput.x)
                {
                    _velocity.x = -_wallDirectionX * walJumpClimb.x;
                    _velocity.y = walJumpClimb.y;
                }
                else if (_directionalInput.x == 0)
                {
                    _velocity.x = -_wallDirectionX * wallJumpHop.x;
                    _velocity.y = wallJumpHop.y;
                }
                else
                {
                    _velocity.x = -_wallDirectionX * wallJumpLeap.x;
                    _velocity.y = wallJumpLeap.y;
                }
            }

            if (!_controller2D.collisionStatus.below) return;
            
            if (_controller2D.collisionStatus.slidingDownMaxSlope)
            {
                if (_directionalInput.x != -Mathf.Sign(_controller2D.collisionStatus.slopeNormal.x))
                {
                    _velocity.y = _maxJumpVelocity * _controller2D.collisionStatus.slopeNormal.y;
                    _velocity.x = _maxJumpVelocity * _controller2D.collisionStatus.slopeNormal.x;
                }
            }
            else
            {
                _velocity.y = _maxJumpVelocity;
            }
        }
        
        //========================================================================================//
        public void OnJumpInputUp()
        {
            if (_velocity.y > _minJumpVelocity)
            {
                _velocity.y = _minJumpVelocity;
            }
        }
    }
}
