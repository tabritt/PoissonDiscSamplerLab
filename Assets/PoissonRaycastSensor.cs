using UnityEngine;
using System.Collections.Generic;

public class PoissonRaycastSensor : MonoBehaviour
{
    public float scanRadius = 10f;
    public float traceDistBetween = 1.5f; // distance between line traces
    public float scanInterval = 0.5f;
    public LayerMask wallMask; //this will be for the Walls to hide
    public LayerMask avoideeMask; //this will be for the Running away from object
    public LayerMask safePointMask;

    private float nextScanTime = 0f;

    void Update()
    {
        if (Time.time >= nextScanTime) //Does them in intervals so it doesn't MEGA spam it
        {
            Scan();
            nextScanTime = Time.time + scanInterval;
        }
    }

    void Scan()
    {
        PoissonDiscSampler sampler = new PoissonDiscSampler(scanRadius * 2, scanRadius * 2, traceDistBetween); //grabs from the given poissonDisc the professor gave us

        Vector3 origin = transform.position; //sets on game object position

        foreach (Vector2 sample in sampler.Samples()) //for each sample. Shoot out a same radius with spacings and a 10f radius around the object.
                                                      // if the scanned object is Avoidee debugs run away
                                                      // if the scanned object is wallMask can hide here
                                                      //Else is green line trace
        {
            Vector2 offset = sample - new Vector2(scanRadius, scanRadius);
            if (offset.magnitude > scanRadius) continue;

            Vector3 direction = new Vector3(offset.x, 0f, offset.y).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, scanRadius))
            {
                Debug.DrawLine(origin, hit.point, Color.red, scanInterval);

                if (((1 << hit.collider.gameObject.layer) & avoideeMask) != 0)
                {
                    Debug.DrawLine(origin, hit.point, Color.red, scanInterval);
                    Debug.Log("Run away!");
                }
                if (((1 << hit.collider.gameObject.layer) & wallMask) != 0)
                {
                    Debug.DrawLine(origin, hit.point, Color.yellow, scanInterval);
                    Debug.Log("can hide here!");
                }
                if (((1 << hit.collider.gameObject.layer) & safePointMask) != 0)
                {
                    Debug.DrawLine(origin, hit.point, Color.blue, scanInterval);
                    Debug.Log("I'm safe!");
                }
            }
            else
            {
                Debug.DrawRay(origin, direction * scanRadius, Color.green, scanInterval);
            }
        }
    }
}
