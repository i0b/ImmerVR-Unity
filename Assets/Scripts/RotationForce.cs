using UnityEngine;

public class RotationForce : MonoBehaviour {
    public float speed;
    
    void FixedUpdate () {
        //Vector3 pos = this.transform.position;
        //float angle = Mathf.Atan(pos.z/pos.x);
        //this.gameObject.GetComponent<Rigidbody>().AddForce(speed * Mathf.Sin(angle) * Time.deltaTime, speed * Mathf.Cos(angle) * Time.deltaTime, 0);

        this.transform.RotateAround(Vector3.zero, Vector3.up, speed * Time.deltaTime);
    }
}
