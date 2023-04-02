
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

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == null)
        {
            return; // protected
        }
        var carController = collision.gameObject.GetComponentInParent<CarController>();
        if (carController == null)
        {
            return;
        }
        /*if (carController.Owner != Networking.LocalPlayer.displayName)
        {
            return;
        }*/
        SetOwner();
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
