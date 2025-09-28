using UnityEngine;

public class SafepointManager : MonoBehaviour
{
    public bool[] safePoints; //stores the transform for each of the runner's safe points
    public GameObject avoidee;

    
    private void Start()
    {
        PoissonRaycastSensor avoideeSensor = avoidee.GetComponent<PoissonRaycastSensor>();
    }
    // Update is called once per frame
    void Update()
    {
        foreach (bool waypoint in safePoints)
        {

        }
    }
}
