
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonHoverBall.Car
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SteeringController : UdonSharpBehaviour
    {
        [Header("Scripts")]
        [SerializeField] WheelController m_wheelController;

        [Header("Settings")]
        [SerializeField] float m_maxAngle = 45f; public float MaxAngle
        {
            get => m_maxAngle;
        }

        [Header("Info")]
        [SerializeField] float m_currentAngle = 0.0f; public float CurrentAngle
        {
            set => m_currentAngle = value;
            get => m_currentAngle;
        }

        float m_previousAngle = 0.0f;

        WheelCollider m_colliderFL;
        WheelCollider m_colliderFR;

        void Start()
        {
            m_colliderFL = m_wheelController.ColliderFL;
            m_colliderFR = m_wheelController.ColliderFR;
            m_currentAngle = m_previousAngle;
        }

        void FixedUpdate()
        {
            ApplySteering();
        }

        void ApplySteering()
        {
            if (m_currentAngle == m_previousAngle)
            {
                return;
            }

            m_previousAngle = m_currentAngle;
            m_colliderFL.steerAngle = m_currentAngle;
            m_colliderFR.steerAngle = m_currentAngle;
        }
    }
}
