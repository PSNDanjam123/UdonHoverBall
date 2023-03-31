
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class CameraController : UdonSharpBehaviour
{
    [SerializeField] Transform m_car;
    [SerializeField] Transform m_ball;

    [SerializeField] Vector3 m_viewOffset = new Vector3(0, 1, -3);
    Vector3 m_Position;
    Quaternion m_Rotation;

    Camera m_camera;

    public Vector3 Position
    {
        private set => gameObject.transform.position = m_Position = value;
        get => m_Position;
    }

    public Quaternion Rotation
    {
        private set => gameObject.transform.rotation = m_Rotation = value;
        get => m_Rotation;
    }

    void Start()
    {
        m_camera = GetComponentInChildren<Camera>();
        m_camera.depthTextureMode = DepthTextureMode.Depth;
        m_camera.stereoTargetEye = StereoTargetEyeMask.Both;
    }

    void Update()
    {
        updatePosition();
        updateRotation();
        handleInput();
    }

    void Enable()
    {
        m_camera.enabled = true;
    }

    void Disable()
    {
        m_camera.enabled = false;
    }

    private void handleInput()
    {
        if (Input.GetKey(KeyCode.C))
        {
            Enable();
        }
        if (Input.GetKey(KeyCode.V))
        {
            Disable();
        }
    }

    private void updatePosition()
    {
        var look = (m_ball.position - m_car.position).normalized;

        var up = Vector3.up;
        var right = Vector3.Cross(up, look).normalized;
        var forward = Vector3.Cross(right, up).normalized;

        var offset = Vector3.zero;
        offset += right * m_viewOffset.x;
        offset += forward * m_viewOffset.z;
        offset += up * m_viewOffset.y;

        var speed = 20f;
        var position = m_car.position + offset;
        Position = Vector3.Lerp(Position, position, Time.fixedDeltaTime * speed);
    }

    private void updateRotation()
    {
        var speed = 20f;
        var vector = Vector3.Lerp(m_car.position, m_ball.position, 0.5f) - transform.position;
        var rotation = Quaternion.LookRotation(vector);
        Rotation = Quaternion.Lerp(Rotation, rotation, Time.fixedDeltaTime * speed);
    }
}
