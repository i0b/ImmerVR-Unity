using System.Collections.Generic;
using UnityEngine;

public class AmbientFeelable : MonoBehaviour {
    void Start ()
    {
        AmbientFeedback feedbackController = FindObjectOfType(typeof(AmbientFeedback)) as AmbientFeedback;

        List<ActuatorType> actuatorTypes = new List<ActuatorType>();

        if (this.gameObject.GetComponent<HapticAttributes>()) {
            actuatorTypes.Add(ActuatorType.Haptic);
        }
        if (this.gameObject.GetComponent<TemperatureAttributes>()) {
            actuatorTypes.Add(ActuatorType.Temperature);
        }
        if (this.gameObject.GetComponent<EMSAttributes>())
        {
            actuatorTypes.Add(ActuatorType.EMS);
        }

        feedbackController.AddTrackedObject(this.gameObject, actuatorTypes.ToArray());
    }
}
