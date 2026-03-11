using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemHandler : MonoBehaviour
{
    public static InputSystemHandler instance;
    public InputActionAsset playerInputs;

    InputAction turnAction;
    public float inputTurnDir;

    InputAction forwardAction;
    public int inputForward;

    InputAction backwardAction;
    public int inputBackward;

    InputAction driftAction;
    public bool inputDrift;
    public bool inputTryDrift;

    public int inputForwardDir;
    public bool inputCameraMode;
    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(this); }
        SubscribeInputs();
    }
    void Start()
    {
        
    }

    void Update()
    {
        inputForwardDir = inputForward + inputBackward;
        
    }

    private void LateUpdate()
    {
        if (inputTryDrift) { inputTryDrift = false; }
    }
    void SubscribeInputs()
    {
        // dans l'Action Map "Player"
        InputActionMap reference = playerInputs.FindActionMap("Player");
        // L'action "Turn"
        turnAction = reference.FindAction("Turn");
        // Quand on appuie          // inputTurnDir = la valeur de l'input
        turnAction.performed += inputInfo => inputTurnDir = inputInfo.ReadValue<float>();
        // Quand on lâche          // inputTurnDir = 0f
        turnAction.canceled += inputInfo => inputTurnDir = 0f;

        // L'action "GoForward"
        forwardAction = reference.FindAction("GoForward");
        forwardAction.performed += inputInfo => inputForward = 1;
        forwardAction.canceled += inputInfo => inputForward = 0;

        // L'action "GoBackward"
        backwardAction = reference.FindAction("GoBackward");
        backwardAction.performed += inputInfo => inputBackward = -1;
        backwardAction.canceled += inputInfo => inputBackward = 0;

        // L'action "Drift"
        driftAction = reference.FindAction("Drift");
        driftAction.performed += inputInfo => inputDrift = true;
        driftAction.canceled += inputInfo => inputDrift = false;
        driftAction.started += inputInfo => inputTryDrift = true;
        // manière plus directe de faire, idéal si on veut juste faire un bool qui switch
        // Dans l'action map "Player"        Quand l'action "SwitchCamera" est faite        Le bool inputCameraMode = !inputCameraMode
        playerInputs.FindActionMap("Player").FindAction("SwitchCamera").performed += autoInputInfo => inputCameraMode = !inputCameraMode; // boutton toggle
    }
    private void OnEnable()
    {
        playerInputs.FindActionMap("Player").Enable();
    }
    private void OnDisable()
    {
        playerInputs.FindActionMap("Player").Disable();
    }
}
