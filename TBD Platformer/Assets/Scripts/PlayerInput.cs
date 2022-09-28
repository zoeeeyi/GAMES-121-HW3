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
        [Space]
        [Header("General Properties")]
        [SerializeField] SavePointController m_savePointController;
        bool m_freezeMovement = false;
        Vector2 m_input;
        Vector3 m_velocity;
        Vector3 m_displarcement;
        PlayerController m_controller;
        Animator m_animator;
        Renderer m_rend;
        PlayerState m_playerState;

        enum PlayerState
        {
            Normal,
            Jumping
        }

        [Space]
        [Header("Horizontal Movement Properties")]
        [SerializeField] float m_xMoveSpeed = 6;
        [SerializeField] float m_xAccelerationTimeGrounded = 0.1f;
        [SerializeField] float m_xAccelerationTimeAir = 0.2f;
        float m_velocityXSmoothing;


        [Space]
        [Header("Vertical Movement Properties")]
        [SerializeField] float m_maxJumpHeight = 4;
        [SerializeField] float m_minJumpHeight = 1;
        [SerializeField] float m_timeToJumpApex = 0.4f;
        bool m_isJumping = false;
        float m_gravity;
        float m_maxJumpVelocity;
        float m_minJumpVelocity;
        //It's more intuitive to set jump height and time than to set gravity and initial velocity.
        //We can calculate gravity and jumpVelocity with these two data.

        [Space]
        [Header("Wall Movement Properties")]
        //Wall slide parameter
        [BoxGroup("Wall Slide")][SerializeField] float m_wallSlideSpeedMax = 3;
        [BoxGroup("Wall Slide")][SerializeField] float m_wallSlideGravBuffer = 0.5f;
        //Wall jump parameters
        [SerializeField] bool m_wallJumpActivate = true;
        [HideIfGroup("m_wallJumpActivate")]
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 m_wallJumpClimb;
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 m_wallJumpOff;
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 m_wallLeap;
        //Specifically for wall leaping, we want to temporarily pause player on each leap so that they don't slide down
        [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] public float m_wallStickTime = 0.25f;
        float m_timeToWallUnstick;

        private void OnEnable()
        {
            posCharacter = m_savePointController.character1LocalPos + transform.position;
            posCharacterFlipped = m_savePointController.character2LocalPos + transform.position;

            GameObject.Find("GameManager").GetComponent<GameManager>().playerInputParent = this;
        }

        private void Start()
        {
            m_controller = GetComponent<PlayerController>();
            m_rend = GetComponent<Renderer>();
            m_animator = GetComponent<Animator>();

            //Save Point
            transform.position = m_savePointController.lastSavePos;

            //Movement Properties Calculation
            m_gravity = -2 * m_maxJumpHeight / Mathf.Pow(m_timeToJumpApex, 2);
            //kinetic movement equation [v0t + (1/2)gt^2] = s. Here we can assume v0 = 0 because the time is exact the same as throwing object down.
            m_maxJumpVelocity = (-1) * m_gravity * m_timeToJumpApex;
            //vt = vo + gt
            m_minJumpVelocity = -Mathf.Sign(m_gravity) * Mathf.Sqrt(2 * Mathf.Abs(m_gravity) * m_minJumpHeight);
            m_playerState = PlayerState.Normal;
        }

        void Update()
        {
            if (m_freezeMovement)
            {
                return;
            }

            //Get inputs
            m_input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            //I stopped here!
            if (m_input.x > 0)
            {
                transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
            }
            else if (m_input.x < 0)
            {
                transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);
            }

            bool isGrounded = (m_controller.collisionInfo.above || m_controller.collisionInfo.below);

            if (isGrounded) m_animator.SetBool("isJumping", false);

            //Calculate movement data
            float targetVelocityX = m_input.x * parent.moveSpeed;
            m_velocity.x = Mathf.SmoothDamp(m_velocity.x, targetVelocityX, ref m_velocityXSmoothing, (isGrounded) ? parent.xAccelerationTimeGrounded : parent.xAccelerationTimeAir);

            //Wall jump
            int wallDirX = (m_controller.collisionInfo.left) ? -1 : 1;

            //Wall sliding
            bool wallSliding = false;
            //if we are sliding on the wall, vertical speed is reduced (max 3)
            if ((m_controller.collisionInfo.left || m_controller.collisionInfo.right) && (!m_controller.collisionInfo.below) && m_velocity.y < 0)
            {
                wallSliding = true;

                /*if(velocity.y < -parent.wallSlideSpeedMax)
                {
                    velocity.y = -parent.wallSlideSpeedMax;
                }*/

                if (m_timeToWallUnstick > 0)
                {
                    m_velocityXSmoothing = 0;
                    m_velocity.x = 0;

                    if (m_input.x != wallDirX && (m_input.x != 0))
                    {
                        m_timeToWallUnstick -= Time.deltaTime;
                    }
                    else
                    {
                        m_timeToWallUnstick = parent.wallStickTime;
                    }
                }
                else
                {
                    m_timeToWallUnstick = parent.wallStickTime;
                }
            }

            //Reset vertical velocity to 0 when on the ground or touching the ceiling
            if (isGrounded)
            {
                m_velocity.y = 0;
            }


            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_animator.SetBool("isJumping", true);

                if (wallSliding)
                {
                    //Jump climb the wall
                    if (parent.wallJumpActivate)
                    {
                        if (wallDirX == m_input.x)
                        {
                            m_velocity.x = -wallDirX * parent.wallJumpClimb.x;
                            m_velocity.y = parent.wallJumpClimb.y;
                        }
                        else
                        {
                            m_velocity.x = -wallDirX * parent.wallLeap.x;
                            m_velocity.y = parent.wallLeap.y;
                        }
                    }
                    //Jump off the wall
                    if (m_input.x == 0)
                    {
                        m_velocity.x = -wallDirX * parent.wallJumpOff.x;
                        m_velocity.y = (parent.wallJumpActivate) ? parent.wallJumpClimb.y : 0;
                    }
                    //Leap between two walls
                }
                if (isGrounded)
                {
                    audioManager.playAudioClip("Jump");
                    m_velocity.y = parent.maxJumpVelocity;
                    m_isJumping = true;
                }
            }

            /*during jumping, if we release the space bar before player reaches the max height
            we "terminate" the jump early by setting velocity.y to a small level*/
            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (m_isJumping)
                {
                    if (m_velocity.y > parent.minJumpVelocity)
                    {
                        m_velocity.y = parent.minJumpVelocity;
                    }
                }
                m_isJumping = false;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (((!inverseGravity) && (parent.characterXGap > 0))//normal character on right.
                    || ((inverseGravity) && (parent.characterXGap < 0))) //flipped character on right.
                {
                    m_centerDashDir = -1;
                }
                else if (((!inverseGravity) && (parent.characterXGap < 0))//normal character on left.
                    || ((inverseGravity) && (parent.characterXGap > 0)))//flipped character on left.
                {
                    m_centerDashDir = 1;
                }
                else
                {
                    m_centerDashDir = 0;
                }

                //velocity.x = 0;
            }

            if ((state == PlayerInputParent.PlayerState.CenterDash) && (m_controller.collisionInfo.left || m_controller.collisionInfo.right))
            {
                m_velocity.x = 0;
                parent.state = PlayerInputParent.PlayerState.Normal;
            }

            switch (state)
            {
                /*case PlayerInputParent.PlayerState.CenterDash:
                    velocity.x = Mathf.SmoothDamp(velocity.x, centerDashDir * parent.centerDashVelocityX, ref velocityXSmoothing, parent.xAccelerationTimeAir);
                    float DisplacementX = velocity.x * Time.deltaTime;
                    float nextPos = transform.position.x + DisplacementX;
                    if (centerDashDir * (nextPos - parent.characterCenter.x) > 0)
                    {
                        Debug.Log("Trigger");
                        displacement.x = parent.characterCenter.x - transform.position.x;
                        displacement.y = 0;
                    }
                    else
                    {
                        displacement.x = DisplacementX;
                        displacement.y = 0;
                    }
                    break;*/

                default:
                    //velocity.x = input.x * moveSpeed;
                    displacement.x = m_velocity.x * Time.deltaTime;
                    float yInitialVelocity = m_velocity.y;
                    m_gravity = (wallSliding) ? (parent.wallSlideGravBuffer * parent.gravity) : parent.gravity;
                    m_velocity.y += m_gravity * Time.deltaTime;
                    displacement.y = (Mathf.Pow(m_velocity.y, 2) - Mathf.Pow(yInitialVelocity, 2)) / (2 * m_gravity);
                    break;
            }

            m_controller.Move(displacement, false, false, true);
        }

        public Vector2 getInput()
        {
            return this.m_input;
        }
    }
}
