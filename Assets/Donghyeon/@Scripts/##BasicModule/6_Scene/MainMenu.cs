using System.Collections;
using UnityEngine;
using VContainer;
using Unity.Netcode;
using Unity.Assets.Scripts.UI;
using Unity.Assets.Scripts.Network;
using System;
using Unity.Assets.Scripts.Auth;
using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Scene
{
public class MainMenuScene : BaseScene
{
    [Inject] private LobbyUIMediator m_LobbyUIMediator;

    [Inject] private NetworkManager _networkManager;
    [Inject] private ConnectionManager _connectionManager;

    [Inject] private AuthManager _authManager;
    [Inject] DebugClassFacade m_DebugClassFacade;

    // 로비 생성/참가 상태 플래그
    private bool _isProcessingLobbyRequest = false;
    
    // 서버 연결 정보
	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		SceneType = EScene.MainMenu;

        // UI 이벤트 구독
        SubscribeEvents();

		return true;
	}


	public override void Clear()
	{
        // UI 이벤트 구독 해제
        UnsubscribeEvents();
	}



    private void OnDisable()
    {
        // UI 이벤트 구독 해제
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        // UI_MainMenu의 정적 이벤트 구독
        UI_MainMenu.OnRandomMatchRequested += OnRandomMatchRequested;
        
        // 로비 이벤트 구독
        m_LobbyUIMediator.OnLobbyJoined += OnLobbyJoinedOrCreated;
        m_LobbyUIMediator.OnLobbyCreated += OnLobbyJoinedOrCreated;
        
        Debug.Log("[MainMenuScene] 이벤트 구독 완료");
    }

    private void UnsubscribeEvents()
    {
        // UI_MainMenu의 정적 이벤트 구독 해제
        UI_MainMenu.OnRandomMatchRequested -= OnRandomMatchRequested;
        
        // 로비 이벤트 구독 해제
        m_LobbyUIMediator.OnLobbyJoined -= OnLobbyJoinedOrCreated;
        m_LobbyUIMediator.OnLobbyCreated -= OnLobbyJoinedOrCreated;
        
        Debug.Log("[MainMenuScene] 이벤트 구독 해제");
    }

    // 로비 참가 또는 생성 완료 처리
    private void OnLobbyJoinedOrCreated()
    {
        // 로비 처리 플래그 초기화
        _isProcessingLobbyRequest = false;
        Debug.Log("[MainMenuScene] 로비 생성/참가 완료 - 요청 처리 플래그 초기화");
    }

    // 이벤트 핸들러
    private async void OnRandomMatchRequested()
    {            
        // 이미 로비 요청 처리 중이면 중복 실행 방지
        if (_isProcessingLobbyRequest)
        {
            Debug.Log("[MainMenuScene] 이미 로비 요청 처리 중입니다.");
            return;
        }

        try
        {
            _isProcessingLobbyRequest = true;
            
            // 타임아웃 처리를 위한 타이머 시작
            StartCoroutine(LobbyRequestTimeout());
            
            // 먼저 사용 가능한 로비를 찾습니다
            var (success, currentLobby) = await m_LobbyUIMediator.QueryLobbiesRequest(false);
            Debug.Log($"[MainMenuScene] QueryLobbiesRequest 요청 완료 - 성공: {success}, 로비: {(currentLobby != null ? currentLobby.Id : "없음")}");

            if (currentLobby != null)
            {
                // 기존 로비에 참가합니다
                Debug.Log($"[MainMenuScene] 로비 {currentLobby.Id}에 참가합니다");
                // m_LobbyUIMediator.QuickJoinRequest();
                var localLobby = new LocalLobby
                {
                    LobbyID = currentLobby.Id,
                    LobbyCode = currentLobby.LobbyCode
                };
                m_LobbyUIMediator.JoinLobbyRequest(localLobby);
            }
            else
            {
                // 로비가 없으면 새로 생성합니다 
                Debug.Log("[MainMenuScene] 사용 가능한 로비가 없어 새 로비를 생성합니다");
                string playerId = _authManager.PlayerId;
                m_LobbyUIMediator.CreateLobbyRequest(playerId, false);
            }   
        }
        catch (Exception e)
        {
            Debug.LogError($"[MainMenuScene] 로비 연결 중 예외 발생: {e.Message}");
            _isProcessingLobbyRequest = false; // 예외 발생 시 플래그 리셋
        }
    }

    // 타임아웃 처리를 위한 코루틴
    private IEnumerator LobbyRequestTimeout()
    {
        // 10초 대기 후 타임아웃으로 간주
        yield return new WaitForSeconds(30f);
        // 아직 처리 중이면 타임아웃으로 간주하고 플래그 초기화
        if (_isProcessingLobbyRequest)
        {
            Debug.LogWarning("[MainMenuScene] 로비 요청 타임아웃 - 요청 처리 플래그 초기화");
            _isProcessingLobbyRequest = false;
        }
    }

    public static event Action<bool> OnWaitingStateChanged; // true: 대기 시작, false: 대기 종료

    private void StartWaitingForPlayers()
    {
        OnWaitingStateChanged?.Invoke(true);
    }
}

}