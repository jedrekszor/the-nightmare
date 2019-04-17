﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : NetworkBehaviour
{

    public static GameManager Instance;
    [SerializeField] private Transform[] _enemySpawnPoints;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private MatchSettings _matchSettings;
    [SerializeField] private GameObject[] _weapons;
    [SerializeField] private int _waves;
    [SerializeField] private int _enemiesAmount;
    [SyncVar] private int _enemiesCounter = 0;
    [SyncVar] private int _spawnedEnemiesCounter = 0;
    [SerializeField] private CameraFacing[] _billboards;
    public CameraFacing[] Billboards { get { return _billboards; } set { _billboards = value; } }
    public enum GameState { Building, Fighting }
    private static GameState _currentState = GameState.Building;
    //private static GameState _currentState = GameState.Fighting;
    public static GameState CurrentState
    {
        get { return _currentState; }
        set
        {
            if (value == GameState.Building)
            {
                foreach (PlayerManager player in _players.Values)
                    player.SetBuildingMode();
                if (Instance.isServer) Instance.StopCoroutine(Instance.SpawnEnemy());
            }
            else if (value == GameState.Fighting)
            {
                foreach (PlayerManager player in _players.Values)
                    player.SetActionMode();
                if (Instance.isServer) Instance.StartCoroutine(Instance.SpawnEnemy());
            }
            _currentState = value;
        }
    }
    [SyncVar] public int ReadyPlayersCnt = 0;            
    public GameObject[] Weapons { get { return _weapons; } set { _weapons = value; } }
    public MatchSettings MatchSettings { get { return _matchSettings; } set { _matchSettings = value; } }


    void Start()
    {
       _matchSettings.WaitForSpawn -= _matchSettings.EnemyRespawnTime;
        if (Instance != null) Debug.LogError("More than one GameManager in scene!");
        else Instance = this;
     }

    #region EnemySpawning
    
    [Command]
    void CmdSpawnEnemy(int randIndex)
    {
       RpcSpawnEnemy(randIndex);
    }



    [ClientRpc]
    void RpcSpawnEnemy(int randIndex)
    {
       Instantiate(_enemyPrefab, _enemySpawnPoints[randIndex]);
    }


    private IEnumerator SpawnEnemy()
    {
        yield return new WaitForSeconds(_matchSettings.EnemyRespawnTime);
        int randIndex = Random.Range(0, 3);
        NetworkServer.Spawn(Instantiate(_enemyPrefab, _enemySpawnPoints[randIndex]));
        _enemiesCounter++;
        _spawnedEnemiesCounter++;
        StartCoroutine(SpawnEnemy());
    }


    public static void SetLayerRecursively(GameObject obj, string layerName)
    {
        if (obj == null) return;

        obj.layer = LayerMask.NameToLayer(layerName);

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, layerName);
        }
    }


    #endregion


    #region PlayerAndEnemies

    private const string PLAYER_ID_PREFIX = "Player ";

    public static PlayerManager LocalPlayer { get; set; }
    private static Dictionary<string, PlayerManager> _players = new Dictionary<string, PlayerManager>();
    private static Dictionary<string, PlayerManager> _activePlayers = new Dictionary<string, PlayerManager>();
    public static Dictionary<string, PlayerManager> Players { get { return _players; } }
    public static Dictionary<string, PlayerManager> ActivePlayers { get { return _activePlayers; } }
    public static Dictionary<string, EnemyControllerServer> _enemies = new Dictionary<string, EnemyControllerServer>();
    public static Dictionary<string, EnemyControllerServer> Enemies { get { return _enemies; } }
    private static int _enemyIdCounter = 0;
    public static int EnemyIdCounter { get { return _enemyIdCounter; } set { _enemyIdCounter = value; } }

    public static void RegisterPlayer(string netId, PlayerManager player)
    {
        string playerId = PLAYER_ID_PREFIX + netId;
        _players.Add(playerId, player);
        _activePlayers.Add(playerId, player);
        player.transform.name = playerId;
    }

    public static void UnregisterPlayer(string playerId)
    {
        _players.Remove(playerId);
        if (_activePlayers.ContainsKey(playerId)) _activePlayers.Remove(playerId);
    }

    public static PlayerManager GetPlayer(string playerId)
    {
        if (!_players.ContainsKey(playerId)) return null;
        return _players[playerId];
    }

    public static EnemyControllerServer GetEnemy(string enemyId)
    {
        return _enemies[enemyId];
    }

    public static void DeactivatePlayer(string playerId)
    {
        _activePlayers.Remove(playerId);
    }

    public static void ActivatePlayer(string playerId, PlayerManager player)
    {
        _activePlayers.Add(playerId, player);
    }

    public void SetCameraForBillboards(Camera cam)
    {
        foreach (CameraFacing cameraFacing in _billboards)
            cameraFacing.cameraToLookAt = cam;
    }


    [Command]
    void CmdRegisterBeingReady()
    {
        ReadyPlayersCnt++;
        if (ReadyPlayersCnt == Players.Count)
            RpcRegisterBeingReady();
    }

    [ClientRpc]
    void RpcRegisterBeingReady()
    {
        Debug.Log("RPC REGISTERD ON: " + transform.name);
        CurrentState = GameState.Fighting;
        foreach (PlayerManager player in _players.Values)
        {
            if (isLocalPlayer)
                player.GetComponent<PlacementController>().ChangeState(CurrentState);
            
        }
    }


    #endregion
}
