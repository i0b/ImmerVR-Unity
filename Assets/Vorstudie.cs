using UnityEngine;
using UnityEngine.UI;

public class Vorstudie : MonoBehaviour
{
    public Text infoText;

    public Logger log;

    private int currentValue;
    private long minValue;
    private long maxValue;

    private enum ActuatorType { EMS, HOT, COLD, VIBRATION };
    private enum ElementPosition { ALL, LEFT_OR_UP, RIGHT_OR_DOWN };
    private Communication communication;
    
    private enum TestState { EMS1_LEFT, EMS1_RIGHT, COLD_UP_LEFT, COLD_DOWN_RIGHT, VIBRATION, HOT_UP_LEFT, HOT_DOWN_RIGHT, EMS2_LEFT, EMS2_RIGHT, FINISHED };
    private TestState state;
    private bool tempertureActuating;
    private SteamVR_TrackedController trackedController;

    void Start () {
        currentValue = 0;
        infoText.text = "";
        communication = FindObjectOfType(typeof(Communication)) as Communication;
        AllOff();
        state = TestState.EMS1_LEFT;
        tempertureActuating = false;
        
        log.Initialize("Vorstudie");
        log.writeHeader("Timestamp;UserID;ActuatorType;MinValue;MaxValue;");

        trackedController = GetComponent<SteamVR_TrackedController>();
        if (trackedController == null)
        {
            trackedController = GetComponentInParent<SteamVR_TrackedController>();
        }

        trackedController.TriggerClicked += HandleTriggerClicked;
    }

    private void AllOff() {
        SendCommand(ActuatorType.COLD, ElementPosition.ALL, 0);
        //SendCommand(ActuatorType.HOT, ElementPosition.ALL, 0);
        SendCommand(ActuatorType.VIBRATION, ElementPosition.ALL, 0);
        SendCommand(ActuatorType.EMS, ElementPosition.ALL, 0);
    }

    private void Log() {
        log.writeToLog(state.ToString() + "; " + minValue.ToString() + "; " + maxValue.ToString() + ";");
        minValue = 0;
        maxValue = 0;
    }

    private void IncValue(ActuatorType actuatorType)
    {
        switch (actuatorType)
        {
            case ActuatorType.EMS:
                if (currentValue == 20) {
                    AllOff();
                    maxValue = -1;
                    Log();
                    nextState();
                }
                currentValue++;
                break;
            case ActuatorType.VIBRATION:
                if (currentValue == 100) {
                    AllOff();
                    maxValue = -1;
                    Log();
                    nextState();
                }
                currentValue += 10;
                break;
        }

        Debug.Log("New Value: " + currentValue);
    }

    private void SendCommand(ActuatorType actuatorType, ElementPosition element, int intensity) {
        switch (actuatorType) {
            case ActuatorType.EMS:
                if (element == ElementPosition.LEFT_OR_UP) {
                    communication.QueueValues(2, new int[] { intensity, 0 });
                }
                else if (element == ElementPosition.RIGHT_OR_DOWN) {
                    communication.QueueValues(2, new int[] { 0, intensity });
                }
                else if (element == ElementPosition.ALL) {
                    communication.QueueValues(2, new int[] { intensity, intensity });
                }
                break;
            case ActuatorType.HOT:
                if (intensity != 1) {
                    communication.QueueValues(1, new int[] { 0, 0, 0, 0 });
                }
                else if (element == ElementPosition.LEFT_OR_UP) {
                    communication.QueueValues(1, new int[] { 0, 30, 0, 0 });
                }
                else if (element == ElementPosition.RIGHT_OR_DOWN) {
                    communication.QueueValues(1, new int[] { 0, 0, 0, 30 });
                }
                else if (element == ElementPosition.ALL) {
                    communication.QueueValues(1, new int[] { 30, 30, 30, 30 });
                }
                break;
            case ActuatorType.COLD:
                if (intensity != 1) {
                    communication.QueueValues(1, new int[] { 0, 0, 0, 0 });
                }
                if (element == ElementPosition.LEFT_OR_UP) {
                    communication.QueueValues(1, new int[] { 0, 20, 0, 0 });
                }
                else if (element == ElementPosition.RIGHT_OR_DOWN) {
                    communication.QueueValues(1, new int[] { 0, 0, 0, 20 });
                }
                else if (element == ElementPosition.ALL) {
                    communication.QueueValues(1, new int[] { 20, 20, 20, 20 });
                }
                break;
            case ActuatorType.VIBRATION:
                communication.QueueValues(0, new int[] { intensity, intensity, intensity, intensity,
                                                         intensity, intensity, intensity, intensity,
                                                         intensity, intensity, intensity, intensity,
                                                         intensity, intensity, intensity, intensity });
                break;
        }
    }

    private void HandleTriggerClicked(object sender, ClickedEventArgs e)
    {
        if (tempertureActuating == true)
        {
            SendCommand(ActuatorType.COLD, ElementPosition.ALL, 0);
            tempertureActuating = false;
            maxValue = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            Log();
            nextState();
            return;
        }
        switch (state) {
            case TestState.COLD_DOWN_RIGHT:
                SendCommand(ActuatorType.COLD, ElementPosition.RIGHT_OR_DOWN, 1);
                break;
            case TestState.COLD_UP_LEFT:
                SendCommand(ActuatorType.COLD, ElementPosition.LEFT_OR_UP, 1);
                break;
            case TestState.HOT_DOWN_RIGHT:
                SendCommand(ActuatorType.HOT, ElementPosition.RIGHT_OR_DOWN, 1);
                break;
            case TestState.HOT_UP_LEFT:
                SendCommand(ActuatorType.HOT, ElementPosition.LEFT_OR_UP, 1);
                break;
        }


        if (state == TestState.COLD_DOWN_RIGHT || state == TestState.COLD_UP_LEFT || state == TestState.HOT_DOWN_RIGHT || state == TestState.HOT_UP_LEFT) {
            minValue = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            tempertureActuating = true;
        }
    }

    void nextState() {
        if (state == TestState.FINISHED) {
            return;
        }

        Debug.Log("New Test State: " + (++state).ToString());
        currentValue = 0;
    }

    void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (state == TestState.EMS1_LEFT || state == TestState.EMS2_LEFT)
            {
                IncValue(ActuatorType.EMS);
                SendCommand(ActuatorType.EMS, ElementPosition.LEFT_OR_UP, currentValue);
            }
            else if (state == TestState.EMS1_RIGHT || state == TestState.EMS2_RIGHT)
            {
                IncValue(ActuatorType.EMS);
                SendCommand(ActuatorType.EMS, ElementPosition.RIGHT_OR_DOWN, currentValue);
            }

            else if (state == TestState.VIBRATION)
            {
                IncValue(ActuatorType.VIBRATION);
                SendCommand(ActuatorType.VIBRATION, ElementPosition.ALL, currentValue);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            AllOff();
            maxValue = currentValue;
            Log();
            nextState();
        }

        else if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.M)) {
            minValue = currentValue;
            Debug.Log("MinValue set to: " + minValue);
        }
	}
}
