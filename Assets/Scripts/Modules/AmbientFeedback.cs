using System;
using System.Collections.Generic;
using UnityEngine;

public class AmbientFeedback : MonoBehaviour {
    public float trackingDistance;
    public float actuationUpdateIntervalMs;
    public Communication communication;

    private Dictionary<int, Module> modulesById;
    private Dictionary<ActuatorType, int> moduleIdByType;
    private float nextActuationUpdate;
    private Dictionary<GameObject, ActuatorType[]> trackedObjects;

    private void InitTrackedObjects() {
        if(trackedObjects == null) {
            trackedObjects = new Dictionary<GameObject, ActuatorType[]>();
        }
    }

    public void AddTrackedObject(GameObject gameObject, ActuatorType[] actuatorTypes) {
        InitTrackedObjects();
        trackedObjects.Add(gameObject, actuatorTypes);
    }

    private void RemoveDestroyedTrackedObjects() {
        foreach (GameObject trackedObject in trackedObjects.Keys) {
            if (trackedObject == null) {
                trackedObjects.Remove(trackedObject);
            }
        }
    }

    private bool ObjectInTrackingDistance(GameObject trackedObject) {
        return Vector3.Magnitude(trackedObject.transform.position - transform.position) < trackingDistance;
    }

    void Start () {
        modulesById = new Dictionary<int, Module>();
        moduleIdByType = new Dictionary<ActuatorType, int>();
        //trackedObjects = new Dictionary<GameObject, ActuatorType[]>();
        InitTrackedObjects();

        Module[] modules = FindObjectsOfType(typeof(Module)) as Module[];

        foreach (Module module in modules) {
            modulesById.Add(module.ID, module);
            moduleIdByType.Add(module.actuatorType, module.ID);
        }

        nextActuationUpdate = 0f;
    }

    void Update () {
        nextActuationUpdate += Time.deltaTime;
        if (nextActuationUpdate > actuationUpdateIntervalMs/1000) {
            RemoveDestroyedTrackedObjects();

            foreach (KeyValuePair<GameObject, ActuatorType[]> entry in trackedObjects) {
                GameObject trackedObject = entry.Key;
                ActuatorType[] actuatorTypes = entry.Value;

                if (ObjectInTrackingDistance(trackedObject)) {
                    foreach (ActuatorType actuatorType in actuatorTypes) {
                        switch (actuatorType) {
                            case ActuatorType.Haptic:
                                ActuateHaptic(trackedObject, moduleIdByType[actuatorType]);
                                break;
                            case ActuatorType.Temperature:
                                ActuateTemperature(trackedObject, moduleIdByType[actuatorType]);
                                break;
                            case ActuatorType.EMS:
                                ActuateEMS(trackedObject, moduleIdByType[actuatorType]);
                                break;
                        }
                    }
                }
            }

            nextActuationUpdate = 0f;
        }
    }

    private GameObject[] TrackedObjectsByType(ActuatorType filterActuatorType) {
        List<GameObject> filteredObjects = new List<GameObject>();

        foreach (KeyValuePair<GameObject, ActuatorType[]> entry in trackedObjects) {
            foreach (ActuatorType actuatorType in entry.Value) {
                if (actuatorType == filterActuatorType) {
                    filteredObjects.Add(entry.Key);
                    break;
                }
            }
        }

        return filteredObjects.ToArray();
    }

    private void ActuateHaptic(GameObject trackedObject, int moduleId) {
        /*
        RaycastHit hit;
        Vector3 direction = trackedObject.transform.position;
        if (Physics.Raycast(Vector3.zero, direction, out hit) && hit.collider.gameObject == trackedObject) {
        */
            HapticAttributes hapticAttributes = trackedObject.GetComponent<HapticAttributes>();

            //calculate force for each element, then send values
            float mass_head = this.GetComponent<HapticAttributes>().Mass;
            float mass_trackedObject = hapticAttributes.Mass;

            Module module = modulesById[moduleId];
            Vector3[] actuatorPositions = module.getPositionOfActuators();
            int[] values = new int[module.count()];

            for (int actuator = 0; actuator < module.count(); actuator++) {
                float distance = Vector3.Magnitude(trackedObject.transform.position - actuatorPositions[actuator]);

                values[actuator] = (int)((mass_head * mass_trackedObject) / (distance * distance));
            }

            communication.QueueValues(moduleId, values);
        /*
        }
        */
    }

    private void ActuateTemperature(GameObject trackedObject, int moduleId) {
        /*
        RaycastHit hit;
        Vector3 direction = trackedObject.transform.position - transform.position;
        if (Physics.Raycast(transform.position, direction, out hit) && hit.collider.gameObject == trackedObject) {
        */
            int[] t_actuators = modulesById[moduleId].getReadValues();
            Module module = modulesById[moduleId];
            int[] values = new int[module.count()];

            // calculate Q_total = Q_room + Sum(Q_element.i)

            // Q_room      = epsilon_skin * sigma * A_skin * (t_skin_actuator^4 - t_room^4);
            // Q_element.i = (sigma * (t_skin_actuator^4 - t_trackedObject^4)) / ((1-epsilon_skin)/(A_skin*epsilon_skin) + 1/(A_skin*F_head_trackedObject) + (1-epsilon_trackedObject)/(A_trackedObject*epsilon_trackedObject))
            // F_head_trackedObject = 1/(Math.PI*distance_skin.trackedObject^2) * (A_projected_on_skin * A_projected_on_trackedObject)/A_skin
            int temperatureModuleId = moduleIdByType[ActuatorType.Temperature];

            int minTemperature = modulesById[temperatureModuleId].MinValue;
            int maxTemperature = modulesById[temperatureModuleId].MaxValue;

            double[][] Qtotals = new double[modulesById[temperatureModuleId].count()][];

            float epsilon_skin = 0.98f;
            double sigma = 5.67 * Math.Pow(10,-8);
            float A_skin = 20;
            float t_room = 20;

            for (int actuator = 0; actuator < modulesById[moduleId].count(); actuator++) {
                Qtotals[actuator] = new double[maxTemperature - minTemperature + 1];
                double QtotalsMin = Double.MaxValue;

                for (int temperature = minTemperature; temperature <= maxTemperature; temperature++) {
                    double Q_room = epsilon_skin * sigma * A_skin * (Math.Pow(temperature, 4) - Math.Pow(t_room, 4));

                    double Q_elements = 0;
                
                    foreach (GameObject temperatureSource in TrackedObjectsByType(ActuatorType.Temperature)) {
                        // F_head_trackedObject = 1/(Math.PI*distance_skin.trackedObject^2) * (A_projected_on_skin * A_projected_on_trackedObject)/A_skin

                        float t_temperatureSource = temperatureSource.GetComponent<TemperatureAttributes>().Temperature;
                        float epsilon_temperatureSource = temperatureSource.GetComponent<TemperatureAttributes>().Epsilon;

                        //double A_temperatureSource = Math.Pow(temperatureSource.GetComponent<SphereCollider>().radius, 2) * Math.PI;
                        double A_temperatureSource = Math.Pow(temperatureSource.transform.localScale.x/2, 2) * Math.PI;


                        float distance = Vector3.Magnitude(temperatureSource.transform.position - module.actuators[actuator].transform.position);
                        float F_actuator_temperatureSource = A_skin * (float)A_temperatureSource * 1/(distance*distance);

                        Q_elements += (sigma * (Math.Pow(temperature, 4) - Math.Pow(t_temperatureSource, 4))) /
                                      ((1 - epsilon_skin) / (A_skin * epsilon_skin) + 1 / (A_skin * F_actuator_temperatureSource) + (1 - epsilon_temperatureSource) / (A_temperatureSource * epsilon_temperatureSource));
                    }
                

                    if (Math.Abs(Q_room + Q_elements) < Math.Abs(QtotalsMin)) {
                        values[actuator] = temperature;
                        QtotalsMin = Q_room + Q_elements;
                    }

                }
            }

        string s = "";
        for (int i = 0; i < values.Length; i++)
            s += values[i] + " ";

        Debug.Log(s);

            communication.QueueValues(moduleId, values);
        //}
    }

    private void ActuateEMS(GameObject trackedObject, int moduleId) {
        RaycastHit hit;
        Vector3 direction = trackedObject.transform.position - transform.position;
        if (Physics.Raycast(transform.position, direction, out hit) && hit.collider.gameObject == trackedObject) {
        }
    }
}
