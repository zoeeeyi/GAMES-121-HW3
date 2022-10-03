using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CustomPlatformerPhysics2D
{
    public class PlayerController : RaycastController
    {
        [Title("Slope Properties")]
        [SerializeField] float m_maxClimbAngle = 80;
        [SerializeField] float m_maxDescendAngle = 75;
        [SerializeField] bool m_canSlideDownSlope = false;
        [ShowIf("m_canSlideDownSlope")]
        [SerializeField] float m_slideDownSpeed = 100;


        [Title("Misc")]
        [SerializeField] List<string> m_killers = new List<string>();

        //Player movement related
        Vector3 m_lastDisplacement = Vector3.zero;
        PlayerInput m_playerInput;

        //Misc
        GameManager m_gameManager;
        Animator m_animator;
        CollisionInfo m_collisionInfo;

        protected override void Start()
        {
            base.Start();
            m_playerInput = GetComponent<PlayerInput>();
            //m_gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            m_animator = GetComponent<Animator>();
            m_collisionInfo.faceDir = 1; //We start facing right
        }

        public void Move(Vector3 _displacement, bool _standingOnPlatform = false, bool _overwritePlatformPush = false, bool _affectAnimation = false)
        {
            UpdateRaycastOrigins();
            m_collisionInfo.Reset();

            //When platforms do a horizontal push on the player, the pushY is 0 so player won't castRay below
            //This will cause player to skip down if they are standing on a platform. We can overwrite this with original player inputs
            if (_overwritePlatformPush)
            {
                _displacement.y = m_playerInput.getDisplacement().y;
            }

            //Check if the player is going down a slope and change the displacement if they are
            if (_displacement.y < 0)
            {
                //SlideDownSlope(ref _displacement);
                DescendSlope(ref _displacement);
            }

            if (_displacement.x != 0)
            {
                m_collisionInfo.faceDir = (int)Mathf.Sign(_displacement.x);
            }
            HorizontalCollisions(ref _displacement);

            if (_displacement.y != 0)
            {
                VerticalCollisions(ref _displacement);
                //in case there is a new slope while we are climbing the slope, we need to detect it beforehand
                if (m_collisionInfo.climbingSlope)
                {
                    SlopeTransition(ref _displacement);
                }
            }

            transform.Translate(_displacement);
            m_lastDisplacement = _displacement;

            if (_standingOnPlatform)
            {
                m_collisionInfo.below = true;
            }

            if (_affectAnimation)
            {
                m_animator.SetFloat("X Speed", Mathf.Abs(_displacement.x));
                m_animator.SetFloat("Y Speed", _displacement.y);
            }
        }

        void HorizontalCollisions(ref Vector3 _displacement)
        {
            float _directionX = m_collisionInfo.faceDir;
            float _rayLength = Mathf.Abs(_displacement.x) + m_skinWidth;

            //when the player is not moving, it cast a very small ray just to detect wall.
            if (Mathf.Abs(_displacement.x) < m_skinWidth)
            {
                _rayLength = 2 * m_skinWidth;
            }

            for (int i = 0; i < m_horizontalRayCount; i++)
            {
                Vector2 _rayOrigin = (_directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                _rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D _hit = Physics2D.Raycast(_rayOrigin, Vector2.right * _directionX, _rayLength, m_xCollisionMask);
                Debug.DrawRay(_rayOrigin, Vector2.right * _directionX * _rayLength, Color.red);

                if (_hit)
                {
                    if (_hit.distance == 0) continue;

                    //If we are moving upwards and touches a surface normal that points down, we are touching a inverted slope
                    if (_hit.normal.y < 0)
                    {
                        m_collisionInfo.touchSlopeCeiling = true;
                    }

                    float _slopeAngle = Vector2.Angle(_hit.normal, Vector2.up);
                    //each frame, start checking with the first ray if the object can climb the slope.
                    if (i == 0 && (_slopeAngle <= m_maxClimbAngle))
                    {
                        //New slope: if the slope angle is not equal to the previous one
                        if (_slopeAngle != m_collisionInfo.slopeAngleOld)
                        {
                            m_collisionInfo.descendingSlope = false;
                            m_collisionInfo.slopeAngle = _slopeAngle;
                            //move to the edge of new slope instead of immediately climb
                            _displacement.x = (_hit.distance - m_skinWidth) * _directionX;
                        }
                        else
                        {
                            ClimbSlope(ref _displacement, _slopeAngle);
                        }
                    }

                    //If the player is not climbing any slope or the slope is too steep, act normal.
                    if (!m_collisionInfo.climbingSlope || _slopeAngle > m_maxClimbAngle)
                    {
                        _displacement.x = Mathf.Min(Mathf.Abs(_displacement.x), (_hit.distance - m_skinWidth)) * _directionX;
                        _rayLength = Mathf.Min(Mathf.Abs(_displacement.x) + m_skinWidth, _hit.distance);

                        if (m_collisionInfo.climbingSlope)
                        {
                            _displacement.y = Mathf.Tan(m_collisionInfo.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(_displacement.x);
                        }

                        m_collisionInfo.left = (_directionX == -1);
                        m_collisionInfo.right = (_directionX == 1);
                    }
                }

                //------------------------------------------------------------------//

                //Cast a small ray to check opposite direction
                _rayLength = 2 * m_skinWidth;
                _rayOrigin = (_directionX == 1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                _rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                //Detect if there is an obstacle
                _hit = Physics2D.Raycast(_rayOrigin, Vector2.left * _directionX, _rayLength, m_xCollisionMask);
                Debug.DrawRay(_rayOrigin, Vector2.left * _directionX * _rayLength, Color.red);


                if (_hit)
                {
                    if (_displacement.x == 0) _displacement.x = (_hit.distance - m_skinWidth) * -_directionX;
                }

            }
        }

        void VerticalCollisions(ref Vector3 _displacement)
        {
            float _directionY = Mathf.Sign(_displacement.y);
            float _rayLength = Mathf.Abs(_displacement.y) + m_skinWidth;//the distance only cover the travel length of this frame

            for (int i = 0; i < m_verticalRayCount; i++)
            {
                Vector2 _rayOrigin = (_directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                _rayOrigin += Vector2.right * (verticalRaySpacing * i + _displacement.x);

                //Detect collision
                RaycastHit2D _hit = Physics2D.Raycast(_rayOrigin, Vector2.up * _directionY, _rayLength, m_yCollisionMask);
                Debug.DrawRay(_rayOrigin, Vector2.up * _directionY * _rayLength, Color.red);

                //Set velocity when collision is detected
                if (_hit)
                {
                    if (_hit.transform.gameObject.layer == LayerMask.NameToLayer("MovePlatform"))
                    {
                        if (_directionY == 1 || _hit.distance == 0)
                        {
                            continue;
                        }

                        //If we press down, the player can fall through platform
                        if (m_playerInput.getInput().y == -1)
                        {
                            m_collisionInfo.fallThroughPlatform = _hit.collider;
                            continue;
                        }
                        //We release down button at instant, but falling through may take some time.
                        //Without following check, game wouldn't know that the player is still falling through, sometimes will cause jigering.
                        //We need to keep track if we are still falling through the same platform.
                        if (m_collisionInfo.fallThroughPlatform == _hit.collider)
                        {
                            continue;
                        }
                    }
                    else if (m_collisionInfo.fallThroughPlatform != null)
                    {
                        m_collisionInfo.fallThroughPlatform = null;
                    }

                    if (Vector2.Angle(_hit.normal, Vector2.up) < 45)
                    {
                        _displacement.y = (_hit.distance - m_skinWidth) * _directionY;

                    }

                    //when the object touches the collider, (hit.distance - skinWidth) = 0. The velocity is set to 0.
                    _rayLength = _hit.distance;

                    if (m_collisionInfo.climbingSlope)
                    {
                        _displacement.x = _displacement.y / Mathf.Tan(m_collisionInfo.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(_displacement.x);
                    }

                    m_collisionInfo.below = (_directionY == -1);
                    m_collisionInfo.above = (_directionY == 1);
                }
            }
        }

        void SlideDownSlope(ref Vector3 _displacement)
        {
            if (_displacement.y < 0 && m_canSlideDownSlope)
            {
                float _slopeDir = 1;
                float _slopeAngle = 0;
                bool _onSlope = false;
                float _rayLength = Mathf.Abs(_displacement.y) + m_skinWidth;
                float _smallestHitDis = -1;
                for (int i = 0; i < m_horizontalRayCount; i++)
                {
                    //Vector2
                }
                for (int i = 0; i < m_verticalRayCount; i++)
                {
                    Vector2 _rayOrigin = raycastOrigins.bottomLeft;
                    _rayOrigin += Vector2.right * (verticalRaySpacing * i);
                    RaycastHit2D _hit = Physics2D.Raycast(_rayOrigin, Vector2.down, _rayLength, m_yCollisionMask);
                    Debug.DrawRay(_rayOrigin, Vector2.down * _rayLength, Color.red);

                    if (_hit)
                    {
                        if (_hit.normal.y < 0)
                        {
                            continue;
                        }
                        if (_smallestHitDis == -1)
                        {
                            _smallestHitDis = _hit.distance;
                        } else if (_hit.distance < _smallestHitDis)
                        {
                            _smallestHitDis = _hit.distance;
                        } else
                        {
                            continue;
                        }
                        _slopeAngle = Vector2.Angle(_hit.normal, Vector2.up);
                        if (_slopeAngle != 0)
                        {
                            _onSlope = true;
                            _slopeDir = Mathf.Sign(_hit.normal.x);
                        }
                    }
                }

                if (_onSlope)
                {
                    //_displacement.x += m_slideDownSpeed * _slopeDir * (-_displacement.y / Mathf.Tan(_slopeAngle * Mathf.Deg2Rad));
                    _displacement.x += m_slideDownSpeed * _slopeDir * (_slopeAngle / 90);
                }
            }
        }

        void ClimbSlope(ref Vector3 _displacement, float _slopeAngle)
        {
            float _moveDistance = Mathf.Abs(_displacement.x);
            float _yClimbVelocity = Mathf.Sin(_slopeAngle * Mathf.Deg2Rad) * _moveDistance;

            //The following will happen only if the player is not jumping
            if (_displacement.y <= _yClimbVelocity)
            {
                _displacement.y = _yClimbVelocity;
                _displacement.x = Mathf.Cos(_slopeAngle * Mathf.Deg2Rad) * _moveDistance * Mathf.Sign(_displacement.x);
                m_collisionInfo.below = true;
                m_collisionInfo.climbingSlope = true;
                m_collisionInfo.slopeAngle = _slopeAngle;
            }
        }

        void DescendSlope(ref Vector3 _displacement)
        {
            float _directionX = Mathf.Sign(_displacement.x);
            //when descending, we need to reverse the direction of ray origins to detect the collision
            Vector2 _rayOrigin = (_directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D _hit = Physics2D.Raycast(_rayOrigin, -Vector2.up, Mathf.Infinity, m_yCollisionMask);

            if (_hit)
            {
                float slopeAngle = Vector2.Angle(_hit.normal, Vector2.up);
                //check if the object is on a flat surface or the descend angle is too high
                if (slopeAngle != 0 && slopeAngle <= m_maxDescendAngle)
                {
                    //hit.normal will point "outward" a slope. If the moving direction is the same as hit.normal, the object is descending.
                    if (Mathf.Sign(_hit.normal.x) == _directionX)
                    {
                        //if the y distance to the slope down below is smaller than the distance we need to move down, we need to do the conversion
                        if (_hit.distance - m_skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(_displacement.x))
                        {
                            float moveDistance = Mathf.Abs(_displacement.x);
                            float yDescendVelocity = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            _displacement.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(_displacement.x);
                            _displacement.y -= yDescendVelocity;

                            m_collisionInfo.slopeAngle = slopeAngle;
                            m_collisionInfo.descendingSlope = true;
                            m_collisionInfo.below = true;
                        }
                    }
                }
            }
        }

        void SlopeTransition(ref Vector3 _displacement)
        {
            float _directionX = Mathf.Sign(_displacement.x);
            float _rayLength = Mathf.Abs(_displacement.x) + m_skinWidth;
            Vector2 _rayOrigin = ((_directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * _displacement.y;
            RaycastHit2D _hit = Physics2D.Raycast(_rayOrigin, Vector2.right * _directionX, _rayLength, m_xCollisionMask);

            if (_hit)
            {
                float _slopeAngle = Vector2.Angle(_hit.normal, Vector2.up);
                if (_slopeAngle != m_collisionInfo.slopeAngle)
                {
                    _displacement.x = (_hit.distance - m_skinWidth) * _directionX;
                    m_collisionInfo.slopeAngle = _slopeAngle;
                }
            }
        }

        public Vector3 GetLastDisplacement()
        {
            return m_lastDisplacement;
        }

        private void OnTriggerEnter2D(Collider2D _other)
        {
            if (m_killers.Contains(_other.tag))
            {
                //m_gameManager.ChangeGameStateTo(GameManager.GameStates.GameOver);
            }
        }

        public struct CollisionInfo
        {
            public bool above, below;
            public bool left, right;

            public bool climbingSlope;
            public bool descendingSlope;
            public bool touchSlopeCeiling;
            public float slopeAngle, slopeAngleOld;
            public int faceDir;

            public Collider2D fallThroughPlatform;

            public void Reset()
            {
                above = below = false;
                left = right = false;
                climbingSlope = false;
                descendingSlope = false;
                touchSlopeCeiling = false;

                slopeAngleOld = slopeAngle;
                slopeAngle = 0;
            }
        }

        public CollisionInfo getCollisionInfo()
        {
            return m_collisionInfo;
        }
    }
}