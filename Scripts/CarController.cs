
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
    [SerializeField] UdonHoverBall.Car.InputController m_inputController;
    [SerializeField] UdonHoverBall.Car.WheelController m_wheelController;
    [SerializeField] UdonHoverBall.Car.EngineController m_engineController;
    [SerializeField] UdonHoverBall.Car.BrakeController m_brakeController;
    [SerializeField] MeshRenderer m_body;
    [SerializeField] MeshRenderer m_trim;
    [SerializeField] Light m_neon;

    Material m_bodyMaterial;

    Material m_trimMaterial;

    [Header("Settings")]
    [SerializeField] byte m_team = 1;
    [SerializeField] Color m_team1Color = Color.blue;
    [SerializeField] Color m_team2Color = Color.red;

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
    [SerializeField, Range(0, 100)] float m_maxSteeringAngle = 45f;
    [SerializeField] float m_jumpForce = 100f;

    void Start()
    {
        m_playerApi = Networking.LocalPlayer;
        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.useGravity = true;

        m_bodyMaterial = m_body.material;
        m_trimMaterial = m_trim.material;

        m_bodyMaterial.SetColor("_Color", m_team == 1 ? m_team1Color : m_team2Color);
        m_trimMaterial.SetColor("_EmissionColor", m_team == 1 ? m_team1Color : m_team2Color);
        m_neon.color = m_team == 1 ? m_team1Color : m_team2Color;

        initSettings();
    }

    void Update()
    {
        initSettings();
    }

    void FixedUpdate()
    {
        m_engineController.ThrottlePosition = m_inputController.RightTrigger;
        m_brakeController.BrakePosition = m_inputController.LeftTrigger;
        applySteering();
        applyJump();
        calculateDownForce();
        applyInertialDampening();
        applyForceRotations();
    }

    private void applyForceRotations()
    {
        if (m_wheelController.IsGrounded)
        {
            return;
        }
        m_rigidBody.AddTorque(Vector3.up * 100 * m_inputController.RightThumbstickHorizontal, ForceMode.Acceleration);
        m_rigidBody.AddTorque(m_rigidBody.transform.right * 100 * m_inputController.RightThumbstickVertical, ForceMode.Acceleration);
        m_rigidBody.AddTorque(m_rigidBody.transform.forward * 10 * -m_inputController.LeftThumbstickHorizontal, ForceMode.Acceleration);
    }

    private void applyInertialDampening()
    {
        m_rigidBody.AddTorque(-m_rigidBody.angularVelocity * 0.7f, ForceMode.Acceleration);
    }

    private void applyJump()
    {
        var jump = m_inputController.RightButton;
        if (!jump)
        {
            return;
        }
        m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Acceleration);
        m_rigidBody.angularVelocity -= m_rigidBody.angularVelocity * 0.7f;
    }

    private WheelCollider[] getWheelColliders()
    {
        WheelCollider[] colliders = {
            m_wheelController.ColliderFL,
            m_wheelController.ColliderFR,
            m_wheelController.ColliderBL,
            m_wheelController.ColliderBR,
        };
        return colliders;
    }

    private void calculateDownForce()
    {
        // write better code here
        var magnitude = m_rigidBody.velocity.magnitude;
        var force = magnitude * -Vector3.up;
        m_rigidBody.AddForce(force, ForceMode.Acceleration);
    }

    private void applySteering()
    {
        var frontL = m_wheelController.ColliderFL;
        var frontR = m_wheelController.ColliderFR;

        var maxSteeringAngle = m_maxSteeringAngle * (1 - Mathf.Clamp(m_rigidBody.velocity.magnitude / 50f, 0.1f, 1.0f));

        frontL.steerAngle = m_inputController.LeftThumbstickHorizontal * maxSteeringAngle;
        frontR.steerAngle = m_inputController.LeftThumbstickHorizontal * maxSteeringAngle;
    }

    private void initSettings()
    {
        mass = m_mass;
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
