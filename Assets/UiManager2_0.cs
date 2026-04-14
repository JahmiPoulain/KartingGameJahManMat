using UnityEngine;

public class UiManager2_0 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void TogglePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }



}
