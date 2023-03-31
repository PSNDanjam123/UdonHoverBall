
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class CarController : UdonSharpBehaviour
{
    Rigidbody m_rigidBody;

    [SerializeField] float m_force = 2.0f;
    [SerializeField] CameraController m_cameraController;

    [Header("Wheels")]
    [SerializeField] WheelCollider m_wheelColliderFR;
    [SerializeField] WheelCollider m_wheelColliderFL;
    [SerializeField] WheelCollider m_wheelColliderBR;
    [SerializeField] WheelCollider m_wheelColliderBL;
    [SerializeField] GameObject m_wheelFR;
    [SerializeField] GameObject m_wheelFL;
    [SerializeField] GameObject m_wheelBR;
    [SerializeField] GameObject m_wheelBL;

    [SerializeField, UdonSynced(UdonSyncMode.Smooth)] float m_steering = 0.0f;
    [SerializeField] float m_throttle = 0.0f;
    [SerializeField] float m_brake = 0.0f;
    [SerializeField] float m_jumpAmount = 1000f;
    [SerializeField] float m_rocketBoost = 0.0f;
    [SerializeField] float m_rocketFuel = 10.0f;
    [SerializeField] float m_maxRocketFuel = 10.0f;
    [SerializeField] float m_rocketBoostAmount = 100f;

    [SerializeField] float m_engineTorque = 100f;
    [SerializeField] float m_brakeTorque = 100f;
    [SerializeField] float m_maxSteeringAngle = 70f;

    [SerializeField, UdonSynced] string m_owner;

    VRCPlayerApi playerApi;

    public string Owner
    {
        private set => m_owner = value;
        get => m_owner;
    }

    float RocketBoost
    {
        set => m_rocketBoost = value;
        get => m_rocketBoost;
    }

    float RocketFuel
    {
        set => m_rocketFuel = value;
        get => m_rocketFuel;
    }
    float MaxRocketFuel
    {
        set => m_maxRocketFuel = value;
        get => m_maxRocketFuel;
    }

    float Throttle
    {
        set
        {
            m_throttle = value;
        }
        get => m_throttle;
    }

    float Brake
    {
        set
        {
            m_brake = value;
        }
        get => m_brake;
    }

    float Steering
    {
        set
        {
            m_steering = value;
        }
        get => m_steering;
    }

    void Start()
    {
        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.centerOfMass = -Vector3.up * 0.3f;
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
        m_owner = Networking.LocalPlayer.displayName;
        m_cameraController.SetCar(gameObject.transform);
        m_cameraController.Enable();
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
        m_owner = null;
        m_cameraController.Disable();
        m_cameraController.UnsetCar();
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
        m_owner = null;
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
            m_rigidBody.AddForce(Vector3.up * m_jumpAmount, ForceMode.Acceleration);
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
        m_rigidBody.AddTorque(Vector3.up * value * multiplier, ForceMode.Acceleration);
    }

    public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        var multiplier = 5.0f;
        var right = Vector3.Cross(m_rigidBody.transform.forward, Vector3.up);
        m_rigidBody.AddTorque(Vector3.right * value * multiplier, ForceMode.Acceleration);
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
        m_rigidBody.AddForce(RocketBoost * m_rocketBoostAmount * transform.forward, ForceMode.Acceleration);
    }

    private void applySteering()
    {
        var responsiveness = 5.0f;
        float max = m_maxSteeringAngle * (1 - Mathf.Min(m_rigidBody.velocity.magnitude / 50, 0.9f));
        var value = Mathf.Lerp(m_wheelColliderFL.steerAngle, Steering * max, Time.fixedDeltaTime * responsiveness);
        m_wheelColliderFL.steerAngle = value;
        m_wheelColliderFR.steerAngle = value;
    }

    private void applyThrottle()
    {
        var newTorque = Throttle * m_engineTorque;
        m_wheelColliderFL.motorTorque = newTorque;
        m_wheelColliderFR.motorTorque = newTorque;
        m_wheelColliderBL.motorTorque = newTorque;
        m_wheelColliderBR.motorTorque = newTorque;
    }
    private void applyBrake()
    {
        var newTorque = Brake * m_brakeTorque;
        m_wheelColliderFL.brakeTorque = newTorque;
        m_wheelColliderFR.brakeTorque = newTorque;
        m_wheelColliderBL.brakeTorque = newTorque;
        m_wheelColliderBR.brakeTorque = newTorque;
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
        wheeColliderUpdateRotation(m_wheelColliderFL, m_wheelFL, true);
        wheeColliderUpdateRotation(m_wheelColliderFR, m_wheelFR, false);
        wheeColliderUpdateRotation(m_wheelColliderBL, m_wheelBL, true);
        wheeColliderUpdateRotation(m_wheelColliderBR, m_wheelBR, false);
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
        return m_owner == playerApi.displayName;
    }

    void SetOwner()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }
}
