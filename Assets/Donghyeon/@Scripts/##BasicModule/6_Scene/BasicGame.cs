using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Scene;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Object = UnityEngine.Object;
using Unity.Netcode;
using Unity.Assets.Scripts.UI;
using Unity.Assets.Scripts.Objects;
using VContainer.Unity;
using System;
using static Define;


namespace Unity.Assets.Scripts.Scene
{
public class BasicGameScene : BaseScene
{

	// [Inject] private ServerMonster _serverMonster; // MonoBehaviour는 이런 방식으로 주입받을 수 없습니다.
	
    // VContainer.IObjectResolver 추가

    [Inject] private UIManager _uiManager;
    [Inject] private ResourceManager _resourceManager;
	// greenslime 몬스터 ID
	
	// 스폰된 몬스터 관리 리스트

    // OnGridSpawned 이벤트가 너무 일찍 호출되는 것을 방지하기 위한 플래그
    private bool _isInitialized = false;
	
    private bool _isEventsSubscribed = false;
    
    private AppleManager _appleManager;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        SceneType = EScene.BasicGame;
 
      	_uiManager.ShowBaseUI<UI_Joystick>();
      	
      	// AppleManager 초기화
      	InitializeAppleManager();

        return true;
    }

    public override void Clear()
    {

    }

    private void OnEnable()
    {
        // 이벤트 구독은 Init에서만 수행
    }

    private void OnDisable()
    {
        // 이벤트 해제는 Clear에서만 수행
    }

    private void SubscribeEvents()
    {

    }

    private void UnsubscribeEvents()
    {

    }
    
    /// <summary>
    /// AppleManager를 초기화합니다.
    /// </summary>
    private void InitializeAppleManager()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return; // 서버에서만 실행
        
        try
        {
            // 프리팹 로드
            GameObject appleManagerPrefab = _resourceManager.Load<GameObject>("Prefabs/InGame/AppleManager");
            
            if (appleManagerPrefab == null)
            {
                Debug.LogError("[BasicGameScene] AppleManager 프리팹을 로드할 수 없습니다!");
                return;
            }
            
            // AppleManager 생성
            GameObject appleManagerObj = Instantiate(appleManagerPrefab, Vector3.zero, Quaternion.identity);
            _appleManager = appleManagerObj.GetComponent<AppleManager>();
            
            if (_appleManager == null)
            {
                Debug.LogError("[BasicGameScene] 생성된 오브젝트에 AppleManager 컴포넌트가 없습니다!");
                Destroy(appleManagerObj);
                return;
            }
            
            // VContainer를 통한 의존성 주입
            FindObjectOfType<LifetimeScope>()?.Container.Inject(_appleManager);
            
            // NetworkObject 확인 및 스폰
            NetworkObject networkObject = appleManagerObj.GetComponent<NetworkObject>();
            if (networkObject != null && !networkObject.IsSpawned)
            {
                networkObject.Spawn();
                Debug.Log("[BasicGameScene] AppleManager를 성공적으로 스폰했습니다.");
            }
            else
            {
                Debug.LogError("[BasicGameScene] AppleManager에 NetworkObject가 없거나 이미 스폰되었습니다.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BasicGameScene] AppleManager 초기화 중 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }
}

}