
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonHoverBall.Car
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EngineController : UdonSharpBehaviour
    {
        [Header("Settings")]
        [SerializeField] AnimationCurve m_powerCurve;
        [SerializeField] float m_minRPM = 1000.0f;

        [Header("Scripts")]
        [SerializeField] WheelController m_wheelController;

        [Header("Info")]
        [SerializeField] float m_currentRPM = 0.0f;
        [SerializeField] float m_currentTorque = 0.0f;
        [SerializeField] float m_throttlePosition = 0.0f; public float ThrottlePosition
        {
            set => m_throttlePosition = value;
            get => m_throttlePosition;
        }

        void Start()
        {
            m_currentRPM = 0.0f;
            m_currentTorque = 0.0f;
            m_throttlePosition = 0.0f;
        }

        void FixedUpdate()
        {
            CalculateRPM();
            CalculateTorque();
            ApplyWheelTorque();
        }

        void CalculateRPM()
        {

            // wheel RPM
            var rpm = m_wheelController.ColliderFL.rpm;
            rpm += m_wheelController.ColliderFR.rpm;
            rpm /= 2;

            m_currentRPM = Mathf.Lerp(m_currentRPM, m_minRPM + Mathf.Abs(rpm), Time.fixedDeltaTime);
        }

        void CalculateTorque()
        {
            m_currentTorque = m_powerCurve.Evaluate(m_currentRPM) * m_throttlePosition;
        }

        void ApplyWheelTorque()
        {
            m_wheelController.ColliderBL.motorTorque = m_currentTorque / 2;
            m_wheelController.ColliderBR.motorTorque = m_currentTorque / 2;
        }
    }

}