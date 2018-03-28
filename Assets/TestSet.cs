using System.Collections.Generic;
using UnityEngine;

public class TestSet : MonoBehaviour {
    //public GameObject projectilePrefab;
    public int[] testOrder;
    public float onDuration;
    
    private List<Dictionary<ActuatorId, int[]>[]> impactTest;
    private int testIndex;
    private TestState testState;
    private float onTimeLeft;
    private bool nextTestStateActive;

    private enum TestState { BASELINE, SATURATED, NEXT };
    private enum ActuatorId { VIBRATION, TEMPERATURE, EMS };

    Communication communication;
    void Start() {
        communication = FindObjectOfType(typeof(Communication)) as Communication;

        testIndex = 0;
        testState = TestState.BASELINE;
        nextTestStateActive = true;
        onTimeLeft = 0;

        impactTest = new List<Dictionary<ActuatorId, int[]>[]>();
        // Test Case 1
        impactTest.Add(new Dictionary<ActuatorId, int[]>[] {
        new Dictionary<ActuatorId, int[]> { { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } } },
        new Dictionary<ActuatorId, int[]> { { ActuatorId.VIBRATION,   new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 25, 25, 0 } },
                                            { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } } }
        });

        // Test Case 2
        impactTest.Add(new Dictionary<ActuatorId, int[]>[] {
        new Dictionary<ActuatorId, int[]> { { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } } },
        new Dictionary<ActuatorId, int[]> { { ActuatorId.VIBRATION,   new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 50, 0 } },
                                            { ActuatorId.TEMPERATURE, new[] { 0, 0, 0, 20 } } }
        });
    }

    public void NextTestState() {
        if (testState == TestState.SATURATED)
        {
            testState = TestState.BASELINE;

            if (++testIndex == impactTest.Count)
            {
                Debug.Log("Test has been completed");
                //nextTestStateActive = false;


                Debug.Log("Starting next round");
                testIndex = 0;
                nextTestStateActive = true;

            }
            /*
            else {
                Debug.Log("Next dataset: " + testIndex);
                nextTestStateActive = true;
            }
            */
        }
        else
        {
            testState = TestState.SATURATED;
            nextTestStateActive = true;
        }
    }

    public void Log(string input) {
        string logString = "user response: " + input;

        foreach (KeyValuePair<ActuatorId, int[]> entry in impactTest[testIndex][(int)testState])
        {
            logString += " Actuator Type: " + entry.Key.ToString() + " values: [";
            for (int i = 0; i < entry.Value.Length; i++) {
                logString += entry.Value[i];
                if (i< entry.Value.Length-1)
                {
                    logString += ", ";
                }
                else
                {
                    logString += "]";
                }
            }
        }

        Debug.Log(logString);
    }
    
    void DoTest() {
        Dictionary<ActuatorId, int[]> testSet = impactTest[testIndex][(int)testState];

        if (onTimeLeft > 0)
        {
            onTimeLeft -= Time.deltaTime;
        }
        else
        {
            communication.QueueValues((int)ActuatorId.VIBRATION, new int[]{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            communication.QueueValues((int)ActuatorId.TEMPERATURE, new int[] { 0, 0, 0, 0 });
            communication.QueueValues((int)ActuatorId.EMS, new int[] { 0, 0 });
        }

        if (testIndex >= impactTest.Count)
        {
            return;
        }
        else if (nextTestStateActive == true)
        {
            nextTestStateActive = false;

            Debug.Log("Test #" + testIndex + " Element " + testState.ToString());

            foreach (KeyValuePair<ActuatorId, int[]> testElement in testSet) {
                int actuatorId = (int)testElement.Key;
                int[] values = testElement.Value;

                communication.QueueValues(actuatorId, values);
            }

            onTimeLeft = onDuration;
            
            Debug.Log("Input user response: ");
        }
    }

    void Update () {
        DoTest();
    }
}
