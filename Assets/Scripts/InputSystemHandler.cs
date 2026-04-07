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

    InputAction glideTurnAction;
    public float inputGlideTurnDir;

    InputAction glideUpAction;
    public int inputGlideUp;

    InputAction glideDownAction;
    public int inputGlideDown;


    public int inputGlideUpDownDir;
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
        inputGlideUpDownDir = inputGlideUp + inputGlideDown;
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

        // L'action "GlideTurn"
        glideTurnAction = reference.FindAction("GlideTurn");
        glideTurnAction.performed += inputInfo => inputGlideTurnDir = inputInfo.ReadValue<float>();
        glideTurnAction.canceled += inputInfo => inputGlideTurnDir = 0f;

        // L'action "GlideUp"
        glideUpAction = reference.FindAction("GlideUp");
        glideUpAction.performed += inputInfo => inputGlideUp = -1;
        glideUpAction.canceled += inputInfo => inputGlideUp = 0;

        // L'action "GlideUp"
        glideDownAction = reference.FindAction("GlideDown");
        glideDownAction.performed += inputInfo => inputGlideDown = 1;
        glideDownAction.canceled += inputInfo => inputGlideDown = 0;

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
