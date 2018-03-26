using System.Collections.Generic;
using UnityEngine;

public class HapticFeedbackAmbient : MonoBehaviour {
    public float trackingDistance;
    public float actuationUpdateInterval;
    public int hapticModuleId;
    public Module module;

    private float nextActuationUpdate = 0f;
    private Dictionary<GameObject,int[]> actuations;
    private int[] values;
    private Communication communication;

    public void Actuate(GameObject feelableObject, int actuatorIndex, float distance) {
        // Gravitational force: F = m*M/r2
        float force = (this.GetComponent<Rigidbody>().mass * feelableObject.GetComponent<Rigidbody>().mass) / (distance * distance);
        int newValueAtIndex = (int)force;

        if (actuations.ContainsKey(feelableObject)) {
            int[] values = actuations[feelableObject];
            values[actuatorIndex] = newValueAtIndex;
            actuations[feelableObject] = values;
        }
        else {
            int[] values = new int[module.count()];
            values[actuatorIndex] = newValueAtIndex;
            actuations.Add(feelableObject, values);
        }
    }
    
    private void Start() {
        actuations = new Dictionary<GameObject, int[]>();
        communication = GameObject.FindGameObjectWithTag("head").GetComponent<Communication>();

        // communication.numberActuators[0].Length
        values = new int[module.count()];
}

    void FixedUpdate() {
        if(Time.time > nextActuationUpdate)
        {
            nextActuationUpdate = nextActuationUpdate + actuationUpdateInterval;

            int[] valueSums = new int[module.count()];

            foreach (KeyValuePair<GameObject, int[]> kvp in actuations) {
                for (int i = 0; i < kvp.Value.Length; i++) {
                    valueSums[i] = (valueSums[i] + kvp.Value[i]);
                }
            }

            actuations.Clear();
            
            for (int i = 0; i < module.count(); i++) {
                if (values[i] != valueSums[i]) {
                    communication.QueueValues(hapticModuleId, values);
                    values = valueSums;
                    break;
                }
            }

        }
    }
}
