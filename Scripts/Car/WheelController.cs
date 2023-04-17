
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonHoverBall.Car
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WheelController : UdonSharpBehaviour
    {
        [Header("Colliders")]
        [SerializeField] WheelCollider m_colliderFR; public WheelCollider ColliderFR
        {
            get => m_colliderFR;
        }
        [SerializeField] WheelCollider m_colliderFL; public WheelCollider ColliderFL
        {
            get => m_colliderFL;
        }
        [SerializeField] WheelCollider m_colliderBR; public WheelCollider ColliderBR
        {
            get => m_colliderBR;
        }
        [SerializeField] WheelCollider m_colliderBL; public WheelCollider ColliderBL
        {
            get => m_colliderBL;
        }

        [Header("Meshes")]
        [SerializeField] Transform m_meshFR;
        [SerializeField] Transform m_meshFL;
        [SerializeField] Transform m_meshBR;
        [SerializeField] Transform m_meshBL;


        [Header("Info")]
        [SerializeField] bool m_isGrounded = true; public bool IsGrounded
        {
            get => m_colliderFR.isGrounded || m_colliderFL.isGrounded || m_colliderBR.isGrounded || m_colliderBL.isGrounded;
        }
        void FixedUpdate()
        {
            AnimateWheels();
        }

        void AnimateWheels()
        {
            AnimateWheel(m_colliderFR, m_meshFR);
            AnimateWheel(m_colliderFL, m_meshFL);
            AnimateWheel(m_colliderBR, m_meshBR);
            AnimateWheel(m_colliderBL, m_meshBL);
        }

        void AnimateWheel(WheelCollider collider, Transform mesh)
        {
            var pos = mesh.position;
            var rot = mesh.rotation;
            collider.GetWorldPose(out pos, out rot);
            mesh.position = pos;
            mesh.rotation = rot;
        }

    }

}