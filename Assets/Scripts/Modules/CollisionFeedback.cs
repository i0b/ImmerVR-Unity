using UnityEngine;
using System;
using System.Collections.Generic;

public class CollisionFeedback : MonoBehaviour {
    public int decresePerUpdate;
    public float temperatureOnTime;
    public float emsOnTime;
    public float A;
    public float MinFeelableTempDif;

    public float DistanceResolution;

    public Communication communication;
    
    private float nextTemperatureStep;
    private float nextEMSStep;
    private Dictionary<Module, int[]> moduleValues;
    private Dictionary<int, Module> moduleById;
    private Dictionary<ActuatorType, Module> moduleByType;

    private float attenuation(float distance) {
        //given derivative dp/ds => p(s) = A*exp(-s)
        //float attenuation = 0;
        return A*Mathf.Exp(-distance);
        //return 1;
    }

    private int[] NormalizeValues(int moduleId, int[] values)
    {
        Module module = moduleById[moduleId];
        if (module.actuatorType == ActuatorType.Haptic)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > 100)
                {
                    values[i] = 100;
                }
            }
        }

        else if (module.actuatorType == ActuatorType.Temperature)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == 0)
                {
                    continue;
                }
                else if (values[i] < module.MinValue)
                {
                    values[i] = module.MinValue;
                }
                else if (values[i] > module.MaxValue)
                {
                    values[i] = module.MaxValue;
                }
            }
        }
        return values;
    }

    private void QueueValues(int id, int[] values)
    {

        Debug.Log("sending new command");
        NormalizeValues(id, values);
        communication.QueueValues(id, values);
    }

    private void Start() {
        moduleValues = new Dictionary<Module, int[]>();
        moduleById = new Dictionary<int, Module>();
        moduleByType = new Dictionary<ActuatorType, Module>();

        nextTemperatureStep = 0f;
        nextEMSStep = 0f;

        Module[] modules = FindObjectsOfType(typeof(Module)) as Module[];

        foreach (Module module in modules) {
            int[] values = new int[module.count()];
            moduleValues.Add(module, values);
            moduleById.Add(module.ID, module);
            moduleByType.Add(module.actuatorType, module);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        GameObject projectile = collision.collider.gameObject;
        Boolean destroy = false;

        HapticAttributes hapticAttributes = projectile.GetComponent<HapticAttributes>();
        TemperatureAttributes temperatureAttributes = projectile.GetComponent<TemperatureAttributes>();
        EMSAttributes emsAttributes = projectile.GetComponent<EMSAttributes>();

        if (hapticAttributes != null) {
            HapticFeedback(hapticAttributes, collision);
            destroy = true;
        }

        if (temperatureAttributes != null) {
            TemperatureFeedback(temperatureAttributes, collision);
            destroy = true;
        }

        if (emsAttributes != null)
        {
            EMSFeedback(emsAttributes, collision);
            destroy = true;
        }


        if (destroy == true) {
            Destroy(projectile);
        }
    }

    private void HapticFeedback(HapticAttributes hapticAttributes, Collision collision) {
        Module module = moduleByType[ActuatorType.Haptic];

        if (module == null) {
            Debug.Log("ERROR: module not found");
            return;
        }

        float velocity = collision.relativeVelocity.magnitude;
        float impulse = 2 * hapticAttributes.Mass * velocity;

        int[] values = moduleValues[module];
        Vector3[] actuatorPoints = module.getPositionOfActuators();

        for (int actuator = 0; actuator < module.count(); actuator++) {
            Vector3 closestPointOnColliderInDirectionActuator = collision.collider.ClosestPoint(actuatorPoints[actuator]);

            float distance = Vector3.Distance(actuatorPoints[actuator], closestPointOnColliderInDirectionActuator);
            // F = dI/dt with t = 1s: abs(F) = abs(I)
            float f = attenuation(distance) * impulse;
            //Debug.Log("Setting element (" + actuatorPoint.x + ", " + actuatorPoint.y + ") to value: " + f);

            values[actuator] = (int)f;

            QueueValues(module.ID, values);
        }
    }

    private void TemperatureFeedback(TemperatureAttributes temperatureAttributes, Collision collision)
    {
        Module module = moduleByType[ActuatorType.Temperature];
        double[] currentTemperatures = module.getMeasuredValues();

        if (module == null)
        {
            Debug.Log("ERROR: module not found");
            return;
        }

        int[] newTemeratures = moduleValues[module];
        Vector3[] actuatorPoints = module.getPositionOfActuators();

        /*
        float mass_head = this.GetComponent<HapticAttributes>().Mass;
        float mass_collider = collision.collider.GetComponent<HapticAttributes>().Mass;

        float c_head = this.GetComponent<TemperatureAttributes>().C;
        float c_collider = collision.collider.GetComponent<TemperatureAttributes>().C;

        int t_collider = collision.collider.GetComponent<TemperatureAttributes>().Temperature;

        for (int actuator = 0; actuator < module.count(); actuator++) {
            Vector3 closestPointOnColliderInDirectionActuator = collision.collider.ClosestPoint(actuatorPoints[actuator]);

            //Riemansche Mischregel
            int t_actuator = currentTemperatures[actuator];
            
            float targetTemperature = (mass_head * c_head * t_actuator + mass_collider * c_collider * t_collider) / (mass_head * c_head + mass_collider * c_collider);

            float deltaTemperature = targetTemperature - t_actuator;

            float distance = Vector3.Distance(actuatorPoints[actuator], closestPointOnColliderInDirectionActuator);
            
            //temperature linear with distance ????? TODO!!, USE C

            newTemeratures[actuator] = (int)(t_actuator + deltaTemperature/distance);
        }
        */

        for (int actuator = 0; actuator < module.count(); actuator++)
        {
            float distance = Vector3.Distance(actuatorPoints[actuator], collision.collider.ClosestPoint(actuatorPoints[actuator]));
            float expectedTemperature = collision.collider.GetComponent<TemperatureAttributes>().ExpectedTemperature;
            //newTemeratures[actuator] = (int)(expectedTemperature);

            //Debug.Log("Distance on Surface: " + distance + " Distance Resolution: " + DistanceResolution);

            if (distance < DistanceResolution)
            {
                distance = 1;
            }
            else
            {
                distance = distance / DistanceResolution;
            }

            /*
            float deltaTemperature = (expectedTemperature - (float)currentTemperatures[actuator]) / (distance);
            
            if (Math.Abs(deltaTemperature) > MinFeelableTempDif)
            {
                newTemeratures[actuator] = (int)(currentTemperatures[actuator] + deltaTemperature);
            }
            else
            {
                newTemeratures[actuator] = 0;
            }
            */
            
             newTemeratures[actuator] = (int)(expectedTemperature / distance);

            //Debug.Log("expected: " + collision.collider.GetComponent<TemperatureAttributes>().ExpectedTemperature + " current: " + currentTemperatures[actuator] + " new: " + newTemeratures[actuator] + " distance: "+distance);
        }

        QueueValues(module.ID, newTemeratures);

        nextTemperatureStep = 0f;
    }

    private void EMSFeedback(EMSAttributes emsAttributes, Collision collision) {
        Module module = moduleByType[ActuatorType.EMS];

        if (module == null)
        {
            Debug.Log("ERROR: module not found");
            return;
        }
        
        int[] values = moduleValues[module];
        Vector3[] actuatorPoints = module.getPositionOfActuators();

        for (int actuator = 0; actuator < module.count(); actuator++)
        {
            Vector3 closestPointOnColliderInDirectionActuator = collision.collider.ClosestPoint(actuatorPoints[actuator]);

            float distance = Vector3.Distance(actuatorPoints[actuator], closestPointOnColliderInDirectionActuator);
            float f = attenuation(distance) * emsAttributes.ExpectedEMS;

            values[actuator] = (int)f;

            QueueValues(module.ID, values);
        }

        nextEMSStep = 0f;
    }
    
    private void FixedUpdate() {
        if (moduleValues == null) {
            Debug.Log("ERROR: no modules available");
            return;
        }

        foreach (Module module in moduleValues.Keys) {
            int[] values = moduleValues[module];
            
            int[] valuesOld = new int[values.Length];
            Array.Copy(values, valuesOld, values.Length);

            if (module.actuatorType == ActuatorType.Haptic) {
                for (int i = 0; i < values.Length; i++) {
                    if (values[i] > 0) {
                        if (values[i] >= decresePerUpdate) {
                            values[i] -= decresePerUpdate;
                        }
                        else {
                            values[i] = 0;
                        }
                    }
                }
            }

            if (module.actuatorType == ActuatorType.Temperature) {
                nextTemperatureStep += Time.deltaTime;

                if (nextTemperatureStep >= temperatureOnTime) {
                    for (int i = 0; i < values.Length; i++) {
                        /*
                            if (values[i] == 26 || values[i] == 0) {
                                values[i] = 0;
                            }
                            else if (values[i] > 26) {
                                values[i] -= decresePerUpdate;
                            }
                            else if (values[i] < 26) {
                                values[i] += decresePerUpdate;
                            }
                        */
                        values[i] = 0;
                    }

                    nextTemperatureStep = 0f;
                }
            }

            if (module.actuatorType == ActuatorType.EMS) {
                nextEMSStep += Time.deltaTime;

                if (nextEMSStep >= emsOnTime) {
                    for (int i = 0; i < values.Length; i++) {
                        values[i] = 0;
                    }

                    nextEMSStep = 0f;
                }
            }

            bool equal = true;
        
            for (int i = 0; i < values.Length; i++) {
                if (valuesOld[i] != values[i]) {
                    equal = false;
                    break;
                }
            }

            if (!equal) {
                QueueValues(module.ID, values);
            }
        }
    }
}