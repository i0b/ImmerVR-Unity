using UnityEngine;

public enum ActuatorType {
    Haptic, Temperature, EMS
}
public class Module : MonoBehaviour {
    public int ID;
    public ActuatorType actuatorType;
    public int MinValue;
    public int MaxValue;
    public GameObject[] actuators;

    private int[] readValues;
    private double[] measuredValues;
    private string actuatorTag;

    void Start () {
        readValues = new int[actuators.Length];
        measuredValues = new double[actuators.Length];
    }
	
    public Vector3[] getPositionOfActuators() {
        Vector3[] positions = new Vector3[actuators.Length];

        for (int actuator = 0; actuator < actuators.Length; actuator++) {
            positions[actuator] = actuators[actuator].transform.position;
        }

        return positions;
    }

    public int[] getReadValues() {
        return readValues;
    }
    public void setReadValues(int[] readValues) {
        this.readValues = readValues;
    }

    public double[] getMeasuredValues() {
        return measuredValues;
    }
    public void setMeasuredValues(double[] measuredValues) {
        this.measuredValues = measuredValues;
    }

    public int count() {
        return actuators.Length;
    }

    public GameObject[] getActuators() {
        return actuators;
    }
}