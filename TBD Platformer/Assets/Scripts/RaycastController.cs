using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour
{
    [Header("Raycast Controller Settings")]
    [SerializeField] protected LayerMask m_xCollisionMask;
    [SerializeField] protected LayerMask m_yCollisionMask;
    [SerializeField] protected int m_horizontalRayCount = 4;
    [SerializeField] protected int m_verticalRayCount = 4;

    protected const float m_skinWidth = 0.015f;
    protected float horizontalRaySpacing;
    protected float verticalRaySpacing;
    protected BoxCollider2D m_collider;
    protected RaycastOrigins raycastOrigins;

    protected virtual void Start()
    {
        CalculateRaySpacing();
    }

    protected void UpdateRaycastOrigins()
    {
        Bounds bounds = m_collider.bounds;
        bounds.Expand(m_skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    protected void CalculateRaySpacing()
    {
        Bounds bounds = m_collider.bounds;
        bounds.Expand(m_skinWidth * -2);

        //Set ray counts
        m_horizontalRayCount = Mathf.Clamp(m_horizontalRayCount, 2, int.MaxValue);
        m_verticalRayCount = Mathf.Clamp(m_verticalRayCount, 2, int.MaxValue);

        //Set ray spacing
        horizontalRaySpacing = bounds.size.y / (m_horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (m_verticalRayCount - 1);
    }

    protected struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
}
