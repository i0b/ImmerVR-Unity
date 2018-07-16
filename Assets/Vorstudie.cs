using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Vorstudie : MonoBehaviour
{
    public Text infoText;
    public SteamVR_TrackedController trackedController;

    public string filename, filepath;
    private string fullpath;

    public int UserID = 0;

    private FileStream fileStream;
    private StreamWriter streamWriter;

    private int currentValue;
    private long minValue;
    private long maxValue;

    private enum ActuatorType { EMS, HOT, COLD, VIBRATION };
    private enum ElementPosition { ALL, LEFT_OR_UP, RIGHT_OR_DOWN };
    private Communication communication;
    
    private enum TestState { EMS1_LEFT, EMS1_RIGHT, COLD_UP_LEFT, COLD_DOWN_RIGHT, VIBRATION, HOT_UP_LEFT, HOT_DOWN_RIGHT, EMS2_LEFT, EMS2_RIGHT, FINISHED };
    private Dictionary<TestState, ActuatorType> stateToActuatorMap;
    private TestState state;

    private enum RunningState { READY, SET_MIN, SET_MAX }
    private RunningState runningState;

    void Start () {
        currentValue = 0;
        runningState = RunningState.READY;
        infoText.text = "ready";
        communication = FindObjectOfType(typeof(Communication)) as Communication;
        //AllOff();
        state = TestState.EMS1_LEFT;
        runningState = RunningState.READY;
        
        InitializeLogFile();
        StartCoroutine(initialForceFeedback());

        stateToActuatorMap = new Dictionary<TestState, ActuatorType>();
        stateToActuatorMap.Add(TestState.EMS1_LEFT, ActuatorType.EMS);
        stateToActuatorMap.Add(TestState.EMS1_RIGHT, ActuatorType.EMS);

        stateToActuatorMap.Add(TestState.EMS2_LEFT, ActuatorType.EMS);
        stateToActuatorMap.Add(TestState.EMS2_RIGHT, ActuatorType.EMS);

        stateToActuatorMap.Add(TestState.COLD_DOWN_RIGHT, ActuatorType.COLD);
        stateToActuatorMap.Add(TestState.COLD_UP_LEFT, ActuatorType.COLD);

        stateToActuatorMap.Add(TestState.HOT_DOWN_RIGHT, ActuatorType.HOT);
        stateToActuatorMap.Add(TestState.HOT_UP_LEFT, ActuatorType.HOT);

        stateToActuatorMap.Add(TestState.VIBRATION, ActuatorType.VIBRATION);
    }

    private void OnEnable()
    {
        trackedController.TriggerClicked -= HandleTriggerClicked;
        trackedController.TriggerClicked += HandleTriggerClicked;
    }
    IEnumerator initialForceFeedback() {
        SendCommand(0, ElementPosition.ALL, 100);
        yield return new WaitForSeconds(2.0f);
        AllOff();
    }

    IEnumerator updateIntensityValue(TestState calledState)
    {
            switch (stateToActuatorMap[calledState])
            {
                case ActuatorType.EMS:
                    if (currentValue == 10)
                    {
                        AllOff();
                        maxValue = -1;
                        Log();
                        nextState();
                    }
                    currentValue++;
                    break;
                case ActuatorType.VIBRATION:
                    if (currentValue == 100)
                    {
                        AllOff();
                        maxValue = -1;
                        Log();
                        nextState();
                    }
                    currentValue += 10;
                    break;
            }

            if (state == TestState.EMS1_LEFT || state == TestState.EMS2_LEFT)
            {
                SendCommand(ActuatorType.EMS, ElementPosition.LEFT_OR_UP, currentValue);
            }
            else if (state == TestState.EMS1_RIGHT || state == TestState.EMS2_RIGHT)
            {
                SendCommand(ActuatorType.EMS, ElementPosition.RIGHT_OR_DOWN, currentValue);
            }

            else if (state == TestState.VIBRATION)
            {
                SendCommand(ActuatorType.VIBRATION, ElementPosition.ALL, currentValue);
            }

        //Debug.Log("New Value: " + currentValue);

        yield return new WaitForSeconds(1.0f);

        if (calledState == state)
        {
            StartCoroutine(updateIntensityValue(calledState));
        }
    }

    public void InitializeLogFile()
    {
        //filepath = Application.persistentDataPath;
        fullpath = filepath + "/" + filename + "_" + UserID + "_values_" + System.DateTime.Now.ToString("_yyMMdd_hhmmss") + ".csv";

        Debug.Log(fullpath);

        fileStream = new FileStream(fullpath, FileMode.Append);
        streamWriter = new StreamWriter(fileStream);

        if (streamWriter == null)
        {
            return;
        }
        
        streamWriter.WriteLine("Timestamp;UserID;ActuatorType;MinValue;MaxValue;");
        streamWriter.Flush();

    }

    public void WriteToLogFile(string exportString)
    {
        if (streamWriter == null)
        {
            return;
        }

        long currentTimestamp = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;

        streamWriter.WriteLine(currentTimestamp + ";" + UserID + ";" + exportString);
        streamWriter.Flush();
    }

    private void AllOff()
    {
        currentValue = 0;
        //runningState = RunningState.READY;
        //communication.QueueValues(1, new int[] { 0, 0, 0, 0 });
        SendCommand(ActuatorType.COLD, ElementPosition.ALL, 0);
        SendCommand(ActuatorType.VIBRATION, ElementPosition.ALL, 0);
        SendCommand(ActuatorType.EMS, ElementPosition.ALL, 0);
    }

    private void Log() {
        WriteToLogFile(state.ToString() + "; " + minValue.ToString() + "; " + maxValue.ToString() + ";");
        minValue = 0;
        maxValue = 0;
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
                if (intensity != 1)
                {
                    communication.QueueValues(1, new int[] { 0, 0, 0, 0 });
                }
                else if (element == ElementPosition.LEFT_OR_UP)
                {
                    communication.QueueValues(1, new int[] { 0, 30, 0, 0 });
                }
                else if (element == ElementPosition.RIGHT_OR_DOWN)
                {
                    communication.QueueValues(1, new int[] { 0, 0, 0, 30 });
                }
                else if (element == ElementPosition.ALL)
                {
                    communication.QueueValues(1, new int[] { 30, 30, 30, 30 });
                }
                break;
            case ActuatorType.COLD:
                if (intensity != 1) {
                    communication.QueueValues(1, new int[] { 0, 0, 0, 0 });
                }
                else if (element == ElementPosition.LEFT_OR_UP) {
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

    void nextState()
    {
        if (state == TestState.FINISHED)
        {
            return;
        }

        AllOff();
        StopAllCoroutines();

        runningState = RunningState.READY;
        infoText.text = "ready";

        Debug.Log("New Test State: " + (++state).ToString());
    }

    private void HandleTriggerClicked(object sender, ClickedEventArgs e)
    {
        Debug.Log("Controller Trigger Clicked");

        if (state == TestState.FINISHED)
        {
            return;
        }

        if (stateToActuatorMap[state] == ActuatorType.VIBRATION || stateToActuatorMap[state] == ActuatorType.EMS)
        {
            Debug.Log("current test: " + state.ToString());
            if (runningState == RunningState.READY)
            {
                StartCoroutine(updateIntensityValue(state));
                runningState = RunningState.SET_MIN;
                infoText.text = "press trigger to set min value";
            }
            else if (runningState == RunningState.SET_MIN)
            {
                Debug.Log("Min value set to: " + currentValue);
                minValue = currentValue;
                runningState = RunningState.SET_MAX;
                infoText.text = "press trigger to set max value";
            }
            else if (runningState == RunningState.SET_MAX)
            {
                maxValue = currentValue;
                runningState = RunningState.READY;
                infoText.text = "ready";
                AllOff();
                Log();
                nextState();
            }
        }

        else
        {
            if (runningState == RunningState.READY)
            {
                switch (state)
                {
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
                runningState = RunningState.SET_MIN;
                infoText.text = "press trigger to set min value";
            }
            else if (runningState == RunningState.SET_MIN)
            {
                minValue = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                runningState = RunningState.SET_MAX;
                infoText.text = "press trigger to set max value";
            }
            else if (runningState == RunningState.SET_MAX) {
                AllOff();
                infoText.text = "ready";
                runningState = RunningState.READY;
                maxValue = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                Log();
                nextState();
            }
        }
    }
    
}
