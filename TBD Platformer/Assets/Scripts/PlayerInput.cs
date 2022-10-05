using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CustomPlatformerPhysics2D
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Renderer))]
    public class PlayerInput : MonoBehaviour
    {
        [Title("General Properties")]
        [SerializeField] SavePointController m_savePointController;
        bool m_freezeMovement = false;
        Vector2 m_input;
        Vector3 m_inputVelocity;
        Vector3 m_displacement;
        PlayerController m_controller;
        Animator m_animator;
        Renderer m_rend;
        GameManager m_gameManager;

        [Title("Horizontal Movement Properties")]
        [SerializeField] float m_xMoveSpeed = 6;
        [SerializeField] float m_xAccelerationTimeGrounded = 0.1f;
        [SerializeField] float m_xAccelerationTimeAir = 0.2f;
        [SerializeField] float m_xDecelerationTimeGrounded = 0.1f;
        [SerializeField] float m_xDecelerationTimeAir = 0.4f;
        float m_velocityXSmoothing;
        float m_lastTargetXVelocity = 0;

        [Title("Vertical Movement Properties")]
        [SerializeField] float m_maxJumpHeight = 4;
        [SerializeField] float m_minJumpHeight = 1;
        [SerializeField] float m_timeToJumpApex = 0.4f;
        bool m_isJumping = false;
        float m_gravity;
        float m_maxJumpVelocity;
        float m_minJumpVelocity;
        float m_maxAllowedVelocityY;
        //It's more intuitive to set jump height and time than to set gravity and initial velocity.
        //We can calculate gravity and jumpVelocity with these two data.

        [Title("Wall Movement Properties")]
        //Wall jump parameters
        [SerializeField] bool m_wallJumpActivate = true;
        [ShowIfGroup("m_wallJumpActivate")]
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 m_wallJumpClimb = new Vector2(7.5f, 16);
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 m_wallJumpOff = new Vector2(8.5f, 7);
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 m_wallLeap = new Vector2(18, 19);
        //Specifically for wall leaping, we want to temporarily pause player on each leap so that they don't slide down
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] public float m_wallStickTime = 0.15f;
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] public float m_ceilingStickTime = 0.15f;
        float m_timeToWallUnstick;
        float m_timeToCeilUnstick;
        bool m_stickToSlopeCeiling = false;
        bool m_stickToAbsoluteCeiling = false;

        //Wall slide parameter
        [BoxGroup("Wall Slide")][SerializeField] float m_wallSlideSpeedMax = 3;
        [BoxGroup("Wall Slide")][SerializeField] float m_wallSlideGravBuffer = 0.5f;

        /*private void OnEnable()
        {
            posCharacter = m_savePointController.character1LocalPos + transform.position;
            posCharacterFlipped = m_savePointController.character2LocalPos + transform.position;

            GameObject.Find("GameManager").GetComponent<GameManager>().playerInputParent = this;
        }*/

        private void Start()
        {
            m_controller = GetComponent<PlayerController>();
            m_rend = GetComponent<Renderer>();
            m_animator = GetComponent<Animator>();
            m_gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            //Save Point
            //transform.position = m_savePointController.lastSavePos;

            //Movement Properties Calculation
            m_gravity = -2 * m_maxJumpHeight / Mathf.Pow(m_timeToJumpApex, 2);
            //kinetic movement equation [v0t + (1/2)gt^2] = s. Here we can assume v0 = 0 because the time is exact the same as throwing object down.
            m_maxJumpVelocity = (-1) * m_gravity * m_timeToJumpApex;
            //vt = vo + gt
            m_minJumpVelocity = -Mathf.Sign(m_gravity) * Mathf.Sqrt(2 * Mathf.Abs(m_gravity) * m_minJumpHeight);
            m_maxAllowedVelocityY = m_gameManager.GetMaxAllowedYVelocity();

            m_timeToWallUnstick = m_wallStickTime;
            m_timeToCeilUnstick = m_ceilingStickTime;
        }

        void Update()
        {
            if (m_freezeMovement) return;

            if (m_gameManager.GetGameOver())
            {
                m_inputVelocity.x = 0;
                CalculateMovement(false);
                return;
            }

            /*if (m_controller.GetLastDisplacement().y < -m_maxAllowedVelocityY && m_gameManager.GetGameStart())
            {
                m_gameManager.SetGameOver();
                CalculateMovement(false);
                return;
            }*/

            //Get basic movement inputs
            m_input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            //For animation! Turn the player when they change direction
            if (m_input.x > 0) transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
            else if (m_input.x < 0) transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);

            //Vertical movement calculation
            //Check if the player is on the ground
            bool _isGrounded = m_controller.getCollisionInfo().below;
            bool _isCeiling = m_controller.getCollisionInfo().above;
            if (_isGrounded || _isCeiling) m_animator.SetBool("isJumping", false);
            //Reset vertical velocity to 0 when on the ground or touching the ceiling
            if (_isGrounded || _isCeiling)
            {
                m_inputVelocity.y = 0;
            }

            if (_isGrounded || _isCeiling) m_isJumping = false;

            //Calculate horizontal velocity
            float _targetVelocityX = m_input.x * m_xMoveSpeed;
            if (Mathf.Sign(_targetVelocityX) == Mathf.Sign(m_lastTargetXVelocity) && (Mathf.Abs(_targetVelocityX) - Mathf.Abs(m_lastTargetXVelocity) < 0))
            {
                m_inputVelocity.x = Mathf.SmoothDamp(m_inputVelocity.x, _targetVelocityX, ref m_velocityXSmoothing, (_isGrounded) ? m_xDecelerationTimeGrounded : m_xDecelerationTimeAir);
            } else
            {
                m_inputVelocity.x = Mathf.SmoothDamp(m_inputVelocity.x, _targetVelocityX, ref m_velocityXSmoothing, (_isGrounded) ? m_xAccelerationTimeGrounded : m_xAccelerationTimeAir);
            }
            m_lastTargetXVelocity = _targetVelocityX;

            //Wall sliding, vertical speed reduced
            int _wallDirX = (m_controller.getCollisionInfo().left) ? -1 : 1;
            bool _wallSliding = false;
            if ((m_controller.getCollisionInfo().left || m_controller.getCollisionInfo().right) && (!_isGrounded))
            {
                if (m_inputVelocity.y < 0)
                {
                    _wallSliding = true;

                    //Set maximum speed
                    if (m_inputVelocity.y > -m_wallSlideSpeedMax)
                    {
                        m_inputVelocity.y = -m_wallSlideSpeedMax;
                    }

                    //Before the player jump off/leap, there will be a temperory gap before the action happens
                    if (m_timeToWallUnstick > 0)
                    {
                        m_velocityXSmoothing = 0;
                        m_inputVelocity.x = 0;

                        //Can only start counting when the x input does not point towards wall
                        if (m_input.x != _wallDirX && (m_input.x != 0))
                        {
                            m_timeToWallUnstick -= Time.deltaTime;
                        }
                        else
                        {
                            m_timeToWallUnstick = m_wallStickTime;
                        }
                    }
                }

                if (m_controller.getCollisionInfo().touchSlopeCeiling)
                {
                    m_stickToSlopeCeiling = true;
                }

                if (m_stickToSlopeCeiling)
                {
                    _wallSliding = true;
                    if (m_timeToCeilUnstick > 0)
                    {
                        m_timeToCeilUnstick -= Time.deltaTime;
                    }
                    else
                    {
                        m_stickToSlopeCeiling = false;
                        m_timeToCeilUnstick = m_ceilingStickTime;
                    }
                } else
                {
                    m_stickToSlopeCeiling = false;
                }
            }
            else
            {
                m_stickToSlopeCeiling = false;
                m_timeToCeilUnstick = m_ceilingStickTime;
                m_timeToWallUnstick = m_wallStickTime;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_animator.SetBool("isJumping", true);

                if (_wallSliding)
                {
                    //Jump climb/Leap the wall
                    if (m_wallJumpActivate)
                    {
                        if (_wallDirX == m_input.x)
                        {
                            m_inputVelocity.x = -_wallDirX * m_wallJumpClimb.x;
                            m_inputVelocity.y = m_wallJumpClimb.y;
                        }
                        else
                        {
                            m_inputVelocity.x = -_wallDirX * m_wallLeap.x;
                            m_inputVelocity.y = m_wallLeap.y;
                        }
                    }
                    //Jump off the wall
                    if (m_input.x == 0)
                    {
                        m_inputVelocity.x = -_wallDirX * m_wallJumpOff.x;
                        m_inputVelocity.y = (m_wallJumpActivate) ? m_wallJumpClimb.y : 0;
                    }
                    //Leap between two walls
                }
                if (_isGrounded)
                {
                    m_inputVelocity.y = m_maxJumpVelocity;
                    m_isJumping = true;
                }
            }

            /*during jumping, if we release the space bar before player reaches the max height
            we "terminate" the jump early by setting velocity.y to a small level*/
            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (m_isJumping)
                {
                    if (m_inputVelocity.y > m_minJumpVelocity)
                    {
                        m_inputVelocity.y = m_minJumpVelocity;
                    }
                }
                m_isJumping = false;
            }

            CalculateMovement(_wallSliding);
        }

        void CalculateMovement(bool _wallSliding)
        {
            //Calculate movement data and send it to player controller
            m_displacement.x = m_inputVelocity.x * Time.deltaTime;
            float yInitialVelocity = m_inputVelocity.y;
            float _gravity = (_wallSliding) ? (m_wallSlideGravBuffer * m_gravity) : m_gravity;
            m_inputVelocity.y += _gravity * Time.deltaTime;
            m_displacement.y = (Mathf.Pow(m_inputVelocity.y, 2) - Mathf.Pow(yInitialVelocity, 2)) / (2 * _gravity); //vt^2 - v0^2 = 2 * a * s
            if (m_stickToSlopeCeiling) m_displacement.y = 0;
            m_controller.Move(m_displacement, false, false, true);
        }

        public Vector2 getInput()
        {
            return this.m_input;
        }

        public Vector2 getDisplacement()
        {
            return this.m_displacement;
        }
    }
}
