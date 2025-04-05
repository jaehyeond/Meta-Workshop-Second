// using UnityEngine;
// using Unity.Netcode;
// using System;
// using System.Collections.Generic;

// // 네트워크 동기화를 위한 범용 인터페이스
// public interface INetworkSyncable
// {
//     // 초기화 및 동기화를 위한 공통 메서드
//     void OnNetworkSpawn(NetworkSyncContext context);
// }

// // 네트워크 동기화 컨텍스트 - 유연하고 확장 가능한 구조
// public class NetworkSyncContext
// {
//     // 기본 필수 정보
//     public ulong ClientId { get; set; }
//     public Vector3 Position { get; set; }
    
//     // 유연한 데이터 저장소
//     private Dictionary<string, object> _contextData = new Dictionary<string, object>();

//     // 데이터 추가 메서드
//     public void SetData(string key, object value)
//     {
//         _contextData[key] = value;
//     }

//     // 데이터 조회 메서드
//     public T GetData<T>(string key, T defaultValue = default)
//     {
//         return _contextData.TryGetValue(key, out object value) 
//             ? (T)value 
//             : defaultValue;
//     }

//     // 편의 메서드들
//     public bool HasData(string key) => _contextData.ContainsKey(key);
//     public void RemoveData(string key) => _contextData.Remove(key);
// }

// // 네트워크 동기화 관리자
// public class NetworkSyncManager : NetworkBehaviour
// {
//     // 싱글톤 패턴
//     public static NetworkSyncManager Instance { get; private set; }

//     // 오브젝트 스폰 및 동기화를 위한 제네릭 메서드
//     public T SpawnAndSync<T>(
//         Vector3 spawnPosition, 
//         ulong clientId, 
//         Action<NetworkSyncContext> configureContext = null) where T : NetworkBehaviour, INetworkSyncable
//     {
//         // 서버에서만 실제 스폰 수행
//         if (!NetworkManager.Singleton.IsServer) return null;

//         // 동기화 컨텍스트 생성
//         var syncContext = new NetworkSyncContext
//         {
//             ClientId = clientId,
//             Position = spawnPosition
//         };

//         // 추가 컨텍스트 설정 콜백
//         configureContext?.Invoke(syncContext);

//         // 오브젝트 스폰
//         T spawnedObject = GetComponent<ObjectManager>().Spawn<T>(spawnPosition, clientId);
        
//         if (spawnedObject == null) 
//         {
//             Debug.LogError($"Failed to spawn object of type {typeof(T).Name}");
//             return null;
//         }

//         // 네트워크 스폰 및 동기화
//         spawnedObject.OnNetworkSpawn(syncContext);

//         // 클라이언트 동기화 요청
//         RequestClientSyncRpc(spawnedObject.NetworkObject.NetworkObjectId, syncContext);

//         return spawnedObject;
//     }

//     // 클라이언트 동기화 RPC
//     [ServerRpc(RequireOwnership = false)]
//     private void RequestClientSyncRpc(ulong networkObjectId, NetworkSyncContext syncContext)
//     {
//         // 모든 클라이언트에 동기화 데이터 브로드캐스트
//         BroadcastClientSyncClientRpc(networkObjectId, syncContext);
//     }

//     // 클라이언트 측 동기화 RPC
//     [ClientRpc]
//     private void BroadcastClientSyncClientRpc(ulong networkObjectId, NetworkSyncContext syncContext)
//     {
//         // 네트워크 오브젝트 찾기
//         if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
//             .TryGetValue(networkObjectId, out NetworkObject networkObject))
//         {
//             // 위치 동기화
//             networkObject.transform.position = syncContext.Position;

//             // INetworkSyncable 인터페이스 확인 및 동기화 호출
//             var syncableObject = networkObject.GetComponent<INetworkSyncable>();
//             syncableObject?.OnNetworkSpawn(syncContext);
//         }
//     }


// }

// // 
// public class GameController : NetworkBehaviour
// {
//     public void SpawnMonster(bool isBoss)
//     {
//         // 의존성 주입된 MapSpawnerFacade에서 스폰 위치 결정
//         var mapSpawnerFacade = FindObjectOfType<MapSpawnerFacade>();
//         var spawnPos = isBoss 
//             ? mapSpawnerFacade.Other_move_list[0] 
//             : mapSpawnerFacade.Player_move_list[0];

//         // 네트워크 동기화를 통한 몬스터 스폰
//         NetworkSyncManager.Instance.SpawnAndSync<ServerMonster>(
//             spawnPos, 
//             NetworkManager.Singleton.LocalClientId, 
//             context => 
//             {
//                 // 동적으로 컨텍스트에 데이터 추가
//                 context.SetData("IsBoss", isBoss);
                
//                 // 보스의 경우 추가 정보 설정
//                 if (isBoss)
//                 {
//                     context.SetData("BossLevel", 5);
//                     context.SetData("BossType", "FireDragon");
//                 }
//             }
//         );
//     }
// }