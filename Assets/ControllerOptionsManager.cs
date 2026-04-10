using UnityEngine;
using UnityEngine.UI;

public class ControllerOptionsManager : MonoBehaviour
{
    private bool isUsingGamepad = false;

    [SerializeField] private GameObject sprite1;
    [SerializeField] private GameObject sprite2;

    [SerializeField] private GameObject leftPanel;
    [SerializeField] private GameObject rightPanel;

    [SerializeField] private GameObject leftPanelIndicator;
    [SerializeField] private GameObject rightPanelIndicator;

    [SerializeField] private GameObject defaultleftPanelButton;
    [SerializeField] private GameObject defaultrightPanelButton;

    void Start()
    {
        if (IsAnyJoystickButton() || IsJoystickMoving())
        {
            isUsingGamepad = true;
        }
        else 
            {
            isUsingGamepad = false;
        }
        // rightPanelIndicator.tintColor = rightPanel.activeSelf ? Color.gray : Color.black;
        leftPanelIndicator.GetComponent<Image>().color = leftPanel.activeSelf ? Color.gray : Color.black;
        rightPanelIndicator.GetComponent<Image>().color = rightPanel.activeSelf ? Color.gray : Color.black;

    }
    void Update()
    {
        if (IsAnyJoystickButton() || IsJoystickMoving())
        {
            isUsingGamepad = true;
        }
        if (Input.anyKeyDown || Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f)
        {
            if (!IsAnyJoystickButton())
            {
                isUsingGamepad = false;
            }
        }

        leftPanelIndicator.GetComponent<Image>().color = leftPanel.activeSelf ? Color.gray : Color.black;
        rightPanelIndicator.GetComponent<Image>().color = rightPanel.activeSelf ? Color.gray : Color.black;

        if (isUsingGamepad)
        {
            sprite1.SetActive(true);
            sprite2.SetActive(true);
        }
        else
        {
            sprite1.SetActive(false);
            sprite2.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton5))
        {
            if (leftPanel.activeSelf)
            {
                    
                leftPanel.SetActive(false);
                rightPanel.SetActive(true);
            }
            else
            {
                leftPanel.SetActive(true);
                rightPanel.SetActive(false);
            }
            setCursorAt();

        }
        if (Input.GetKeyDown(KeyCode.JoystickButton4))
        {
            if (rightPanel.activeSelf)
            {
                rightPanel.SetActive(false);
                leftPanel.SetActive(true);
            }
            else
            {
                rightPanel.SetActive(true);
                leftPanel.SetActive(false);
            }
            setCursorAt();
        }
    }
    public void manettePanel()
    {
        leftPanel.SetActive(true);
        rightPanel.SetActive(false);
    }

    public void clavierPanel()
    {
        leftPanel.SetActive(false);
        rightPanel.SetActive(true);
    }

    void setCursorAt()
    {
        MainMenuScript.instance.SetCursorAt(leftPanel.activeSelf ? defaultleftPanelButton : defaultrightPanelButton);
        MainMenuScript.instance.SetCursorAt(rightPanel.activeSelf ? defaultrightPanelButton : defaultleftPanelButton);
    }


    bool IsAnyJoystickButton()
    {
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                return true;
        }
        return false;
    }

    bool IsJoystickMoving()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.2f ||
               Mathf.Abs(Input.GetAxis("Vertical")) > 0.2f;
    }
}
