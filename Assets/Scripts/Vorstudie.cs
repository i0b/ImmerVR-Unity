using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Vorstudie : MonoBehaviour
{
    public Text infoText;
    public SteamVR_TrackedController trackedController;
    public Module TemperatureModule;
    public int MaxTemperatureDifference = 5;
    public AudioSource clickAudio;
    public int MaxValueEMS = 10;

    public Communication communication;

    public string Filename;

    private string fullpath;
    private FileStream fileStream;
    private StreamWriter streamWriter;

    private int currentValue;
    private long minValue;
    private long maxValue;
    private bool coolDown;
    private static float COOLDOWNTIME = 0.5f;
    private float temperature = 0;

    private enum ActuatorType { EMS, TEMPERATURE, VIBRATION };
    //private Communication communication;
    
    private enum TestState { EMS1_LEFT, EMS1_RIGHT, COLD_UP_RIGHT, COLD_DOWN_RIGHT, VIBRATION, HOT_UP_RIGHT, HOT_DOWN_RIGHT, EMS2_LEFT, EMS2_RIGHT, FINISHED };
    private Dictionary<TestState, ActuatorType> stateToActuatorMap;
    private TestState state;

    private enum RunningState { READY, SET_MIN, SET_MAX }
    private RunningState runningState;

    void Start () {
        currentValue = 0;
        runningState = RunningState.READY;
        
        //communication = FindObjectOfType(typeof(Communication)) as Communication;
        //AllOff();
        state = TestState.EMS1_LEFT;
        infoText.text = state.ToString()+" ready";
        runningState = RunningState.READY;
        
        InitializeLogFile();

        stateToActuatorMap = new Dictionary<TestState, ActuatorType>();
        stateToActuatorMap.Add(TestState.EMS1_LEFT, ActuatorType.EMS);
        stateToActuatorMap.Add(TestState.EMS1_RIGHT, ActuatorType.EMS);

        stateToActuatorMap.Add(TestState.EMS2_LEFT, ActuatorType.EMS);
        stateToActuatorMap.Add(TestState.EMS2_RIGHT, ActuatorType.EMS);

        stateToActuatorMap.Add(TestState.COLD_DOWN_RIGHT, ActuatorType.TEMPERATURE);
        stateToActuatorMap.Add(TestState.COLD_UP_RIGHT, ActuatorType.TEMPERATURE);

        stateToActuatorMap.Add(TestState.HOT_DOWN_RIGHT, ActuatorType.TEMPERATURE);
        stateToActuatorMap.Add(TestState.HOT_UP_RIGHT, ActuatorType.TEMPERATURE);

        stateToActuatorMap.Add(TestState.VIBRATION, ActuatorType.VIBRATION);
    }

    private void OnEnable()
    {
        trackedController.TriggerClicked -= HandleTriggerClicked;
        trackedController.TriggerClicked += HandleTriggerClicked;

        trackedController.PadClicked -= HandleTouchPadClicked;
        trackedController.PadClicked += HandleTouchPadClicked;
    }

    private void ModifyCurrentValue(bool increase) {
        clickAudio.Play();

        // increase: button UP
        bool decrease = !increase;

        switch (stateToActuatorMap[state])
        {
            case ActuatorType.EMS:
                if (currentValue == MaxValueEMS)
                {
                    AllOff();
                    maxValue = -1;
                    Log();
                    nextState();
                }
                else if (currentValue > minValue && decrease)
                {
                    currentValue--;
                }
                else if (increase) {
                    currentValue++;
                }
                break;
            case ActuatorType.VIBRATION:
                if (currentValue == 100)
                {
                    AllOff();
                    maxValue = -1;
                    Log();
                    nextState();
                }
                else if (currentValue > minValue && decrease)
                {
                    currentValue = currentValue - 10;
                }
                else if (increase)
                {
                    currentValue = currentValue + 10;
                }
                break;
            case ActuatorType.TEMPERATURE:
                if (currentValue == 0) {
                    break;
                }

                if (Math.Abs(temperature - currentValue) > MaxTemperatureDifference || currentValue >= 30 )
                {
                    AllOff();
                    maxValue = -1;
                    Log();
                    nextState();
                    break;
                }

                if (increase)
                {
                    if (state == TestState.COLD_DOWN_RIGHT || state == TestState.COLD_UP_RIGHT)
                    {
                        if (currentValue < temperature)
                        {
                            currentValue++;
                        }
                    }
                    else
                    {
                        currentValue++;
                    }
                }
                else if (decrease)
                {
                    if (state == TestState.HOT_DOWN_RIGHT || state == TestState.HOT_UP_RIGHT)
                    {
                        if (currentValue > temperature)
                        {
                            currentValue--;
                        }
                    }
                    else
                    {
                        currentValue--;
                    }

                }
                break;
        }
    }

    private void HandleTouchPadClicked(object sender, ClickedEventArgs e)
    {
        if (runningState == RunningState.READY)
        {
            return;
        }

        if (!coolDown)
        {
            coolDown = true;

            bool increase = e.padY > 0;

            ModifyCurrentValue(increase);
            
            SendCommand(currentValue);

            StartCoroutine(ResetCoolDown());
        }
    }

    IEnumerator ResetCoolDown()
    {
        yield return new WaitForSeconds(COOLDOWNTIME);
        coolDown = false;
    }

    public void InitializeLogFile()
    {
        //filepath = Application.persistentDataPath;
        fullpath = communication.filepath + "/" + Filename + "_" + communication.UserID + "_values_" + System.DateTime.Now.ToString("_yyMMdd_hhmmss") + ".csv";

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

        streamWriter.WriteLine(currentTimestamp + ";" + communication.UserID + ";" + exportString);
        streamWriter.Flush();
    }

    private void AllOff()
    {
        currentValue = 0;
        communication.QueueValues(0, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        communication.QueueValues(1, new int[] { 0, 0, 0, 0 });
        communication.QueueValues(2, new int[] { 0, 0 });
    }

    private void Log() {
        WriteToLogFile(state.ToString() + "; " + minValue.ToString() + "; " + maxValue.ToString() + ";");
    }

    private void SendCommand(int intensity) {
        if (state == TestState.EMS1_LEFT || state == TestState.EMS2_LEFT) {
                    communication.QueueValues(2, new int[] { intensity, 0 });
        }
        else if (state == TestState.EMS1_RIGHT || state == TestState.EMS2_RIGHT) {
            communication.QueueValues(2, new int[] { 0, intensity });
        }
        else if (state == TestState.COLD_DOWN_RIGHT || state == TestState.HOT_DOWN_RIGHT)
        {
            communication.QueueValues(1, new int[] { 0, 0, 0, intensity });
        }
        else if (state == TestState.COLD_UP_RIGHT || state == TestState.HOT_UP_RIGHT)
        {
            communication.QueueValues(1, new int[] { 0, 0, intensity, 0 });
        }
        else if (state == TestState.VIBRATION) {
            communication.QueueValues(0, new int[] { intensity, intensity, intensity, intensity,
                                                    intensity, intensity, intensity, intensity,
                                                    intensity, intensity, intensity, intensity,
                                                    intensity, intensity, intensity, intensity });
        }
    }

    void nextState()
    {
        if (state == TestState.FINISHED)
        {
            infoText.text = "Finished";
            AllOff();
            return;
        }

        AllOff();
        minValue = 0;
        maxValue = 0;

        runningState = RunningState.READY;
        Debug.Log("New Test State: " + (++state).ToString());

        infoText.text = state.ToString() + " ready";
    }

    private void HandleTriggerClicked(object sender, ClickedEventArgs e)
    {
        Debug.Log("Controller Trigger Clicked");

        if (state == TestState.FINISHED)
        {
            infoText.text = "Finished";
            AllOff();
            return;
        }
        
        Debug.Log("current test: " + state.ToString());
        if (runningState == RunningState.READY)
        {
            if (state == TestState.COLD_DOWN_RIGHT || state == TestState.HOT_DOWN_RIGHT)
            {
                temperature = (float)TemperatureModule.getMeasuredValues()[3];
                currentValue = (int)temperature;
            }
            else if (state == TestState.COLD_UP_RIGHT || state == TestState.HOT_UP_RIGHT)
            {
                temperature = (float)TemperatureModule.getMeasuredValues()[2];
                currentValue = (int)temperature;
            }
            runningState = RunningState.SET_MIN;
            infoText.text = state.ToString()+": press trigger to set min value";
        }
        else if (runningState == RunningState.SET_MIN)
        {
            Debug.Log("Min value set to: " + currentValue);
            minValue = currentValue;
            runningState = RunningState.SET_MAX;
            infoText.text = state.ToString()+": press trigger to set max value";
        }
        else if (runningState == RunningState.SET_MAX)
        {
            maxValue = currentValue;
            runningState = RunningState.READY;
            infoText.text = state.ToString()+": ready";
            AllOff();
            Log();
            nextState();
        }
    }
}
