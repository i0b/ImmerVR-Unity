using UnityEngine;

public class MoveToHead : MonoBehaviour {
    public float speed;
    //private GameObject target;
    private Rigidbody rb;

    void Start()
    {
        //target = GameObject.FindGameObjectWithTag("head");
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //float angle = Mathf.Atan(pos.z/pos.x);
        //this.gameObject.GetComponent<Rigidbody>().AddForce(speed * Mathf.Sin(angle) * Time.deltaTime, speed * Mathf.Cos(angle) * Time.deltaTime, 0);

        //Vector3 targetPoint = target.transform.position;

        Vector3 targetPoint = Vector3.zero;
        //TODO CHANGE
        Vector3 direction = targetPoint - this.transform.position;
        Vector3.Normalize(direction);
        //this.transform.Translate(direction * speed * Time.deltaTime);
        //Debug.DrawRay(this.transform.position, Vector3.zero, Color.black);
        rb.AddRelativeForce(direction * speed);
    }
}