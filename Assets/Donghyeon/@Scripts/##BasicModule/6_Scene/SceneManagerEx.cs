
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using Unity.Assets.Scripts.Resource;
using System;
using Unity.Assets.Scripts.Network;

namespace Unity.Assets.Scripts.Scene
{
    public enum EScene
	{
		Unknown,
		TitleScene,
		GameScene,
        MainMenu,
        StartUp,
        BasicGame
	}


    
    public class SceneManagerEx
    {
        [Inject] private ResourceManager _resourceManager;
        [Inject] private NetworkManager _networkManager;
        [Inject] private ConnectionManager _connectionManager;
        [Inject] DebugClassFacade _debugClassFacade;

        // 씬 전환 요청 추적을 위한 변수
        private bool _isSceneTransitionInProgress = false;
        private float _lastSceneTransitionTime = 0f;
        private string _pendingSceneName = null;

        public BaseScene CurrentScene { get { return GameObject.FindAnyObjectByType<BaseScene>(); } }

        public void LoadScene(EScene type)
        {
            // 씬 전환 전에 현재 씬과 리소스 정리
            Debug.Log($"[SceneManagerEx] 씬 전환: {CurrentScene?.SceneType} -> {type}");
            
            // 씬 로드
            SceneManager.LoadScene(GetSceneName(type));
        }

        // 네트워크용 씬 로드 메서드 - 개선됨
        public virtual void LoadScene(string sceneName, bool useNetworkSceneManager, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            _debugClassFacade?.LogInfo(GetType().Name, $"LoadScene 호출: {sceneName}, 네트워크={useNetworkSceneManager}");
            
            if (useNetworkSceneManager)
            {
                if (_networkManager == null)
                {
                    _debugClassFacade?.LogError(GetType().Name, "NetworkManager가 null입니다.");
                    return;
                }

                // 네트워크 상태 확인
                bool isServer = _networkManager.IsServer;
                
                if (isServer)
                {
                    _debugClassFacade?.LogInfo(GetType().Name, $"서버: 씬 전환 시작: {sceneName}");
                    
                    // 서버에서만 NetworkSceneManager 호출 (클라이언트는 자동으로 동기화됨)
                    if (_networkManager.SceneManager != null)
                    {
                        try
                        {
                            _networkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
                            _debugClassFacade?.LogInfo(GetType().Name, $"서버: {sceneName} 씬 로드 요청 성공");
                        }
                        catch (Exception e)
                        {
                            _debugClassFacade?.LogError(GetType().Name, $"서버: 씬 로드 중 오류 발생: {e.Message}");
                        }
                    }
                    else
                    {
                        _debugClassFacade?.LogError(GetType().Name, "NetworkManager.SceneManager가 null입니다.");
                    }
                }
                else
                {
                    _debugClassFacade?.LogInfo(GetType().Name, "클라이언트: 서버의 씬 전환 명령을 기다립니다.");
                    // 클라이언트는 아무것도 하지 않고 서버의 씬 전환을 기다림
                }
            }
        
        }

        private void ResetSceneTransitionState()
        {
            // 씬 전환 상태 리셋 (코루틴이나 지연 처리로 구현해야 하지만, 여기서는 타이머 기반으로 작성)
            _isSceneTransitionInProgress = false;
        }

        public void LoadSceneForAllPlayers(EScene type)
        {
            // 씬 전환 전에 현재 씬과 리소스 정리
            _debugClassFacade?.LogInfo(GetType().Name, $"네트워크 씬 전환: {CurrentScene?.SceneType} -> {type}");
            
            string sceneName = type.ToString();
            
            if (_networkManager != null && _networkManager.SceneManager != null)
            {
                _networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                _debugClassFacade?.LogInfo(GetType().Name, $"NetworkSceneManager를 통해 {sceneName} 씬 로드 요청 성공");
                
                // 추가: RPC를 통해서도 알림 (중복 보장)
                if (_connectionManager != null && _networkManager.IsServer)
                {
                    _connectionManager.LoadSceneClientRpc(sceneName);
                    _debugClassFacade?.LogInfo(GetType().Name, "RPC를 통해 클라이언트에게 씬 전환 명령 전송");
                }
            }
            else
            {
                _debugClassFacade?.LogError(GetType().Name, "NetworkManager 또는 SceneManager가 null입니다.");
            }
        }

        private string GetSceneName(EScene type)
        {
            string name = System.Enum.GetName(typeof(EScene), type);
            return name;
        }

        public void Clear()
        {
            // 현재 씬의 Clear 메서드 호출
            CurrentScene?.Clear();
        }
    }
}