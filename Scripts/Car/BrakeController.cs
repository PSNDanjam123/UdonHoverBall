
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonHoverBall.Car
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BrakeController : UdonSharpBehaviour
    {
        [Header("Settings")]
        [SerializeField] float m_brakeForce = 20.0f;
        [Header("Scripts")]
        [SerializeField] WheelController m_wheelController;

        [Header("Info")]
        [SerializeField] float m_brakePosition = 0.0f; public float BrakePosition
        {
            set => m_brakePosition = value;
            get => m_brakePosition;
        }

        float m_previousBrakePosition = 0.0f;

        void Start()
        {
            m_previousBrakePosition = m_brakePosition;
        }

        void FixedUpdate()
        {
            ApplyBrakes();
        }

        void ApplyBrakes()
        {
            if (m_brakePosition == m_previousBrakePosition)
            {
                return;
            }
            m_previousBrakePosition = m_brakePosition;
            var amount = m_brakePosition * m_brakeForce;
            m_wheelController.ColliderFL.brakeTorque = amount;
            m_wheelController.ColliderFR.brakeTorque = amount;
            m_wheelController.ColliderBL.brakeTorque = amount;
            m_wheelController.ColliderBR.brakeTorque = amount;
        }
    }
}
