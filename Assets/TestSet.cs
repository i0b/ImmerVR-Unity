using System.Collections.Generic;
using UnityEngine;

class AttributeSet {
    public int Mass;
    public int ExpectedTemperature;
    public int ExpectedEMS;
    public Vector3 Position;

    public AttributeSet(Vector3 position, int mass, int expectedTemperature, int expectedEMS) {
        Position = position;
        Mass = mass;
        ExpectedTemperature = expectedTemperature;
        ExpectedEMS = expectedEMS;
    }
}

public class TestSet : MonoBehaviour {
    public GameObject projectilePrefab;
    public int[] testOrder;
    public bool contineOk;

    private List<AttributeSet[]> impactTest;
    private int testIndex;
    private TestState testState;
    private AttributeSet currentTestValues;

    private enum TestState { BASELINE, SATURATED, NEXT };

    Communication communication;
    void Start() {
        communication = FindObjectOfType(typeof(Communication)) as Communication;

        testIndex = 0;
        testState = TestState.BASELINE;
        contineOk = false;

        impactTest = new List<AttributeSet[]>();
        // AttributeSet: Position, Mass, Expected Temperatrue, Expected EMS
        // Test Case 1
        impactTest.Add(new AttributeSet[] { new AttributeSet(new Vector3(20, 10, 20), 60, 25, 0),
                                            new AttributeSet(new Vector3(20, 10, 20), 60, -20, 0) });

        // Test Case 2
        impactTest.Add(new AttributeSet[] { new AttributeSet(new Vector3(-20, 10, 20), 90, 25, 0),
                                            new AttributeSet(new Vector3(-20, 10, 20), 90, 80, 0) });

        // Test Case 3
        impactTest.Add(new AttributeSet[] { new AttributeSet(new Vector3(20, 10, 20), 60, 25, 0),
                                            new AttributeSet(new Vector3(20, 10, 20), 60, 25, 8) });


        // Test Case 4
        impactTest.Add(new AttributeSet[] { new AttributeSet(new Vector3(-20, 10, 20), 90, 25, 0),
                                            new AttributeSet(new Vector3(-20, 10, 20), 90, 25, 8) });


        //communication.QueueValues(0, new int[]{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        //communication.QueueValues(1, new int[] { 0, 0, 0, 0 });
        //communication.QueueValues(2, new int[] { 0, 0 });

    }

    public void log(string input) {
        Debug.Log(input + JsonUtility.ToJson(currentTestValues));
    }
    
    void doTest() {
        if (testIndex >= impactTest.Count) {
            return;
        }
        AttributeSet[] projectileAttributes = impactTest[testIndex];

        
        // prepare next test
        if (testState == TestState.NEXT && contineOk == true)
        {
            testState = TestState.BASELINE;
            if (++testIndex == impactTest.Count)
            {
                Debug.Log("Test has been completed");
                testIndex = 0;
            }
            /*
            else {
                Debug.Log("Next dataset: " + testIndex);
            }
            */
        }

        else if (contineOk == true) {
            Debug.Log("Test #" + testIndex + " Element " + testState.ToString());

            currentTestValues = projectileAttributes[(int)testState];

            createProjectile(currentTestValues);
            contineOk = false;
            testState++;

            // wait for result
            Debug.Log("Input user response: ");
        }
    }

    void createProjectile(AttributeSet projectileAttributes) {
        GameObject newProjectile = Instantiate(
                projectilePrefab,
                projectileAttributes.Position,
                Quaternion.identity
            );

        newProjectile.GetComponent<HapticAttributes>().Mass = projectileAttributes.Mass;
        newProjectile.GetComponent<TemperatureAttributes>().ExpectedTemperature = projectileAttributes.ExpectedTemperature;
        newProjectile.GetComponent<EMSAttributes>().ExpectedEMS = projectileAttributes.ExpectedEMS;
    }

    void Update () {
        doTest();
    }
}
