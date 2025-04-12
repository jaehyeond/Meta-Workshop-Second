using UnityEngine;
using Unity.Netcode;
using VContainer;
using Unity.Assets.Scripts.Resource;  // 이 줄을 추가해야 합니다
using Unity.Assets.Scripts.Objects; // BaseObject 타입 참조를 위해 추가
using VContainer.Unity;

/// <summary>
/// Apple 오브젝트 생성 및 관리를 담당하는 클래스
/// </summary>
public class AppleManager : MonoBehaviour
{
    [SerializeField] private int _initialAppleCount = 100;  // 초기 생성할 사과 개수
    [SerializeField] private float _spawnAreaRadius = 10f;  // 스폰 영역 반경
    [SerializeField] private float _spawnHeight = 0.0f;  // 스폰 높이


    [Inject] private ObjectManager _objectManager;

    public void SpawnInitialApples()
    {
        Debug.Log($"[{GetType().Name}] Spawning initial {_initialAppleCount} apples...");
        for (int i = 0; i < _initialAppleCount; i++)
        {
            SpawnApple(); // 개별 사과 스폰 및 활성화 로직 재사용
        }
    }


    /// <summary>
    /// 단일 사과를 스폰하는 메서드
    /// </summary>
    public void SpawnApple()
    {
        Debug.Log($"[{GetType().Name}] Spawning apple...");
        Vector3 spawnPosition = GetRandomPosition();

        // ObjectManager에서 활성화를 처리하므로 여기서는 스폰만 호출합니다.
        _objectManager.Spawn<BaseObject>(spawnPosition, prefabName: "Apple");
    }



    private Vector3 GetRandomPosition()
    {
        // XZ 평면에서 랜덤한 위치 계산
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(0f, _spawnAreaRadius);
        
        float x = Mathf.Cos(angle) * distance;
        float z = Mathf.Sin(angle) * distance;
        
        return new Vector3(x, _spawnHeight, z);
    }
   // Apple 파괴를 위한 ClientRpc 추가

   
 
} 