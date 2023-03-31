
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CarController : UdonSharpBehaviour
{
    Rigidbody m_rigidBody;

    [SerializeField] float m_force = 2.0f;
    [SerializeField] CameraController m_cameraController;

    void Start()
    {
        m_rigidBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        handleInput();
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
}
