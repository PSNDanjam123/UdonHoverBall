
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonHoverBall.Car
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InputController : UdonSharpBehaviour
    {
        [Header("Info")]
        bool m_useVR = true;
        [Header("Inputs")]
        [SerializeField, Range(0, 1)] float m_leftTrigger = 0.0f; public float LeftTrigger
        {
            private set => m_leftTrigger = value;
            get => m_leftTrigger;
        }
        [SerializeField, Range(0, 1)] float m_rightTrigger = 0.0f; public float RightTrigger
        {
            private set => m_rightTrigger = value;
            get => m_rightTrigger;
        }
        [SerializeField, Range(-1, 1)] float m_leftThumbstickHorizontal = 0.0f; public float LeftThumbstickHorizontal
        {
            private set => m_leftThumbstickHorizontal = value;
            get => m_leftThumbstickHorizontal;
        }
        [SerializeField, Range(-1, 1)] float m_rightThumbstickHorizontal = 0.0f; public float RightThumbstickHorizontal
        {
            private set => m_rightThumbstickHorizontal = value;
            get => m_rightThumbstickHorizontal;
        }
        [SerializeField, Range(-1, 1)] float m_leftThumbstickVertical = 0.0f; public float LeftThumbstickVertical
        {
            private set => m_leftThumbstickVertical = value;
            get => m_leftThumbstickVertical;
        }
        [SerializeField, Range(-1, 1)] float m_rightThumbstickVertical = 0.0f; public float RightThumbstickVertical
        {
            private set => m_rightThumbstickVertical = value;
            get => m_rightThumbstickVertical;
        }
        [SerializeField] bool m_leftButton = false; public bool LeftButton
        {
            private set => m_leftButton = value;
            get => m_leftButton;
        }
        [SerializeField] bool m_rightButton = false; public bool RightButton
        {
            private set => m_rightButton = value;
            get => m_rightButton;
        }

        void Start()
        {
            m_useVR = Networking.LocalPlayer.IsUserInVR();
        }

        void Update()
        {
            if (m_useVR)
            {
                GetVRInputs();
            }
            else
            {
                GetDesktopInputs();
            }
        }

        void GetVRInputs()
        {
            // Naming
            var prefix = "Oculus_CrossPlatform_";
            var left = prefix + "Primary";
            var right = prefix + "Secondary";

            // Trigger
            LeftTrigger = Input.GetAxis(left + "IndexTrigger");
            RightTrigger = Input.GetAxis(right + "IndexTrigger");

            // Thumbstick
            LeftThumbstickHorizontal = Input.GetAxis(left + "ThumbstickHorizontal");
            RightThumbstickHorizontal = Input.GetAxis(right + "ThumbstickHorizontal");

            // Buttons
            LeftButton = Input.GetButton(prefix + "Button2");
            RightButton = Input.GetButton(prefix + "Button4");
        }

        void GetDesktopInputs()
        {
            var responsiveness = 10.0f;
            var mouseScaling = 10.0f;

            // Trigger
            LeftTrigger = HandleKMInputLerp(KeyCode.S, 1.0f, 0.0f, LeftTrigger, responsiveness);
            RightTrigger = HandleKMInputLerp(KeyCode.W, 1.0f, 0.0f, RightTrigger, responsiveness);

            // Thumbstick
            var left = Input.GetKey(KeyCode.A);
            LeftThumbstickHorizontal = HandleKMInputLerp(left ? KeyCode.A : KeyCode.D, left ? -1.0f : 1.0f, 0.0f, LeftThumbstickHorizontal, responsiveness);
            RightThumbstickHorizontal = Mathf.Lerp(Mathf.Clamp(Input.GetAxis("Mouse X") / mouseScaling, -1.0f, 1.0f), RightThumbstickHorizontal, Time.fixedDeltaTime * responsiveness);
            RightThumbstickVertical = Mathf.Lerp(Mathf.Clamp(Input.GetAxis("Mouse Y") / mouseScaling, -1.0f, 1.0f), RightThumbstickVertical, Time.fixedDeltaTime * responsiveness);

            // Buttons
            RightButton = Input.GetKey(KeyCode.Space);
        }
        private float HandleKMInputLerp(KeyCode keyCode, float input, float nonInput, float current, float responsiveness = 1.0f)
        {
            if (!Input.GetKey(keyCode))
            {
                input = nonInput;
            }
            return Mathf.Lerp(current, input, Time.fixedDeltaTime * responsiveness);
        }
    }
}
