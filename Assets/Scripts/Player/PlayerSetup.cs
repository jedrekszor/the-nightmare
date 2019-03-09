﻿using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(PlayerManager))]
public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private Behaviour[] _toDisable;
    [SerializeField] private Camera _sceneCamera;
    [SerializeField] private Camera _cam;
    [SerializeField] private GameObject _weaponObjectPrefab;
    private PlayerEquipment _equipment;
     
    // Start is called before the first frame update
    void Start()
    {
        EquipWeapon();

        if (!isLocalPlayer)
        {
            DisableComponents();
            AssignRemoteLayer();
            DisableWeaponCamera();
        }
        else
        {
           GameManager.LocalPlayer = GetComponent<PlayerManager>();
           _sceneCamera = Camera.main;
           if (_sceneCamera != null)
                _sceneCamera.gameObject.SetActive(false);
        }
        GetComponent<PlayerManager>().Setup();
    }


    void DisableWeaponCamera()
    {
        _cam.transform.GetChild(1).GetComponent<Camera>().enabled = false;

        GameManager.SetLayerRecursively(_cam.transform.GetChild(0).GetChild(0).gameObject, "LocalPlayer");
    }

    void EquipWeapon()
    {
        GameObject weaponObject = Instantiate(_weaponObjectPrefab, _cam.transform.GetChild(0));
        PlayerShoot shoot = GetComponent<PlayerShoot>();
        shoot.Cam = _cam;
        _equipment = GetComponent<PlayerEquipment>();
        _equipment.Weapon = weaponObject.GetComponent<PlayerWeapon>();
        _equipment.WeaponSound = weaponObject.GetComponent<AudioSource>();
        shoot.Equipment = _equipment;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        GameManager.RegisterPlayer(GetComponent<NetworkIdentity>().netId.ToString(), GetComponent<PlayerManager>());
    }

    private void AssignRemoteLayer()
    {
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("RemotePlayer");
    }

    private void DisableComponents()
    {
        for (int i = 0; i < _toDisable.Length; i++)
            _toDisable[i].enabled = false;
    }

    private void OnDisable()
    {
        if (_sceneCamera != null)
            _sceneCamera.gameObject.SetActive(true);

        GameManager.UnregisterPlayer(transform.name);
    }

    private void OnEnable()
    {
        if (_sceneCamera != null)
            _sceneCamera.gameObject.SetActive(true);

        if (!GameManager.Players.ContainsKey(transform.name)) GameManager.RegisterPlayer(GetComponent<NetworkIdentity>().netId.ToString(), GetComponent<PlayerManager>());
    }

    //public void DeactivateCamera()
    //{
    //    if (_sceneCamera != null)
    //        _sceneCamera.gameObject.SetActive(false);
    //}
}


