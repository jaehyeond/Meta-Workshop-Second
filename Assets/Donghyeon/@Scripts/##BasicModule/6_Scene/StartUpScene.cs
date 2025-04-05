using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Scene;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Object = UnityEngine.Object;

namespace Unity.Assets.Scripts.Scene
{
public class StartUpScene : BaseScene
{
	[Inject] private ResourceManager _resourceManager;

	private const string PRELOAD_LABEL = "PreLoad";
	
	// 리소스 로딩 관련 이벤트
	public event System.Action<string, int, int> OnResourceLoadProgress;
	public event System.Action OnResourceLoadComplete;
	
	// 리소스 로딩 상태 추적 변수
	private bool _isResourceLoaded = false;

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		SceneType = EScene.StartUp;
		
		// 리소스 매니저 이벤트 구독
		if (_resourceManager != null)
		{
			_resourceManager.OnLoadingCompleted += OnResourceLoadingComplete;
		}

		StartLoadAssets();

		return true;
	}

	public void StartLoadAssets()
    {
        try
        {
            Debug.Log("[StartUpScene] 리소스 로드 시작");
            
            // ResourceManager가 null인지 확인
            if (_resourceManager == null)
            {
                Debug.LogError("[StartUpScene] ResourceManager가 null입니다!");
                // 리소스 로드 완료 이벤트 발생 (실패로 처리)
                _isResourceLoaded = true;
                OnResourceLoadComplete?.Invoke();
                return;
            }
            
            // PreLoad 라벨이 존재하는지 확인
            if (string.IsNullOrEmpty(PRELOAD_LABEL))
            {
                Debug.LogError("[StartUpScene] PreLoad 라벨이 비어있습니다!");
                // 리소스 로드 완료 이벤트 발생 (실패로 처리)
                _isResourceLoaded = true;
                OnResourceLoadComplete?.Invoke();
                return;
            }
            
            // PreLoad 라벨의 모든 리소스 비동기 로드
            _resourceManager.LoadAllAsync<Object>(PRELOAD_LABEL, (key, count, totalCount) =>
            {
                // 진행 상황 이벤트 발생
                OnResourceLoadProgress?.Invoke(key, count, totalCount);
                
            });
            
            Debug.Log("[StartUpScene] 리소스 로드 요청 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StartUpScene] 리소스 로딩 중 오류 발생: {e.Message}\n{e.StackTrace}");
            _isResourceLoaded = true;
            OnResourceLoadComplete?.Invoke();
        }
    }
    
    private void OnResourceLoadingComplete()
    {
        Debug.Log("[StartUpScene] 모든 리소스 로드 완료");
        _isResourceLoaded = true;
        OnResourceLoadComplete?.Invoke();
    }

	public override void Clear()
	{
		// 이벤트 구독 해제
		if (_resourceManager != null)
		{
			_resourceManager.OnLoadingCompleted -= OnResourceLoadingComplete;
		}
	}

    public void LoadMainMenuScene()
    {
        _sceneManager.LoadScene(EScene.MainMenu);
    }
}

}
