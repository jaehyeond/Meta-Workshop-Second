using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Scene;
using Unity.Assets.Scripts.UI;
using UnityEngine;
using Unity.Assets.Scripts.Resource;
using System;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using Unity.Assets.Scripts.Data;
using Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers;
using Unity.Assets.Scripts.Module.ApplicationLifecycle;

public class BasicGameLifetimeScope : LifetimeScope
{
    // 필드 정의
    private CameraProvider _cameraProvider;
    private DebugClassFacade _debugClassFacade;
    private ResourceManager _resourceManager;

    protected override void Configure(IContainerBuilder builder)
    {
        // 빈 상태로 둡니다
        Debug.Log($"[{GetType().Name}] Configure 메서드 실행");
    }

    // Awake에서 수동 Resolve
    protected override void Awake()
    {
        base.Awake();
        
        Debug.Log($"[{GetType().Name}] Awake 메서드 시작 - 의존성 수동 해결 시도");
        
        try
        {
            // 컨테이너에서 직접 의존성 해결
            _cameraProvider = Container.Resolve<CameraProvider>();
            _debugClassFacade = Container.Resolve<DebugClassFacade>();
            
            // ResourceManager 해결 시도
            _resourceManager = Container.Resolve<ResourceManager>();
   
            
            Debug.Log($"[{GetType().Name}] 모든 의존성 해결 성공!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] 의존성 해결 오류: {ex.Message}");
            Debug.LogException(ex);
        }
    }
    

}

