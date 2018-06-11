using UnityEngine;

public class AddCollidingProjectiles : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject head;
    public KeyboardControl keyboardControl;
    public int Mass;
    public int TemperatureOnImpactPosition;
    public int EMSOnImpactPosition;
    public Color ProjectileColor;

    public float spawnInterval;
    public float distance;
    public float offset;
    private float nextSpawnTime;

    private void Start()
    {
        nextSpawnTime = Time.time;
    }

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
            if (Mass == 0)
            {
                Destroy(newProjectile.GetComponent<HapticAttributes>());
            }
            else
            {
                newProjectile.GetComponent<HapticAttributes>().Mass = Mass;
            }

            if (TemperatureOnImpactPosition == 0)
            {
                Destroy(newProjectile.GetComponent<TemperatureAttributes>());
            }
            else
            {
                newProjectile.GetComponent<TemperatureAttributes>().ExpectedTemperature = TemperatureOnImpactPosition;
            }

            if (EMSOnImpactPosition == 0)
            {
                Destroy(newProjectile.GetComponent<EMSAttributes>());
            }
            else
            {
                newProjectile.GetComponent<EMSAttributes>().ExpectedEMS = EMSOnImpactPosition;
            }
            
            //newProjectile.GetComponent<Material>().color = ProjectileColor;
            Renderer renderer = newProjectile.GetComponent<Renderer>();
            renderer.material.color = ProjectileColor;

            //newProjectile.GetComponent<TemperatureAttributes>().ExpectedTemperature = ExpTemperatureBaseline+Random.Range(-ExpTemperatureVariance, ExpTemperatureVariance);

            //Debug.Log("now tracking " + trackableObjects.Count + " elements.");
        }
    }

    void FixedUpdate()
    {
        CreateProjectile();
    }
}
