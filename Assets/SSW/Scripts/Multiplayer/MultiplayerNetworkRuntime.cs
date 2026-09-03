using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class MultiplayerNetworkRuntime : MonoBehaviour
    {
        const string PlayerResourcePath = "Network/PlayerMagicianNetwork";

        readonly List<Vector3> _spawnPositions = new List<Vector3>();
        NetworkManager _manager;
        GameObject _playerPrefab;
        bool _callbacksRegistered;

        public bool HasPlayerPrefab => _playerPrefab != null;

        void Awake()
        {
            SceneManager.sceneLoaded += HandleUnitySceneLoaded;
        }

        public void Prepare()
        {
            if (_manager == null)
                _manager = NetworkManager.Singleton;

            if (_manager == null)
            {
                GameObject managerObject = new GameObject("NetworkManager");
                UnityTransport transport = managerObject.AddComponent<UnityTransport>();
                _manager = managerObject.AddComponent<NetworkManager>();
                _manager.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    EnableSceneManagement = true,
                    PlayerPrefab = null,
                    ForceSamePrefabs = true,
                    TickRate = 30
                };
            }
            else
            {
                if (_manager.NetworkConfig == null)
                    _manager.NetworkConfig = new NetworkConfig();

                if (_manager.NetworkConfig.NetworkTransport == null)
                {
                    UnityTransport transport = _manager.GetComponent<UnityTransport>();
                    if (transport == null) transport = _manager.gameObject.AddComponent<UnityTransport>();
                    _manager.NetworkConfig.NetworkTransport = transport;
                }

                _manager.NetworkConfig.EnableSceneManagement = true;
                _manager.NetworkConfig.PlayerPrefab = null;
            }

            LoadPlayerPrefab();
            RegisterCallbacks();
        }

        public void LoadScene(string sceneName)
        {
            Prepare();
            if (!_manager.IsServer || !_manager.IsListening)
                throw new InvalidOperationException("방 연결이 끝난 뒤 다시 시도해주세요");
            if (_manager.SceneManager == null)
                throw new InvalidOperationException("네트워크 씬 기능이 준비되지 않았습니다");

            SceneEventProgressStatus status = _manager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
                throw new InvalidOperationException($"맵을 불러오지 못했습니다 ({status})");
        }

        void LoadPlayerPrefab()
        {
            if (_playerPrefab == null)
                _playerPrefab = Resources.Load<GameObject>(PlayerResourcePath);
            if (_playerPrefab == null || _manager.IsListening) return;

            bool alreadyRegistered = _manager.NetworkConfig.Prefabs.Prefabs.Any(entry => entry.Prefab == _playerPrefab);
            if (!alreadyRegistered)
            {
                _manager.NetworkConfig.Prefabs.Add(new NetworkPrefab
                {
                    Override = NetworkPrefabOverride.None,
                    Prefab = _playerPrefab
                });
            }
        }

        void RegisterCallbacks()
        {
            if (_callbacksRegistered || _manager == null) return;
            _callbacksRegistered = true;
            _manager.OnServerStarted += HandleServerStarted;
            _manager.OnServerStopped += HandleServerStopped;
        }

        void HandleServerStarted()
        {
            if (_manager.SceneManager != null)
            {
                _manager.SceneManager.OnLoadEventCompleted -= HandleNetworkLoadCompleted;
                _manager.SceneManager.OnLoadEventCompleted += HandleNetworkLoadCompleted;
            }
        }

        void HandleServerStopped(bool _)
        {
            if (_manager != null && _manager.SceneManager != null)
                _manager.SceneManager.OnLoadEventCompleted -= HandleNetworkLoadCompleted;
        }

        void HandleUnitySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_manager == null || !_manager.IsListening || _playerPrefab == null) return;
            if (scene.name == "MainMenu") return;
            CaptureSpawnsAndRemoveOfflinePlayers();
        }

        void CaptureSpawnsAndRemoveOfflinePlayers()
        {
            _spawnPositions.Clear();
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
            List<GameObject> offlineRoots = new List<GameObject>();

            foreach (PlayerController player in players)
            {
                if (player.GetComponentInParent<NetworkObject>() != null) continue;

                GameObject root = player.transform.root.gameObject;
                if (offlineRoots.Contains(root)) continue;
                offlineRoots.Add(root);
                if (_spawnPositions.Count == 0) _spawnPositions.Add(root.transform.position);
            }

            foreach (GameObject root in offlineRoots)
                Destroy(root);

            if (_spawnPositions.Count == 0)
                _spawnPositions.Add(new Vector3(0f, 1.5f, 0f));
        }

        void HandleNetworkLoadCompleted(
            string sceneName,
            LoadSceneMode mode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut)
        {
            if (_manager == null || !_manager.IsServer || _playerPrefab == null) return;
            SpawnPlayers();
        }

        void SpawnPlayers()
        {
            int index = 0;
            foreach (ulong clientId in _manager.ConnectedClientsIds)
            {
                if (_manager.SpawnManager.GetPlayerNetworkObject(clientId) != null)
                {
                    index++;
                    continue;
                }

                Vector3 basePosition = _spawnPositions[Mathf.Min(index, _spawnPositions.Count - 1)];
                float offset = (index - (_manager.ConnectedClientsIds.Count - 1) * 0.5f) * 2.2f;
                Vector3 position = basePosition + Vector3.right * offset;

                GameObject player = Instantiate(_playerPrefab, position, Quaternion.identity);
                NetworkObject networkObject = player.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(clientId, true);
                index++;
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleUnitySceneLoaded;
            if (_manager == null) return;

            _manager.OnServerStarted -= HandleServerStarted;
            _manager.OnServerStopped -= HandleServerStopped;
            if (_manager.SceneManager != null)
                _manager.SceneManager.OnLoadEventCompleted -= HandleNetworkLoadCompleted;
        }
    }
}
