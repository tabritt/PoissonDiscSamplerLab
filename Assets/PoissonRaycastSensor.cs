using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class PoissonRaycastSensor : MonoBehaviour
{
    [Header("Scan Settings")]
    public float scanRadius = 10f;
    public float minDistanceBetweenRays = 1.5f;   // Minimum spacing for ray samples
    public float scanInterval = 0.5f;
    public LayerMask wallMask;              // Layers that block line of sight
    public LayerMask avoideeMask;           // Layers for what we're avoiding

    [Header("Target to Avoid")]
    public Transform avoidee;
    public float eyeHeight = 1.6f;          // Height from the ground for line of sight checks

    [Header("Movement Settings")]
    public NavMeshAgent agent;
    public float standOffFromWall = 0.6f;   // How far to step back from walls
    public float repathCooldown = 0.75f;    // so it doesn't have to think so often
    public float navSampleRadius = 1.0f;    // Radius for projecting points onto the NavMesh

    float nextScanTime = 0f;
    float nextRepathTime = 0f;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Time.time >= nextScanTime)
        {
            ScanAndMove();
            nextScanTime = Time.time + scanInterval;
        }
    }

    void ScanAndMove()
    {
        if (avoidee == null || agent == null)
            return;

        var sampler = new PoissonDiscSampler(scanRadius * 2f, scanRadius * 2f, minDistanceBetweenRays);
        Vector3 origin = transform.position;

        Vector3? bestSpot = null;
        float bestScore = float.NegativeInfinity;

        foreach (Vector2 sample in sampler.Samples())
        {
            Vector2 offset = sample - new Vector2(scanRadius, scanRadius);
            if (offset.magnitude > scanRadius)
                continue;

            Vector3 direction = new Vector3(offset.x, 0f, offset.y).normalized;

            bool hitSomething = Physics.Raycast(origin + Vector3.up * 0.2f, direction, out RaycastHit hit, scanRadius,
                                               wallMask | avoideeMask, QueryTriggerInteraction.Ignore);

            Vector3 objPos = hitSomething
                ? hit.point - direction * standOffFromWall
                : origin + direction * scanRadius;

            if (!NavMesh.SamplePosition(objPos, out NavMeshHit navHit, navSampleRadius, NavMesh.AllAreas))
                continue;

            Vector3 navPos = navHit.position;

            bool hasCover = HasCoverFromAvoidee(navPos);

            // Line traces so to better see what we doing
            if (hitSomething)
            {
                Color color = Color.green;
                int layer = hit.collider.gameObject.layer;
                if ((avoideeMask.value & (1 << layer)) != 0) color = Color.red;
                else if ((wallMask.value & (1 << layer)) != 0) color = Color.yellow;

                Debug.DrawLine(origin, hit.point, color, scanInterval);
            }
            else
            {
                Debug.DrawRay(origin, direction * scanRadius, Color.green, scanInterval);
            }

            float distToAvoidee = Vector3.Distance(navPos, GetAvoideeEyePosition());
            float coverBonus = hasCover ? 1000f : 0f;   
            float score = coverBonus + distToAvoidee * 2f;

            if (hitSomething && IsAvoidee(hit.collider))
                score -= 500f;  // if see avoidee we say bad. Similar to EQS in Unreal

            if (score > bestScore)
            {
                bestScore = score;
                bestSpot = navPos;
            }
        }

        
        if (avoidee != null)
        {
            Vector3 from = transform.position + Vector3.up * eyeHeight;
            Vector3 to = GetAvoideeEyePosition();
            Vector3 dir = to - from;
            float dist = dir.magnitude;

            if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, dist, wallMask, QueryTriggerInteraction.Ignore))
            {
                // walkl block green line
                Debug.DrawLine(from, to, Color.green, scanInterval);
            }
            else
            {
                // Red line for no LOS
                Debug.DrawLine(from, to, Color.red, scanInterval);
            }
        }

        if (bestSpot.HasValue && Time.time >= nextRepathTime)
        {
            agent.SetDestination(bestSpot.Value);
            nextRepathTime = Time.time + repathCooldown;
            Debug.DrawRay(bestSpot.Value, Vector3.up * 1.5f, Color.cyan, scanInterval);
        }
    }

    Vector3 GetAvoideeEyePosition()
    {
        return (avoidee != null ? avoidee.position : Vector3.zero) + Vector3.up * eyeHeight;
    }

    bool HasCoverFromAvoidee(Vector3 point)
    {
        if (avoidee == null)
            return false;

        Vector3 from = point + Vector3.up * eyeHeight;
        Vector3 to = GetAvoideeEyePosition();
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.01f)
            return false;

        // something block have cover
        return Physics.Raycast(from, dir.normalized, dist, wallMask, QueryTriggerInteraction.Ignore);
    }

    bool IsWall(Collider col) => ((1 << col.gameObject.layer) & wallMask.value) != 0;
    bool IsAvoidee(Collider col) => ((1 << col.gameObject.layer) & avoideeMask.value) != 0;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, scanRadius);
        // Removed drawing line to avoidee here for less clutter in editor
    }
#endif
}
