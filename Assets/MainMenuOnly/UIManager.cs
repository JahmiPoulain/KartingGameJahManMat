using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] GameObject pausePanel;

    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        pauseAction.action.performed += OnPause;
        pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        pauseAction.action.performed -= OnPause;
        pauseAction.action.Disable();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
    }


    public void quitGame()
    {
        SceneManager.LoadScene("MainMenu2_0");
    }

}