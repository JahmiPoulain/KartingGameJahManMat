using UnityEngine;
using UnityEngine.UI;

public class UiManager2_0 : MonoBehaviour
{
    [SerializeField] private GameObject ButtonsCanva;
    [SerializeField] private GameObject loadingScreenCanva;



    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                canvas.SetActive(false);
            }

            GameObject uiPanel = GameObject.Find("UiPanel");
            if (uiPanel != null)
            {
                uiPanel.SetActive(true);
            }
        }



    }


    public void TogglePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }

    // start loadingScreen
    public void StartLoadingScreen()
    {
        // Assuming you have a loading screen GameObject in your scene
        GameObject loadingScreen = GameObject.Find("LoadingScreen");
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }
    }



}
