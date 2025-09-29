using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class PoissonRaycastSensor1 : MonoBehaviour
{
    [Header("Scan")]
    public float scanRadius = 10f;
    public float traceDistBetween = 1.5f;   // Poisson min spacing
    public float scanInterval = 0.5f;
    public LayerMask wallMask;              // geometry that blocks line of sight
    public LayerMask avoideeMask;           // layer(s) of the thing we run from
    // public LayerMask safePointMask;      // (not used anymore)

    [Header("Target to Avoid")]
    public Transform avoidee;
    public float eyeHeight = 1.6f;          // eye line for LOS checks

    [Header("Movement")]
    public NavMeshAgent agent;
    public float standOffFromWall = 0.6f;   // step back from a wall hit
    public float repathCooldown = 0.75f;    // don’t spam SetDestination
    public float navSampleRadius = 1.0f;    // how far to project to NavMesh

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
        if (avoidee == null || agent == null) return;

        var sampler = new PoissonDiscSampler(scanRadius * 2f, scanRadius * 2f, traceDistBetween);
        Vector3 origin = transform.position;

        Vector3? bestPoint = null;
        float bestScore = float.NegativeInfinity;

        foreach (Vector2 sample in sampler.Samples())
        {
            Vector2 offset = sample - new Vector2(scanRadius, scanRadius);
            if (offset.magnitude > scanRadius) continue;

            Vector3 dir = new Vector3(offset.x, 0f, offset.y).normalized;

            // Ray from origin in dir up to scanRadius
            bool hitSomething = Physics.Raycast(origin + Vector3.up * 0.2f, dir, out RaycastHit hit, scanRadius,
                                                wallMask | avoideeMask, QueryTriggerInteraction.Ignore);

            Vector3 candidate = hitSomething
                ? hit.point - dir * standOffFromWall
                : origin + dir * scanRadius;

            // Snap to NavMesh
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, navSampleRadius, NavMesh.AllAreas))
                continue;

            Vector3 navPos = navHit.position;

            // Check if LOS to avoidee is blocked by wall
            bool hasCover = HasCoverFromAvoidee(navPos);

            float distToAvoidee = Vector3.Distance(navPos, AvoideeEye());
            float coverBonus = hasCover ? 1000f : 0f;   // strong preference for cover
            float score = coverBonus + distToAvoidee * 2f;

            // (Optional) penalize direct avoidee hits
            if (hitSomething && IsAvoidee(hit.collider))
                score -= 500f;

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = navPos;
            }

            // Debug
            if (hitSomething)
            {
                Color c = Color.green;
                int layer = hit.collider.gameObject.layer;
                if ((avoideeMask.value & (1 << layer)) != 0) c = Color.red;
                else if ((wallMask.value & (1 << layer)) != 0) c = Color.yellow;
                // else if ((safePointMask.value & (1 << layer)) != 0) c = Color.blue; // (commented out)

                Debug.DrawLine(origin, hit.point, c, scanInterval);
                Debug.DrawLine(hit.point, candidate, Color.white, scanInterval);
                Debug.DrawRay(navPos + Vector3.up * 0.05f, Vector3.up * 0.5f, c, scanInterval);
            }
            else
            {
                Debug.DrawRay(origin, dir * scanRadius, Color.green, scanInterval);
                Debug.DrawRay(navPos + Vector3.up * 0.05f, Vector3.up * 0.5f, Color.green, scanInterval);
            }
        }

        if (bestPoint.HasValue && Time.time >= nextRepathTime)
        {
            agent.SetDestination(bestPoint.Value);
            nextRepathTime = Time.time + repathCooldown;
            Debug.DrawRay(bestPoint.Value, Vector3.up * 1.5f, Color.cyan, scanInterval);
        }
    }

    Vector3 AvoideeEye()
    {
        return (avoidee != null ? avoidee.position : Vector3.zero) + Vector3.up * eyeHeight;
    }

    bool HasCoverFromAvoidee(Vector3 candidate)
    {
        if (avoidee == null) return false;

        Vector3 from = candidate + Vector3.up * eyeHeight;
        Vector3 to = AvoideeEye();
        Vector3 dir = (to - from);
        float dist = dir.magnitude;
        if (dist <= 0.01f) return false;

        // If a wall blocks the line, we have cover
        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, dist, wallMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }
        return false;
    }

    bool IsWall(Collider col) => ((1 << col.gameObject.layer) & wallMask.value) != 0;
    bool IsAvoidee(Collider col) => ((1 << col.gameObject.layer) & avoideeMask.value) != 0;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, scanRadius);
        if (avoidee != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, AvoideeEye());
        }
    }
#endif
}
