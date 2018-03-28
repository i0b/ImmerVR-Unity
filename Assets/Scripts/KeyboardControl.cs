using UnityEngine;

public class KeyboardControl : MonoBehaviour {
    //public CollisionFeedback collisionFeedback;
    //public int mass;
    public TestSet testSet;
    private string input = "";

    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
           mass += 5;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (mass > 0) {
                mass -= 5;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            collisionFeedback.A -= 0.2f;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            collisionFeedback.A += 0.2f;
        }
        */

        if (Input.GetKeyDown(KeyCode.Return))
        {
            testSet.Log(input);
            testSet.NextTestState();
            input = "";
            //Debug.Log("continuing...");
        }

        else if (Input.anyKeyDown)
        {
            input += Input.inputString;
        }

        //if (Input.GetKeyUp(KeyCode.Space))
    }
}
