using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestSet : MonoBehaviour {
    //public GameObject projectilePrefab;
    public int[] testOrder;
    public float onDuration;
    public Canvas infoCanvas;
    public Text infoText;
    public Canvas selectCanvas;
    public Text selectText;
    public Button buttonOne;
    public Text buttonOneText;
    public Button buttonTwo;
    public Text buttonTwoText;

    private List<Dictionary<ActuatorId, int[]>[]> impactTest;
    private int testIndex;
    private TestActuationState testActuationState;
    private TestActuationSet testActuationSet;
    private TestGlobalState testGlobalState;
    private TestQuestionState testQuestionState;
    private float onTimeLeft;
    private AnswerState answerState;

    private enum TestGlobalState { TEST_INFO, ACTUATE, QUESTION };
    private enum TestActuationState { IDLE, SET, RUNNING };
    private enum TestActuationSet { READY, BASELINE, SATURATED, FINISHED };
    private enum TestQuestionState { FIRST_QUESTION, SECOND_QUESTION };
    private enum ActuatorId { VIBRATION, TEMPERATURE, EMS };
    public enum AnswerState { ONE, TWO, NONE };

    Communication communication;
    void Start() {
        communication = FindObjectOfType(typeof(Communication)) as Communication;

        testIndex = 0;
        testActuationState = TestActuationState.IDLE;
        testActuationSet = TestActuationSet.READY;
        testGlobalState = TestGlobalState.TEST_INFO;
        testQuestionState = TestQuestionState.FIRST_QUESTION;
        onTimeLeft = 0;

        impactTest = new List<Dictionary<ActuatorId, int[]>[]>();
        // Test Case 1
        impactTest.Add(new Dictionary<ActuatorId, int[]>[] {
        new Dictionary<ActuatorId, int[]> { { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } } },
        new Dictionary<ActuatorId, int[]> { { ActuatorId.VIBRATION,   new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25, 25, 0 } },
                                            { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } }}
        });

        // Test Case 2
        impactTest.Add(new Dictionary<ActuatorId, int[]>[] {
        new Dictionary<ActuatorId, int[]> { { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } } },
        new Dictionary<ActuatorId, int[]> { { ActuatorId.VIBRATION,   new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 50, 0 } },
                                            { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } } }
        });
        
        buttonOne.onClick.AddListener(delegate () { NextGlobalState(AnswerState.ONE); });
        buttonTwo.onClick.AddListener(delegate () { NextGlobalState(AnswerState.TWO); });
    }

    public void NextGlobalState(AnswerState answer) {
        if (testGlobalState == TestGlobalState.QUESTION && answer == AnswerState.NONE) {
            return;
        }

        bool skip = false;
        if (testGlobalState == TestGlobalState.ACTUATE) {
            skip = true;
        }

        switch (testGlobalState)
        {
            case TestGlobalState.TEST_INFO:
                testGlobalState = TestGlobalState.ACTUATE;
                break;
            case TestGlobalState.ACTUATE:
                NextActuateState();
                break;
            case TestGlobalState.QUESTION:
                NextTestState(answer);
                break;
        }

        if (!skip) {
            LogNewState(testGlobalState);
        }
    }

    private void LogNewState(TestGlobalState newGlobalState) {
        Debug.Log("new global state: " + newGlobalState.ToString());
    }

    private void NextTestState(AnswerState answer)
    {
        LogQuestion(testQuestionState, answer);

        //if ((int)testQuestionState < Enum.GetNames(typeof(TestQuestionState)).Length - 2)
        if (testQuestionState == TestQuestionState.FIRST_QUESTION)
        {
            testQuestionState = TestQuestionState.SECOND_QUESTION;
        }
        else
        {
            if (++testIndex == impactTest.Count)
            {
                Debug.Log("Test has been completed");

                Debug.Log("Starting next round");
                testIndex = 0;
                testActuationState = TestActuationState.IDLE;

            }

            testQuestionState = TestQuestionState.FIRST_QUESTION;
            testGlobalState = TestGlobalState.TEST_INFO;
        }
    }

    public void NextActuateState() {
        if (testActuationState == TestActuationState.RUNNING) {
            return;
        }

        if (testActuationSet == TestActuationSet.READY)
        {
            testActuationSet = TestActuationSet.BASELINE;
            testActuationState = TestActuationState.SET;
        }
        else if (testActuationSet == TestActuationSet.BASELINE)
        {
            testActuationSet = TestActuationSet.SATURATED;
            testActuationState = TestActuationState.SET;
        }
        else if (testActuationSet == TestActuationSet.SATURATED)
        {
            testActuationSet = TestActuationSet.READY;
            testActuationState = TestActuationState.IDLE;

            testGlobalState = TestGlobalState.QUESTION;
            LogNewState(testGlobalState);
            /*
            else {
                Debug.Log("Next dataset: " + testIndex);
                nextTestStateActive = true;
            }
            */
        }
        
        Debug.Log("new actuator state: " + testActuationSet.ToString());
    }
    private void LogActuators(TestActuationSet subTestSet) {
        int subTestIndex;
        if (testActuationSet == TestActuationSet.BASELINE)
        {
            subTestIndex = 0;
        }
        else if (testActuationSet == TestActuationSet.SATURATED)
        {
            subTestIndex = 1;
        }
        else
        {
            return;
        }

        string logString = "Set Values: { ";

        foreach (KeyValuePair<ActuatorId, int[]> entry in impactTest[testIndex][subTestIndex])
        {
            logString += "{ type: " + entry.Key.ToString() + " values: [";
            for (int i = 0; i < entry.Value.Length; i++)
            {
                logString += entry.Value[i];
                if (i < entry.Value.Length - 1)
                {
                    logString += ", ";
                }
                else
                {
                    logString += "]} ";
                }
            }
        }

        logString += "}";

        Debug.Log(logString);
    }

    private void LogQuestion(TestQuestionState questionNumber, AnswerState answer) {
        string logString = "test #"+testIndex+", question #"+(int)questionNumber+": " + answer.ToString();

        Debug.Log(logString);
    }
    
    void DoActuate() {
        Dictionary<ActuatorId, int[]> testSet = null;

        if (testIndex >= impactTest.Count)
        {
            Debug.Log("Test Index out of range.");
            communication.QueueValues((int)ActuatorId.VIBRATION, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            communication.QueueValues((int)ActuatorId.TEMPERATURE, new int[] { 0, 0, 0, 0 });
            communication.QueueValues((int)ActuatorId.EMS, new int[] { 0, 0 });
            return;
        }

        if (testActuationState == TestActuationState.IDLE) {
            infoText.text = "Press Return To Continue";
            infoCanvas.gameObject.SetActive(true);
            selectCanvas.gameObject.SetActive(false);
        }
        else {
            infoCanvas.gameObject.SetActive(false);
            selectCanvas.gameObject.SetActive(false);
        }

        if (testActuationSet == TestActuationSet.BASELINE)
        {
            testSet = impactTest[testIndex][0];
        }
        else if (testActuationSet == TestActuationSet.SATURATED)
        {
            testSet = impactTest[testIndex][1];
        }

        if (onTimeLeft > 0)
        {
            LogActuators(testActuationSet);
            onTimeLeft -= Time.deltaTime;
        }
        else
        {
            if (testActuationState == TestActuationState.SET)
            {
                testActuationState = TestActuationState.RUNNING;

                if (testActuationSet == TestActuationSet.BASELINE || testActuationSet == TestActuationSet.SATURATED)
                {
                    //Debug.Log("Test #" + testIndex + " Element " + testActuationSet.ToString());

                    foreach (KeyValuePair<ActuatorId, int[]> testElement in testSet)
                    {
                        int actuatorId = (int)testElement.Key;
                        int[] values = testElement.Value;

                        communication.QueueValues(actuatorId, values);
                    }

                    onTimeLeft = onDuration;
                }
            }

            else
            {
                testActuationState = TestActuationState.IDLE;
                communication.QueueValues((int)ActuatorId.VIBRATION, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
                communication.QueueValues((int)ActuatorId.TEMPERATURE, new int[] { 0, 0, 0, 0 });
                communication.QueueValues((int)ActuatorId.EMS, new int[] { 0, 0 });
            }
        }

    }

    private void DoQuestion()
    {
        switch (testQuestionState) {
            case TestQuestionState.FIRST_QUESTION:
                selectText.text = "how pleasant did you feel";
                buttonOneText.text = "very";
                buttonTwoText.text = "little";
                break;
            case TestQuestionState.SECOND_QUESTION:
                selectText.text = "which sensations was stronger";
                buttonOneText.text = "first";
                buttonTwoText.text = "second";
                break;
        }
    }

    void Update ()
    {
        switch (testGlobalState) {
            case TestGlobalState.TEST_INFO:
                infoText.text = "test pair #" + (testIndex+1);
                infoCanvas.gameObject.SetActive(true);
                selectCanvas.gameObject.SetActive(false);
                break;
            case TestGlobalState.ACTUATE:
                infoCanvas.gameObject.SetActive(false);
                selectCanvas.gameObject.SetActive(false);
                DoActuate();
                break;
            case TestGlobalState.QUESTION:
                infoCanvas.gameObject.SetActive(false);
                selectCanvas.gameObject.SetActive(true);
                DoQuestion();
                break;
        }
    }
}
