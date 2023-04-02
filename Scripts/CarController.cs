
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;
using VRC.Udon;
using VRC.SDK3;
using VRC.Udon.Common;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody)), UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class CarController : UdonSharpBehaviour
{
    VRCPlayerApi m_playerApi;
    Rigidbody m_rigidBody;

    [SerializeField, UdonSynced] string m_driver;

    [Header("Components")]
    [SerializeField] CameraController m_camera;
    [SerializeField] WheelCollider[] m_wheelColliders;
    [SerializeField] Transform[] m_wheelMeshes;

    [Header("Settings")]

    [SerializeField, Range(0, 100000)] float m_mass = 1000; public float mass
    {
        set
        {
            m_mass = value;
            if (m_rigidBody.mass != value)
            {
                m_rigidBody.mass = value;
            }
        }
        get => m_mass;
    }
    [SerializeField] AnimationCurve m_enginePower;
    [SerializeField] float m_engineMinRPM = 1000.0f;
    [SerializeField, Range(0, 100)] float m_maxSteeringAngle = 45f;
    [SerializeField] float m_jumpForce = 100f;
    [Header("Engine")]
    [SerializeField, Range(0, 1)] float m_throttle = 0.0f;
    [SerializeField] float m_engineRPM = 0.0f;
    [SerializeField] float m_engineTorque = 0.0f;

    [Header("Inputs")]
    [SerializeField, Range(0, 1)] float m_inputLeftTrigger = 0.0f; public float inputLeftTrigger
    {
        private set => m_inputLeftTrigger = value;
        get => m_inputLeftTrigger;
    }
    [SerializeField, Range(0, 1)] float m_inputRightTrigger = 0.0f; public float inputRightTrigger
    {
        private set => m_inputRightTrigger = value;
        get => m_inputRightTrigger;
    }
    [SerializeField, Range(-1, 1)] float m_inputLeftThumbstickHorizontal = 0.0f; public float inputLeftThumbstickHorizontal
    {
        private set => m_inputLeftThumbstickHorizontal = value;
        get => m_inputLeftThumbstickHorizontal;
    }
    [SerializeField, Range(-1, 1)] float m_inputRightThumbstickHorizontal = 0.0f; public float inputRightThumbstickHorizontal
    {
        private set => m_inputRightThumbstickHorizontal = value;
        get => m_inputRightThumbstickHorizontal;
    }
    [SerializeField, Range(-1, 1)] float m_inputLeftThumbstickVertical = 0.0f; public float inputLeftThumbstickVertical
    {
        private set => m_inputLeftThumbstickVertical = value;
        get => m_inputLeftThumbstickVertical;
    }
    [SerializeField, Range(-1, 1)] float m_inputRightThumbstickVertical = 0.0f; public float inputRightThumbstickVertical
    {
        private set => m_inputRightThumbstickVertical = value;
        get => m_inputRightThumbstickVertical;
    }
    [SerializeField] bool m_inputLeftButton = false; public bool inputLeftButton
    {
        private set => m_inputLeftButton = value;
        get => m_inputLeftButton;
    }
    [SerializeField] bool m_inputRightButton = false; public bool inputRightButton
    {
        private set => m_inputRightButton = value;
        get => m_inputRightButton;
    }

    void Start()
    {
        m_playerApi = Networking.LocalPlayer;
        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.useGravity = true;
        initSettings();
    }

    void Update()
    {
        initSettings();
        getInputs();
    }


    void FixedUpdate()
    {
        applyThrottle();
        applyBrake();
        applySteering();
        applyJump();
        animateWheels();
        calculateEnginePower();
    }

    private void applyJump()
    {
        var jump = inputRightButton;
        if (!jump)
        {
            return;
        }
        m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Acceleration);
    }

    private void applyThrottle()
    {
        m_throttle = inputRightTrigger;
    }

    private void calculateEnginePower()
    {
        m_engineRPM = Mathf.Lerp(m_engineRPM, m_engineMinRPM + Mathf.Abs(wheelRPM()), Time.fixedDeltaTime);
        m_engineTorque = m_enginePower.Evaluate(m_engineRPM) * m_throttle;
        foreach (var collider in m_wheelColliders)
        {
            collider.motorTorque = m_engineTorque / m_wheelColliders.Length;
        }
    }

    private float wheelRPM()
    {
        float sum = 0;
        foreach (var collider in m_wheelColliders)
        {
            sum += collider.rpm;
        }
        return sum / m_wheelColliders.Length;
    }

    private void applyBrake()
    {
        var brake = inputLeftTrigger;

        foreach (var collider in m_wheelColliders)
        {
            collider.brakeTorque = brake * 100;
        }
    }

    private void applySteering()
    {
        var frontL = m_wheelColliders[0];
        var frontR = m_wheelColliders[1];

        frontL.steerAngle = inputLeftThumbstickHorizontal * m_maxSteeringAngle;
        frontR.steerAngle = inputLeftThumbstickHorizontal * m_maxSteeringAngle;
    }
    private void getInputs()
    {
        // VR Inputs
        if (m_playerApi.IsUserInVR())
        {
            getVRInputs();
            return;
        }

        getKMInputs();
    }

    private void getVRInputs()
    {
        // Naming
        var prefix = "Oculus_CrossPlatform_";
        var left = prefix + "Primary";
        var right = prefix + "Secondary";

        // Trigger
        inputLeftTrigger = Input.GetAxis(left + "IndexTrigger");
        inputRightTrigger = Input.GetAxis(right + "IndexTrigger");

        // Thumbstick
        inputLeftThumbstickHorizontal = Input.GetAxis(left + "ThumbstickHorizontal");
        inputRightThumbstickHorizontal = Input.GetAxis(right + "ThumbstickHorizontal");

        // Buttons
        inputLeftButton = Input.GetButton(prefix + "Button2");
        inputRightButton = Input.GetButton(prefix + "Button4");
    }

    private void getKMInputs()
    {
        var responsiveness = 10.0f;
        var mouseScaling = 10.0f;

        // Trigger
        inputLeftTrigger = handleKMInputLerp(KeyCode.S, 1.0f, 0.0f, inputLeftTrigger, responsiveness);
        inputRightTrigger = handleKMInputLerp(KeyCode.W, 1.0f, 0.0f, inputRightTrigger, responsiveness);

        // Thumbstick
        var left = Input.GetKey(KeyCode.A);
        inputLeftThumbstickHorizontal = handleKMInputLerp(left ? KeyCode.A : KeyCode.D, left ? -1.0f : 1.0f, 0.0f, inputLeftThumbstickHorizontal, responsiveness);
        inputRightThumbstickHorizontal = Mathf.Lerp(Mathf.Clamp(Input.GetAxis("Mouse X") / mouseScaling, -1.0f, 1.0f), inputRightThumbstickHorizontal, Time.fixedDeltaTime * responsiveness);
        inputRightThumbstickVertical = Mathf.Lerp(Mathf.Clamp(Input.GetAxis("Mouse Y") / mouseScaling, -1.0f, 1.0f), inputRightThumbstickVertical, Time.fixedDeltaTime * responsiveness);

        // Buttons
        inputRightButton = Input.GetKey(KeyCode.Space);
    }

    private float handleKMInputLerp(KeyCode keyCode, float input, float nonInput, float current, float responsiveness = 1.0f)
    {
        if (!Input.GetKey(keyCode))
        {
            input = nonInput;
        }
        return Mathf.Lerp(current, input, Time.fixedDeltaTime * responsiveness);
    }

    private void initSettings()
    {
        mass = m_mass;
    }

    private void animateWheels()
    {
        for (var i = 0; i < m_wheelColliders.Length; i++)
        {
            var collider = m_wheelColliders[i];
            var mesh = m_wheelMeshes[i];
            var pos = mesh.position;
            var rot = mesh.rotation;
            collider.GetWorldPose(out pos, out rot);
            mesh.transform.position = pos;
            mesh.transform.rotation = rot;
        }
    }

    public override void Interact()
    {
        if (!IsDriver())
        {
            Enter();
        }
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (m_driver == player.displayName)
        {
            Exit();
        }
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!IsOwner() || m_driver != player.displayName)
        {
            return;
        }
        m_driver = "";
        RequestSerialization();
    }

    void Enter()
    {
        SetOwner();
        m_driver = Networking.LocalPlayer.displayName;
        m_camera.SetCar(gameObject.transform);
        m_camera.Enable();
        Networking.LocalPlayer.TeleportTo(Vector3.up * 1000f, Quaternion.identity);
        RequestSerialization();
    }

    void Exit()
    {
        SetOwner();
        m_camera.Disable();
        m_camera.SetCar(null);
        m_driver = "";
        RequestSerialization();
    }

    bool IsDriver(VRCPlayerApi playerApi = null)
    {
        if (playerApi == null)
        {
            playerApi = m_playerApi;
        }
        return m_driver == playerApi.displayName;
    }

    bool IsOwner(VRCPlayerApi playerApi = null)
    {
        if (playerApi == null)
        {
            playerApi = m_playerApi;
        }
        return Networking.IsOwner(playerApi, gameObject);
    }

    void SetOwner(VRCPlayerApi playerApi = null)
    {
        if (playerApi == null)
        {
            playerApi = m_playerApi;
        }
        Networking.SetOwner(playerApi, gameObject);
    }
}
