
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

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

    void Start()
    {
        m_rigidBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        handleInput();
        wheeColliderUpdateRotation(m_wheelColliderFL, m_wheelFL, true);
        wheeColliderUpdateRotation(m_wheelColliderFR, m_wheelFR, false);
        wheeColliderUpdateRotation(m_wheelColliderBL, m_wheelBL, true);
        wheeColliderUpdateRotation(m_wheelColliderBR, m_wheelBR, false);
    }

    private void handleInput()
    {
        var right = (m_cameraController.Rotation * Vector3.right).normalized;
        var forward = Vector3.Cross(right, Vector3.up).normalized;
        if (Input.GetKey(KeyCode.W))
        {
            m_rigidBody.AddForce(forward * m_force, ForceMode.Acceleration);
        }
        if (Input.GetKey(KeyCode.S))
        {
            m_rigidBody.AddForce(-forward * m_force, ForceMode.Acceleration);
        }
        if (Input.GetKey(KeyCode.A))
        {
            m_rigidBody.AddForce(-right * m_force, ForceMode.Acceleration);
        }
        if (Input.GetKey(KeyCode.D))
        {
            m_rigidBody.AddForce(right * m_force, ForceMode.Acceleration);
        }
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
}
