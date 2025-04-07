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

    protected override void Configure(IContainerBuilder builder)
    {
        // 빈 상태로 둡니다
        Debug.Log($"[{GetType().Name}] Configure 메서드 실행");



        ResourceManager resourceManager = null;
        resourceManager = Parent.Container.Resolve<ResourceManager>();
        builder.RegisterInstance(resourceManager);

        CameraProvider _cameraProvider = null;
        _cameraProvider = Parent.Container.Resolve<CameraProvider>();
        builder.RegisterInstance(_cameraProvider);

        DebugClassFacade _debugClassFacade = null;
        _debugClassFacade = Parent.Container.Resolve<DebugClassFacade>();
        builder.RegisterInstance(_debugClassFacade);

        UIManager _uiManager = null;
        _uiManager = Parent.Container.Resolve<UIManager>();
        builder.RegisterInstance(_uiManager);

        GameManager _gameManager = null;
        _gameManager = Parent.Container.Resolve<GameManager>();
        builder.RegisterInstance(_gameManager);
 

        AppleManager _appleManager = null;
        _appleManager = Parent.Container.Resolve<AppleManager>();
        builder.RegisterInstance(_appleManager);

        _debugClassFacade?.LogInfo(GetType().Name, "BasicGameScene 등록 시도");
        builder.RegisterComponentInHierarchy<BasicGameScene>();

        ObjectManager _objectManager = null;
        _objectManager = Parent.Container.Resolve<ObjectManager>();
        builder.RegisterInstance(_objectManager);
    }

    

}

