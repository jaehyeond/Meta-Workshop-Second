using System;
using System.Collections.Generic;
using Unity.Assets.Scripts.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class NetUtils
{

    [Inject] private NetworkManager _networkManager;
    private readonly Dictionary<string, GameObject> _registeredPrefabs = new Dictionary<string, GameObject>();

    public ulong LocalID_P()
    {
        return _networkManager.LocalClientId;
    }
    public static ulong LocalID()
    {
        return NetworkManager.Singleton.LocalClientId;
    }


    public void HostAndClientMethod_P(Action clientAction, Action HostAction)
    {
        if (_networkManager == null)
        {
            Debug.LogError("[NetUtils] _networkManager가 null입니다!");
            return;
        }
        
        if (_networkManager.IsClient) clientAction?.Invoke();
        else if (_networkManager.IsServer) HostAction?.Invoke();
    }
   public static void HostAndClientMethod(Action clientAction, Action HostAction)
    {
        if (NetworkManager.Singleton.IsClient) clientAction?.Invoke();
        else if (NetworkManager.Singleton.IsServer) HostAction?.Invoke();
    }

    public  bool TryGetSpawnedObject_P(ulong networkObjectId, out NetworkObject spawnedObject)
    {
        spawnedObject = null;
        
        return _networkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out spawnedObject);
    }

    public static bool TryGetSpawnedObject(ulong networkObjectId, out NetworkObject spawnedObject)
    {
        spawnedObject = null;
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetUtils] NetworkManager.Singleton이 null입니다!");
            return false;
        }
        
        if (NetworkManager.Singleton.SpawnManager == null)
        {
            Debug.LogError("[NetUtils] NetworkManager.Singleton.SpawnManager가 null입니다!");
            return false;
        }
        
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out spawnedObject);
    }

    public bool IsClientCheck(ulong clientId)
    {
        if (LocalID() == clientId) return true;
        return false;
    }
// NetUtils 클래스에 추가
    // 기존 InitializeNetworkObject 메소드 (현재 사용 중인)
    public void InitializeNetworkObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            Debug.LogError("[NetUtils] 초기화할 게임 오브젝트가 null입니다.");
            return;
        }

        NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = gameObject.AddComponent<NetworkObject>();
            Debug.Log($"[NetUtils] {gameObject.name}에 NetworkObject 컴포넌트를 추가했습니다.");
        }


        if ( _networkManager != null && _networkManager.IsServer && !networkObject.IsSpawned)
        {
            try
            {
                networkObject.Spawn();
                Debug.Log($"[NetUtils] {gameObject.name}을 네트워크에 스폰했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetUtils] {gameObject.name} 스폰 중 오류 발생: {ex.Message}");
            }
        }
    }

    // 런타임에 NetworkManager에 프리팹 등록
    public void RegisterNetworkPrefabRuntime(GameObject gameObject)
    {
        if (_networkManager == null || gameObject == null) return;

        string prefabName = gameObject.name;
        
        // 이미 등록된 프리팹인지 확인
        if (_registeredPrefabs.ContainsKey(prefabName))
        {
            Debug.Log($"[NetUtils] {prefabName}은 이미 NetworkManager에 등록되어 있습니다.");
            return;
        }

        try
        {
            // NetworkPrefab 생성 및 등록
            NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // 직접 NetworkConfig.Prefabs 목록에 추가
                NetworkPrefab networkPrefab = new NetworkPrefab
                {
                    Prefab = gameObject
                };

                // NetworkManager.PrefabHandler.AddHandler 사용하기
                NetworkPrefab newPrefab = new NetworkPrefab { Prefab = gameObject };
                
                _networkManager.NetworkConfig.Prefabs.Add(networkPrefab);
                _registeredPrefabs.Add(prefabName, gameObject);
            }
            else
            {
                Debug.LogWarning($"[NetUtils] {prefabName}에는 NetworkObject 컴포넌트가 없습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetUtils] 프리팹 등록 중 오류: {e.Message}\n{e.StackTrace}");
        }
    }
}

// 동적으로 생성된 오브젝트를 위한 커스텀 PrefabInstanceHandler
    // public static string RarityColor(Rarity rarity)
    // {
    //     switch (rarity)
    //     {
    //         case Rarity.Common: return "<color=#A4A4A4>";
    //         case Rarity.UnCommon: return "<color=#79FF73>";
    //         case Rarity.Rare: return "<color=#6EE5FF>";
    //         case Rarity.Hero: return "<color=#FF9EF5>";
    //         case Rarity.Legendary: return "<color=#FFBA13>";

    //     }
    //     return "";
    // }
// }
