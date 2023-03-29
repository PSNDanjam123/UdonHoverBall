
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PlayerSettings : UdonSharpBehaviour
{
    private VRCPlayerApi _playerApi;

    [SerializeField] private float _walkSpeed = 2.0f;
    [SerializeField] private float _runSpeed = 4.0f;
    [SerializeField] private float _jumpImpulse = 3.0f;
    [SerializeField] private float _strafeSpeed = 2.0f;
    [SerializeField] private float _boostStrength = 0.1f;
    [SerializeField] private float _maxFuel = 10.0f;
    [SerializeField] GameObject _fuelDisplay;
    [SerializeField] Text _fuelDisplayText;
    private float _fuel = 10.0f;
    private bool _usingFuel = false;

    private bool _canDoubleJump = false;
    private bool _doubleJumped = false;

    public float Fuel
    {
        set
        {
            _fuel = value;
            _fuelDisplayText.text = value.ToString("F1");
        }
        get => _fuel;
    }

    void Start()
    {
        _playerApi = Networking.LocalPlayer;
        _playerApi.SetWalkSpeed(_walkSpeed);
        _playerApi.SetRunSpeed(_runSpeed);
        _playerApi.SetJumpImpulse(_jumpImpulse);
        _playerApi.SetStrafeSpeed(_strafeSpeed);
        Fuel = _maxFuel;
    }

    void Update()
    {
        var head = _playerApi.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        var rot = head.rotation;
        var forward = rot * Vector3.forward;
        var right = rot * Vector3.right;
        var up = rot * Vector3.up;
        _fuelDisplay.transform.position = head.position + (forward * 0.2f) + (right * 0.05f) + (up * -0.05f);
        var lookAt = _fuelDisplay.transform.position - head.position;
        _fuelDisplay.transform.LookAt(head.position + lookAt.normalized);
    }

    void FixedUpdate()
    {
        if (_playerApi.IsPlayerGrounded())
        {
            _canDoubleJump = false;
            _doubleJumped = false;
            if (Fuel < _maxFuel)
            {
                Fuel += 0.01f;
            }
            _usingFuel = false;
        }
        if (_usingFuel && Fuel > 0)
        {
            var vector = _playerApi.GetVelocity();
            vector += Vector3.up * _boostStrength;
            Fuel -= 0.02f;
            _playerApi.SetVelocity(vector);
        }
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (Fuel < 0.2 || _playerApi.IsPlayerGrounded())
        {
            _usingFuel = false;
            return;
        }
        _usingFuel = true;
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        // if no value then do nothing
        if (!value || _doubleJumped)
        {
            return;
        }

        // toggle can double jump
        if (!_canDoubleJump && !_doubleJumped)
        {
            _canDoubleJump = true;
            return;
        }

        // disable double jump
        _canDoubleJump = false;
        _doubleJumped = true;

        // apply double jump
        var velocity = _playerApi.GetVelocity();
        velocity += -Physics.gravity.normalized * _jumpImpulse;
        _playerApi.SetVelocity(velocity);
    }
}
