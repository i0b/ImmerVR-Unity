using UnityEngine;

public class AddCollidingProjectiles : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject head;
    public KeyboardControl keyboardControl;
    public int Mass;
    public int TemperatureOnImpactPosition;
    public int EMSOnImpactPosition;

    public float spawnInterval;
    public float distance;
    public float offset;
    private float nextSpawnTime = 0f;


    void CreateProjectile()
    {
        if (Time.time > nextSpawnTime)
        {
            nextSpawnTime = nextSpawnTime + spawnInterval;

            Vector3 position = head.transform.forward;

            position.x = Random.Range(-offset, offset) + position.x * distance;
            position.y = Random.Range(-offset, offset) + position.y * distance;
            position.z = Random.Range(-offset, offset) + position.z * distance;
            
            GameObject newProjectile = Instantiate(
                projectilePrefab,
                position,
                Quaternion.identity
            );

            //newProjectile.GetComponent<Rigidbody>().mass = keyboardControl.mass;
            newProjectile.GetComponent<HapticAttributes>().Mass = Mass;
            newProjectile.GetComponent<TemperatureAttributes>().ExpectedTemperature = TemperatureOnImpactPosition;
            newProjectile.GetComponent<EMSAttributes>().ExpectedEMS = EMSOnImpactPosition;

            //newProjectile.GetComponent<TemperatureAttributes>().ExpectedTemperature = ExpTemperatureBaseline+Random.Range(-ExpTemperatureVariance, ExpTemperatureVariance);

            //Debug.Log("now tracking " + trackableObjects.Count + " elements.");
        }
    }

    void FixedUpdate()
    {
        CreateProjectile();
    }
}
