
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Demo : UdonSharpBehaviour
{
    Rigidbody _rigidBody;
    [SerializeField] float _hitStrength = 1.0f;

    private VRCPlayerApi _player;

    void Start()
    {
        _player = Networking.LocalPlayer;
        _rigidBody = GetComponent<Rigidbody>();

    }
    public override void Interact()
    {
        SetOwner();
        var vector = _rigidBody.position - _player.GetPosition();
        _rigidBody.velocity = vector * _hitStrength;
    }

    void SetOwner(VRCPlayerApi playerApi = null)
    {
        if (playerApi == null)
        {
            playerApi = Networking.LocalPlayer;
        }
        Networking.SetOwner(playerApi, gameObject);
    }

    bool IsOwner(VRCPlayerApi playerApi = null)
    {
        if (playerApi == null)
        {
            playerApi = Networking.LocalPlayer;
        }
        return Networking.IsOwner(playerApi, gameObject);
    }

}
