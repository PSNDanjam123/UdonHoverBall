
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3;
using VRC.Udon.Common;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody)), UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class CarController : UdonSharpBehaviour
{
    Rigidbody m_legacy_rigidBody;

    [SerializeField] float m_legacy_force = 2.0f;
    [SerializeField] CameraController m_legacy_cameraController;

    [Header("Wheels")]
    [SerializeField] WheelCollider m_legacy_wheelColliderFR;
    [SerializeField] WheelCollider m_legacy_wheelColliderFL;
    [SerializeField] WheelCollider m_legacy_wheelColliderBR;
    [SerializeField] WheelCollider m_legacy_wheelColliderBL;
    [SerializeField] GameObject m_legacy_wheelFR;
    [SerializeField] GameObject m_legacy_wheelFL;
    [SerializeField] GameObject m_legacy_wheelBR;
    [SerializeField] GameObject m_legacy_wheelBL;

    [SerializeField, UdonSynced(UdonSyncMode.Smooth)] float m_legacy_steering = 0.0f;
    [SerializeField] float m_legacy_throttle = 0.0f;
    [SerializeField] float m_legacy_brake = 0.0f;
    [SerializeField] float m_legacy_jumpAmount = 1000f;
    [SerializeField] float m_legacy_rocketBoost = 0.0f;
    [SerializeField] float m_legacy_rocketFuel = 10.0f;
    [SerializeField] float m_legacy_maxRocketFuel = 10.0f;
    [SerializeField] float m_legacy_rocketBoostAmount = 100f;

    [SerializeField] float m_legacy_engineTorque = 100f;
    [SerializeField] float m_legacy_brakeTorque = 100f;
    [SerializeField] float m_legacy_maxSteeringAngle = 70f;

    [SerializeField, UdonSynced] string m_legacy_owner;

    VRCPlayerApi playerApi;

    public string Owner
    {
        private set => m_legacy_owner = value;
        get => m_legacy_owner;
    }

    float RocketBoost
    {
        set => m_legacy_rocketBoost = value;
        get => m_legacy_rocketBoost;
    }

    float RocketFuel
    {
        set => m_legacy_rocketFuel = value;
        get => m_legacy_rocketFuel;
    }
    float MaxRocketFuel
    {
        set => m_legacy_maxRocketFuel = value;
        get => m_legacy_maxRocketFuel;
    }

    float Throttle
    {
        set
        {
            m_legacy_throttle = value;
        }
        get => m_legacy_throttle;
    }

    float Brake
    {
        set
        {
            m_legacy_brake = value;
        }
        get => m_legacy_brake;
    }

    float Steering
    {
        set
        {
            m_legacy_steering = value;
        }
        get => m_legacy_steering;
    }

    void Start()
    {
        m_legacy_rigidBody = GetComponent<Rigidbody>();
        m_legacy_rigidBody.centerOfMass = -Vector3.up * 0.3f;
        RocketFuel = MaxRocketFuel;
        playerApi = Networking.LocalPlayer;
    }

    void FixedUpdate()
    {
        updateWheelRotations();
        if (!ControlsCar())
        {
            return;
        }
        handleInput();
        applyThrottle();
        applyBrake();
        applySteering();
        applyRocketBoost();
    }

    public override void Interact()
    {
        SetOwner();
        m_legacy_owner = Networking.LocalPlayer.displayName;
        m_legacy_cameraController.SetCar(gameObject.transform);
        m_legacy_cameraController.Enable();
        Networking.LocalPlayer.TeleportTo(Vector3.up * 1000f, Quaternion.identity);
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        if (!IsOwner())
        {
            return;
        }
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (!ControlsCar())
        {
            return; // not them
        }
        m_legacy_owner = null;
        m_legacy_cameraController.Disable();
        m_legacy_cameraController.UnsetCar();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (ControlsCar())
        {
            return;
        }
        if (!IsOwner())
        {
            return;
        }
        m_legacy_owner = null;
        RequestSerialization();
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (!ControlsCar())
        {
            return; // not them
        }
        if (value)
        {
            m_legacy_rigidBody.AddForce(Vector3.up * m_legacy_jumpAmount, ForceMode.Acceleration);
        }
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (!ControlsCar())
        {
            return; // not them
        }
        if (!playerApi.IsUserInVR())
        {
            return; // different controls for PC
        }

        var throttle = 0.0f;

        if (args.handType == HandType.RIGHT)
        {
            // forward
            throttle = 1.0f;
        }
        else
        {
            // reverse
            throttle = -1.0f;
        }
        Throttle = throttle;
    }

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        if (!ControlsCar())
        {
            return; // not them
        }
        Steering = value;
    }

    public override void InputLookHorizontal(float value, UdonInputEventArgs args)
    {
        var multiplier = 5.0f;
        m_legacy_rigidBody.AddTorque(Vector3.up * value * multiplier, ForceMode.Acceleration);
    }

    public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        var multiplier = 5.0f;
        var right = Vector3.Cross(m_legacy_rigidBody.transform.forward, Vector3.up);
        m_legacy_rigidBody.AddTorque(Vector3.right * value * multiplier, ForceMode.Acceleration);
    }

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        if (!ControlsCar())
        {
            return; // not them
        }
        if (!playerApi.IsUserInVR())
        {
            return;
        }
        var rocketBoost = 0.0f;
        var val = 0.0f;
        if (value)
        {
            val = 1.0f;
        }
        rocketBoost = val;
        RocketBoost = rocketBoost;
    }

    private void applyRocketBoost()
    {
        if (RocketBoost < 0.1 && RocketFuel < MaxRocketFuel)
        {
            RocketFuel += 0.05f;
            return;
        }
        if (RocketFuel <= 0)
        {
            return;
        }
        RocketFuel -= 0.05f;
        m_legacy_rigidBody.AddForce(RocketBoost * m_legacy_rocketBoostAmount * transform.forward, ForceMode.Acceleration);
    }

    private void applySteering()
    {
        var responsiveness = 5.0f;
        float max = m_legacy_maxSteeringAngle * (1 - Mathf.Min(m_legacy_rigidBody.velocity.magnitude / 50, 0.9f));
        var value = Mathf.Lerp(m_legacy_wheelColliderFL.steerAngle, Steering * max, Time.fixedDeltaTime * responsiveness);
        m_legacy_wheelColliderFL.steerAngle = value;
        m_legacy_wheelColliderFR.steerAngle = value;
    }

    private void applyThrottle()
    {
        var newTorque = Throttle * m_legacy_engineTorque;
        m_legacy_wheelColliderFL.motorTorque = newTorque;
        m_legacy_wheelColliderFR.motorTorque = newTorque;
        m_legacy_wheelColliderBL.motorTorque = newTorque;
        m_legacy_wheelColliderBR.motorTorque = newTorque;
    }
    private void applyBrake()
    {
        var newTorque = Brake * m_legacy_brakeTorque;
        m_legacy_wheelColliderFL.brakeTorque = newTorque;
        m_legacy_wheelColliderFR.brakeTorque = newTorque;
        m_legacy_wheelColliderBL.brakeTorque = newTorque;
        m_legacy_wheelColliderBR.brakeTorque = newTorque;
    }


    private void handleInput()
    {
        if (playerApi.IsUserInVR())
        {
            return; // only for non VR
        }
        updateThrottle();
        updateBrake();
        updateRocketBoost();
    }

    private void updateRocketBoost()
    {
        var rocketBoost = 0.0f;
        var speed = 2.0f;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            rocketBoost = Mathf.Lerp(RocketBoost, 1.0f, Time.fixedDeltaTime * speed);
        }
        RocketBoost = rocketBoost;
    }

    private void updateWheelRotations()
    {
        wheeColliderUpdateRotation(m_legacy_wheelColliderFL, m_legacy_wheelFL, true);
        wheeColliderUpdateRotation(m_legacy_wheelColliderFR, m_legacy_wheelFR, false);
        wheeColliderUpdateRotation(m_legacy_wheelColliderBL, m_legacy_wheelBL, true);
        wheeColliderUpdateRotation(m_legacy_wheelColliderBR, m_legacy_wheelBR, false);
    }

    private void updateThrottle()
    {
        var throttle = 0.0f;
        var speed = 2.0f;
        if (Input.GetKey(KeyCode.W))
        {
            throttle = Mathf.Lerp(Throttle, 1.0f, Time.fixedDeltaTime * speed);
        }
        if (Input.GetKey(KeyCode.S))
        {
            throttle = Mathf.Lerp(Throttle, -1.0f, Time.fixedDeltaTime * speed);
        }
        Throttle = throttle;
    }
    private void updateBrake()
    {
        var brake = 0.0f;
        var speed = 2.0f;
        if (Input.GetKey(KeyCode.Space))
        {
            brake = Mathf.Lerp(Brake, 1.0f, Time.fixedDeltaTime * speed);
        }
        Brake = brake;
    }

    private void wheeColliderUpdateRotation(WheelCollider collider, GameObject wheel, bool left)
    {
        var pos = wheel.transform.position;
        var quat = wheel.transform.rotation;

        collider.GetWorldPose(out pos, out quat);

        if (left)
        {
            quat *= Quaternion.AngleAxis(180, Vector3.up);
        }

        wheel.transform.position = pos;
        wheel.transform.rotation = quat;
    }

    bool IsOwner(VRCPlayerApi playerApi = null)
    {
        if (playerApi == null)
        {
            playerApi = Networking.LocalPlayer;
        }
        return Networking.IsOwner(playerApi, gameObject);
    }

    bool ControlsCar(VRCPlayerApi playerApi = null)
    {
        if (playerApi == null)
        {
            playerApi = Networking.LocalPlayer;
        }
        return m_legacy_owner == playerApi.displayName;
    }

    void SetOwner()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }
}
