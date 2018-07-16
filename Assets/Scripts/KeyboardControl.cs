using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyboardControl : MonoBehaviour {
    //public CollisionFeedback collisionFeedback;
    //public int mass;
    public TestSet testSet;
    private string input = "";

    public Button buttonOne;
    public Button buttonTwo;

    private ColorBlock colorYes;
    private ColorBlock colorNo;
    private ColorBlock colorHighlight;

    private Button selectedButton;

    void Update()
    {
        colorHighlight = new ColorBlock();
        colorHighlight.normalColor = Color.white;
        colorHighlight.highlightedColor = new Color(0.7f, 0.7f, 0.7f);
        colorHighlight.colorMultiplier = 1;

        colorYes = new ColorBlock();
        colorYes.normalColor = Color.white;
        colorYes.highlightedColor = new Color(0.25f, 1f, 0.35f);
        colorYes.colorMultiplier = 1;

        colorNo = new ColorBlock();
        //colorNo.normalColor = Color.red;
        colorNo.normalColor = Color.white;
        colorNo.highlightedColor = new Color(1f, 0.25f, 0.35f);
        colorNo.colorMultiplier = 1;

        buttonOne.colors = colorHighlight;
        buttonTwo.colors = colorHighlight;
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
        */

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ExecuteEvents.Execute<IPointerEnterHandler>(buttonOne.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute<IPointerExitHandler>(buttonTwo.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            selectedButton = buttonOne;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ExecuteEvents.Execute<IPointerEnterHandler>(buttonTwo.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute<IPointerExitHandler>(buttonOne.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            selectedButton = buttonTwo;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (selectedButton != null)
            {
                ExecuteEvents.Execute<IPointerClickHandler>(selectedButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute<IPointerExitHandler>(buttonOne.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
                ExecuteEvents.Execute<IPointerExitHandler>(buttonTwo.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
                selectedButton = null;
            }
            else
            {
                testSet.NextGlobalState(TestSet.AnswerState.NONE);
            }
        }

        else if (Input.anyKeyDown)
        {
            input += Input.inputString;
        }

        //if (Input.GetKeyUp(KeyCode.Space))
    }
}
