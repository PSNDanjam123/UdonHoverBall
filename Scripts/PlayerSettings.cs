
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

    private bool _canDoubleJump = false;
    private bool _doubleJumped = false;

    void Start()
    {
        _playerApi = Networking.LocalPlayer;
        _playerApi.SetWalkSpeed(_walkSpeed);
        _playerApi.SetRunSpeed(_runSpeed);
        _playerApi.SetJumpImpulse(_jumpImpulse);
        _playerApi.SetStrafeSpeed(_strafeSpeed);
    }

    void Update()
    {
        var head = _playerApi.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        var rot = head.rotation;
        var forward = rot * Vector3.forward;
        var right = rot * Vector3.right;
        var up = rot * Vector3.up;
    }

    void FixedUpdate()
    {
        if (_playerApi.IsPlayerGrounded())
        {
            _canDoubleJump = false;
            _doubleJumped = false;
        }
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
