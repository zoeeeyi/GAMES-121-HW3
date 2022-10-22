using CustomPlatformerPhysics2D;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("General")]
    [Range(0, 1)]
    [SerializeField] float m_cameraSizeSmoothTime = 0.5f;
    [SerializeField] float m_cameraSizeMult = 0.5f;
    float m_cameraStartSize;
    float m_cameraTargetSize; //Should be used according to speed
    float m_cameraSizeSmoothV = 0;
    Camera m_camera;
    GameManager m_gameManager;

    [Header("Set target")]
    [SerializeField] PlayerController m_target;
    PlayerInput m_playerInput;
    Collider2D m_targetCollider;
    float m_targetMaxYVelocity;

    [Header("Focus area settings")]
    [SerializeField] Vector2 m_focusAreaSize = new Vector2(2, 2);
    [Range(0, 1)]
    [SerializeField] float m_focusPosSmoothTime = 1;
    [Range(0, 1)]
    [SerializeField] float m_focusAreaRecenterTime;
    float m_focusPosSmoothX = 0;
    float m_focusPosSmoothY = 0;
    Vector2 m_focusAreaRecenterSmoothV;
    st_FocusArea m_focusArea;

    [Header("Look ahead setting")]
    [SerializeField] float m_lookAheadDistant = 0.1f;
    [Range(0, 1)]
    [SerializeField] float m_lookAheadSmoothTime = 0.5f;
    Vector2 m_lookAheadDir;
    Vector2 m_lookAheadTarget;
    Vector2 m_currentLookAhead;
    Vector2 m_lookAheadTargetClearSmoothV;
    float m_lookAheadSmoothX = 0;
    float m_lookAheadSmoothY = 0;
    bool m_lookAheadStopped = true;

    bool m_cameraInitialized = false;

    void Start()
    {
        //Initialize camera position and size
        m_camera = GetComponent<Camera>();

        //Set focus area
        m_targetCollider = m_target.gameObject.GetComponent<Collider2D>();
        m_focusArea = new st_FocusArea(m_targetCollider.bounds, m_focusAreaSize);

        //Set target parameters
        m_gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        m_targetMaxYVelocity = m_gameManager.GetMaxAllowedYVelocity();
        m_playerInput = m_target.gameObject.GetComponent<PlayerInput>();
        transform.position = m_target.transform.position + Vector3.back * 10;
    }

    private void Update()
    {
        if (!m_cameraInitialized)
        {
            float _xInput = Input.GetAxis("Horizontal");
            float _yInput = Input.GetAxis("Vertical");
            if (_xInput != 0 || _yInput != 0)
            {
                m_cameraStartSize = m_camera.orthographicSize;
                m_cameraInitialized = true;
                m_gameManager.SetGameStart();
            }
        }
    }

    void LateUpdate()
    {
        //If player haven't pressed any button, camera should not update
        if (!m_cameraInitialized) { return; }

        m_focusArea.Update(m_targetCollider.bounds);

        //Set look ahead properties
        if (m_focusArea.velocity.magnitude != 0)
        {
            m_lookAheadDir.x = Mathf.Sign(m_focusArea.velocity.x);
            m_lookAheadDir.y = Mathf.Sign(m_focusArea.velocity.y);

            bool _shouldWeLookAhead = false;
            if (Mathf.Sign(m_playerInput.getInput().x) == m_lookAheadDir.x && m_playerInput.getInput().x != 0)
            {
                _shouldWeLookAhead = true;
                m_lookAheadStopped = false;
                m_lookAheadTarget.x = m_lookAheadDir.x * m_lookAheadDistant;
            }
            if (Mathf.Sign(m_target.GetLastDisplacement().y) == m_lookAheadDir.y && m_target.GetLastDisplacement().y != 0)
            {
                _shouldWeLookAhead = true;
                m_lookAheadStopped = false;
                m_lookAheadTarget.y = m_lookAheadDir.y * m_lookAheadDistant;
            }
            if (!_shouldWeLookAhead)
            {
                if (m_lookAheadStopped == false)
                {
                    m_lookAheadTarget = m_currentLookAhead + (m_lookAheadDir * m_lookAheadDistant - m_currentLookAhead) / 4;
                    m_lookAheadStopped = true;
                }
            }
        }

        //Recenter camera
        if (m_playerInput.getInput() == Vector2.zero)
        {
            Vector2 _newTargetCenter = m_targetCollider.bounds.center;
            if (m_focusArea.center != _newTargetCenter)
            {
                m_focusArea.center = Vector2.SmoothDamp(m_focusArea.center, _newTargetCenter, ref m_focusAreaRecenterSmoothV, m_focusAreaRecenterTime);
                m_focusArea.Recenter();
            }
            m_lookAheadTarget = Vector2.SmoothDamp(m_lookAheadTarget, Vector2.zero, ref m_lookAheadTargetClearSmoothV, m_focusAreaRecenterTime);
        }

        m_currentLookAhead.x = Mathf.SmoothDamp(m_currentLookAhead.x, m_lookAheadTarget.x, ref m_lookAheadSmoothX, m_lookAheadSmoothTime);
        m_currentLookAhead.y = Mathf.SmoothDamp(m_currentLookAhead.y, m_lookAheadTarget.y, ref m_lookAheadSmoothY, m_lookAheadSmoothTime);

        //calculate new position
        Vector2 _newFocusPosition;
        _newFocusPosition = m_focusArea.center;
        _newFocusPosition.y += m_currentLookAhead.y;

        _newFocusPosition.x = Mathf.SmoothDamp(transform.position.x, _newFocusPosition.x, ref m_focusPosSmoothX, m_focusPosSmoothTime);
        _newFocusPosition.y = Mathf.SmoothDamp(transform.position.y, _newFocusPosition.y, ref m_focusPosSmoothY, m_focusPosSmoothTime);

        //Set camera position
        transform.position = (Vector3)_newFocusPosition + Vector3.back * 10;

        //Set camera size
        m_cameraTargetSize = m_cameraStartSize * (1 + m_cameraSizeMult * (Mathf.Abs(m_target.GetLastDisplacement().y) / m_gameManager.GetMaxAllowedYVelocity()));
        m_camera.orthographicSize = Mathf.SmoothDamp(m_camera.orthographicSize, m_cameraTargetSize, ref m_cameraSizeSmoothV, m_cameraSizeSmoothTime);

    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(m_focusArea.center, m_focusArea.size);
    }

    struct st_FocusArea
    {
        public Vector2 center;
        public Vector2 size;
        public Vector2 velocity;
        Vector2 focusAreaSize;
        float left, right;
        float top, bottom;

        public st_FocusArea(Bounds _targetBounds, Vector2 _size)
        {
            focusAreaSize = _size;

            left = _targetBounds.center.x - focusAreaSize.x;
            right = _targetBounds.center.x + focusAreaSize.x;
            bottom = _targetBounds.center.y - focusAreaSize.y;
            top = _targetBounds.center.y + focusAreaSize.y;

            velocity = Vector2.zero;
            center = new Vector2((left + right) / 2, (top + bottom) / 2);
            size = new Vector2(right - left, top - bottom);
        }

        public void Update(Bounds _targetBounds)
        {
            float _shiftX = 0;
            if (_targetBounds.min.x < left)
            {
                _shiftX = _targetBounds.min.x - left;
            }
            else if (_targetBounds.max.x > right)
            {
                _shiftX = _targetBounds.max.x - right;
            }
            left += _shiftX;
            right += _shiftX;

            float _shiftY = 0;
            if (_targetBounds.min.y < bottom)
            {
                _shiftY = _targetBounds.min.y - bottom;
            }
            else if (_targetBounds.max.y > top)
            {
                _shiftY = _targetBounds.max.y - top;
            }
            top += _shiftY;
            bottom += _shiftY;
            center = new Vector2((left + right) / 2, (top + bottom) / 2);
            velocity = new Vector2(_shiftX, _shiftY);
        }

        public void Recenter()
        {
            left = center.x - focusAreaSize.x;
            right = center.x + focusAreaSize.x;
            top = center.y + focusAreaSize.y;
            bottom = center.y - focusAreaSize.y;
        }
    }
}
