using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerRopeConstraint : MonoBehaviour
{
    [Header("Rope Settings")]
    public Transform anchorPoint;
    public float ropeLength = 5.0f;
    public bool ropeEnabled = true;
    public LayerMask obstacleLayerMask = -1; // What layers can the rope wrap around
    public float offset = 0.5f;

    [Header("Visual")]
    public bool showRope = true;
    public float ropeWidth = 0.05f;
    public Material ropeMaterial;

    [Header("Debug")]
    public bool showDebugRays = false;

    private CharacterController _controller;
    private LineRenderer _line;
    private List<Vector3> _ropePoints = new List<Vector3>();

    void Start()
    {
        _controller = GetComponent<CharacterController>();

        // Initialize rope points with just the anchor
        if (anchorPoint != null)
        {
            _ropePoints.Add(anchorPoint.position);
        }

        if (showRope)
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.startWidth = ropeWidth;
            _line.endWidth = ropeWidth;
            _line.material = ropeMaterial ?? new Material(Shader.Find("Sprites/Default"));
            _line.useWorldSpace = true;
        }
    }

    void LateUpdate()
    {
        if (!ropeEnabled || anchorPoint == null)
        {
            if (_line != null) _line.enabled = false;
            return;
        }

        UpdateRopePoints();
        EnforceRopeConstraint();
        UpdateVisuals();
    }

    void UpdateRopePoints()
    {
        // Ensure we always have at least the anchor point
        if (_ropePoints.Count == 0)
        {
            _ropePoints.Add(anchorPoint.position);
        }

        // Update anchor position (in case it moves)
        _ropePoints[0] = anchorPoint.position;

        // Get the last point in our rope (the point we're checking from)
        Vector3 lastPoint = _ropePoints[^1];
        Vector3 playerPos = transform.position;

        // Use elevated positions for the raycast to match visual rope height
        Vector3 elevatedLastPoint = lastPoint;
        elevatedLastPoint.y = transform.position.y + 1f;

        Vector3 elevatedPlayerPos = playerPos;
        elevatedPlayerPos.y = transform.position.y + 1f;

        // Check if there's an obstacle between the last point and player at rope height
        RaycastHit hit;
        Vector3 direction = elevatedPlayerPos - elevatedLastPoint;
        float distance = direction.magnitude;

        if (Physics.Linecast(elevatedPlayerPos, elevatedLastPoint, out hit, obstacleLayerMask))
        {
            // There's an obstacle! Add the hit point as a new rope point
            Vector3 hitPoint = hit.point;

            // Add a larger offset along the surface normal to avoid going through objects
            Vector3 localOffset = hit.normal * offset;
            hitPoint += localOffset;
            
            Debug.DrawRay(hit.point, localOffset * 10f, Color.cyan, 1f);

            // Also ensure the point is at the proper rope height
            hitPoint.y = transform.position.y + 1f;

            _ropePoints.Add(hitPoint);

            if (showDebugRays)
            {
                Debug.DrawRay(elevatedLastPoint, direction.normalized * hit.distance, Color.red, 0.1f);
            }
        }
        else
        {
            // No obstacle, try to remove unnecessary points
            RemoveUnnecessaryPoints();

            if (showDebugRays)
            {
                Debug.DrawRay(elevatedLastPoint, direction.normalized * distance, Color.green, 0.1f);
            }
        }
    }

    void RemoveUnnecessaryPoints()
    {
        Vector3 playerPos = transform.position;

        // Check from the end backwards, but keep at least the anchor point
        for (int i = _ropePoints.Count - 1; i >= 1; i--)
        {
            // Can we go directly from the previous point to the player?
            Vector3 previousPoint = _ropePoints[i - 1];

            if (!Physics.Linecast(previousPoint, playerPos, obstacleLayerMask))
            {

                _ropePoints.RemoveAt(i);
            }
            else
            {

                break;
            }
        }
    }

    void EnforceRopeConstraint()
    {
        // Calculate total rope length used at elevated positions
        float totalLength = 0f;
        Vector3 elevatedPlayerPos = transform.position;
        elevatedPlayerPos.y = transform.position.y + 1f;

        // Add up all the segments using elevated positions for consistency
        for (int i = 0; i < _ropePoints.Count - 1; i++)
        {
            Vector3 point1 = _ropePoints[i];
            Vector3 point2 = _ropePoints[i + 1];

            // Elevate both points for consistent measurement
            point1.y = transform.position.y + 1f;
            point2.y = transform.position.y + 1f;

            totalLength += Vector3.Distance(point1, point2);
        }

        // Add the final segment from last rope point to player (both elevated)
        if (_ropePoints.Count > 0)
        {
            Vector3 elevatedLastPoint = _ropePoints[^1];
            elevatedLastPoint.y = transform.position.y + 1f;
            totalLength += Vector3.Distance(elevatedLastPoint, elevatedPlayerPos);
        }

        // If we're exceeding the rope length, pull the player back
        if (totalLength > ropeLength)
        {
            Vector3 lastRopePoint = _ropePoints[^1];
            Vector3 toPlayer = transform.position - lastRopePoint; // Use ground positions for movement
            float lastSegmentLength = toPlayer.magnitude;
            float allowedLastSegmentLength = ropeLength - totalLength + lastSegmentLength;

            if (allowedLastSegmentLength < 0) allowedLastSegmentLength = 0;

            Vector3 constrainedPos = lastRopePoint + toPlayer.normalized * allowedLastSegmentLength;
            Vector3 correction = constrainedPos - transform.position;

            _controller.Move(correction);
        }
    }

    void UpdateVisuals()
    {
        if (!showRope || _line == null) return;

        _line.enabled = true;

        // Set up the line renderer with all rope points + elevated player position
        List<Vector3> allPoints = new List<Vector3>(_ropePoints);

        // Add elevated player position (like in your original code)
        Vector3 elevatedPlayerPos = transform.position;
        elevatedPlayerPos.y = transform.position.y + 1f;
        allPoints.Add(elevatedPlayerPos);

        // Also elevate the anchor point for visual consistency
        if (allPoints.Count > 0)
        {
            Vector3 elevatedAnchor = anchorPoint.position;
            elevatedAnchor.y = transform.position.y + 1f;
            allPoints[0] = elevatedAnchor;
        }

        _line.positionCount = allPoints.Count;
        for (int i = 0; i < allPoints.Count; i++)
        {
            _line.SetPosition(i, allPoints[i]);
        }
    }

    // Helper method to get total rope length for debugging
    public float GetCurrentRopeLength()
    {
        float totalLength = 0f;
        Vector3 playerPos = transform.position;

        for (int i = 0; i < _ropePoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(_ropePoints[i], _ropePoints[i + 1]);
        }

        if (_ropePoints.Count > 0)
        {
            totalLength += Vector3.Distance(_ropePoints[^1], playerPos);
        }

        return totalLength;
    }

    // Reset the rope 
    public void ResetRope()
    {
        _ropePoints.Clear();
        if (anchorPoint != null)
        {
            _ropePoints.Add(anchorPoint.position);
        }
    }

    void OnDrawGizmos()
    {
        if (!ropeEnabled || anchorPoint == null) return;

        // Draw rope points
        Gizmos.color = Color.yellow;
        foreach (Vector3 point in _ropePoints)
        {
            Gizmos.DrawWireSphere(point, 0.1f);
        }

        // Draw rope segments
        Gizmos.color = Color.red;
        for (int i = 0; i < _ropePoints.Count - 1; i++)
        {
            Gizmos.DrawLine(_ropePoints[i], _ropePoints[i + 1]);
        }

        // Draw final segment to player
        if (_ropePoints.Count > 0)
        {
            Gizmos.DrawLine(_ropePoints[^1], transform.position);
        }
    }
}