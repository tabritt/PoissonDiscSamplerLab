using UnityEngine;
using UnityEngine.AI; // Optional: Use if you want NavMeshAgent-based movement

public class AvoideeWalkee : MonoBehaviour
{
    public Transform[] targets;

    public float walkSpeed = 3.5f;

    public float stoppingDistance = 0.1f;

    private int currentTargetIndex = 0;

    private void Update()
    {
        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("AvoideeWalker: No targets assigned.");
            return;
        }

        Transform currentTarget = targets[currentTargetIndex];
        Vector3 direction = currentTarget.position - transform.position;
        float distance = direction.magnitude;

        if (distance > stoppingDistance)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * walkSpeed * Time.deltaTime;

           
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
        else
        {
            // Arrived at current target — move to the next, looping back to 0
            currentTargetIndex = (currentTargetIndex + 1) % targets.Length;
        }
    }
}
