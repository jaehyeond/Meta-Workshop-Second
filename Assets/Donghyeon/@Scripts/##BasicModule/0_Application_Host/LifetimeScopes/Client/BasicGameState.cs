// BasicGameState.cs
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using static Define;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// 게임 상태 변경 이벤트 대리자
/// </summary>
public delegate void OnMoneyUpEventHandler();
public delegate void OnTimerUpEventHandler();
public delegate void OnGameOverEventHandler();
public delegate void OnWaveChangedEventHandler(bool isBossWave);
public delegate void OnScoreUpdatedEventHandler(ulong clientId, int newScore, string playerName);

/// <summary>
/// 기본 게임 상태를 관리하는 클래스
/// </summary>
[RequireComponent(typeof(NetcodeHooks), typeof(GameStateNetworkHandler))]
public class BasicGameState : GameStateLifetimeScope 
{    
    #region Fields

    [Header("Network")]
    [SerializeField] private NetcodeHooks m_NetcodeHooks;
    private GameStateNetworkHandler _networkHandler;
    private bool _isNetworkReady = false;
    private bool _isServer = false; // 서버인지 여부를 저장
    
    [Header("Game State")]
    private float _timer = 180.0f;

    [Header("Score System")]
    private Dictionary<ulong, int> _playerScores = new Dictionary<ulong, int>();
    private Dictionary<string, int> _itemScoreValues = new Dictionary<string, int>() {
        {"Apple", 10},
        {"Candy", -5},
        {"Beef", 30},
        {"Beer", -30}
    };
    
    [Header("Game Settings")]

    
    [Header("Optimization")]
    private float _timerUpdateInterval = 0.1f;
    private float _lastTimerUpdate = 0f;

    #endregion

    #region Properties

    /// <summary>
    /// 현재 게임 타이머
    /// </summary>
    public float Timer => _timer;
    
    /// <summary>
    /// 플레이어의 현재 스코어를 반환
    /// </summary>
    public int GetPlayerScore(ulong clientId)
    {
        if (_playerScores.TryGetValue(clientId, out int score))
            return score;
        return 0;
    }

    /// <summary>
    /// 모든 플레이어의 스코어 정보를 반환
    /// </summary>
    public Dictionary<ulong, int> GetAllPlayerScores()
    {
        return new Dictionary<ulong, int>(_playerScores);
    }

    /// <summary>
    /// 현재 게임 상태
    /// </summary>
    public override GameState ActiveState => GameState.BasicGame;

    #endregion
    
    #region Events

 
    public event OnMoneyUpEventHandler OnMoneyUp;

    public event OnTimerUpEventHandler OnTimerUp;

    public event OnGameOverEventHandler OnGameOver;

    public event OnWaveChangedEventHandler OnWaveChanged;
    
    public event OnScoreUpdatedEventHandler OnScoreUpdated;
    
    private SessionManager<SessionPlayerData> _sessionManager;

    #endregion
    
    #region Dependencies

    [Inject] public NetworkManager _networkManager;
    [Inject] public ResourceManager _resourceManager;
 
    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Awake 시 컴포넌트 초기화
    /// </summary>
    public void Awake()
    {            
        base.Awake();
        
        // NetworkHandler 초기화 - 컴포넌트가 없으면 안전하게 추가
        _networkHandler = GetComponent<GameStateNetworkHandler>();
        if (_networkHandler == null)
        {
            _networkHandler = gameObject.AddComponent<GameStateNetworkHandler>();
            Debug.Log("[BasicGameState] GameStateNetworkHandler 컴포넌트가 추가되었습니다.");
        }
        
        // NetcodeHooks 초기화 - 컴포넌트가 없으면 안전하게 추가
        if (m_NetcodeHooks == null)
        {
            m_NetcodeHooks = GetComponent<NetcodeHooks>();
            if (m_NetcodeHooks == null)
            {
                m_NetcodeHooks = gameObject.AddComponent<NetcodeHooks>();
                Debug.Log("[BasicGameState] NetcodeHooks 컴포넌트가 추가되었습니다.");
            }
        }
        
        // NetworkHandler 초기화
        if (_networkHandler != null)
        {
            _networkHandler.Initialize(this);
            Debug.Log("[BasicGameState] GameStateNetworkHandler가 초기화되었습니다.");
        }
         _sessionManager = SessionManager<SessionPlayerData>.Instance;
         LogSessionData();

    }
    /// <summary>
/// 현재 연결된 모든 플레이어의 세션 데이터를 간단히 로그에 출력합니다.
/// </summary>
    public void LogSessionData()
    {
        if (_sessionManager == null)
        {
            Debug.LogError("[BasicGameState] SessionManager가 없습니다.");
            return;
        }

        Debug.Log("===== 세션 데이터 로그 시작 =====");
        
        // NetworkManager를 통해 현재 연결된 모든 클라이언트 ID 가져오기
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            string playerId = _sessionManager.GetPlayerId(clientId);
            if (string.IsNullOrEmpty(playerId))
                continue;
                
            var playerData = _sessionManager.GetPlayerData(playerId);
            if (!playerData.HasValue)
                continue;
                
            var data = playerData.Value;
            
            // 핵심 데이터만 간단히 출력
            Debug.Log($"플레이어[{clientId}]: 이름={data.PlayerName}, HP={data.CurrentHitPoints}, 위치={data.PlayerPosition}");
        }
        
        Debug.Log("===== 세션 데이터 로그 종료 =====");
    }



    public void StartGame()
    {
        // 세션이 시작되었음을 알림
        _sessionManager.OnSessionStarted();
        
        // 필요한 초기화 작업
        InitializeState();
    }

    // 게임 종료 시 호출
    public void EndGame()
    {
        // 세션이 종료되었음을 알림
        _sessionManager.OnSessionEnded();
    }
    public void OnPlayerConnected(ulong clientId, string playerId)
    {
        // 플레이어 세션 데이터 설정
        SessionPlayerData playerData = new SessionPlayerData(
            clientId, 
            $"Player_{clientId}", 
            new NetworkGuid(),
            100, // 초기 체력
            true, // 연결됨
            false // 캐릭터 생성 안됨
        );
        
        _sessionManager.SetupConnectingPlayerSessionData(clientId, playerId, playerData);
        
        // 필요한 추가 처리
        // SyncGameStateToPlayer(clientId);
    }

    // private void SyncGameStateToPlayer(ulong clientId)
    // {
    //     if (_networkHandler == null) return;
        
    //     // 특정 플레이어에게만 상태 동기화
    //     _networkHandler.SyncStateToClientRpc(
    //         _timer,
    //         _wave,
    //         _money,
    //         _monsterCount,
    //         _isBossWave,
    //         new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } }
    //     );
    // }

    public void OnPlayerDisconnected(ulong clientId)
    {
        _sessionManager.DisconnectClient(clientId);
    }
    /// <summary>
    /// 게임 상태 초기화
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[BasicGameState] 초기화 시작");
        CheckDependencyInjection();

        if (m_NetcodeHooks != null)
        {
            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            m_NetcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
            Debug.Log("[BasicGameState] 네트워크 이벤트 등록 완료");
        }
        else
        {
            Debug.LogError("[BasicGameState] NetcodeHooks가 null입니다!");
        }
    }
    
    /// <summary>
    /// 프레임마다 서버 상태 업데이트
    /// </summary>
    private void Update()
    {
        if (_isNetworkReady && _isServer)
        {
            UpdateServerState();
        }
    }
    
    /// <summary>
    /// 객체 파괴 시 이벤트 해제
    /// </summary>
    protected override void OnDestroy()
    {
        if (m_NetcodeHooks != null)
        {
            m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
            m_NetcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            Debug.Log("[BasicGameState] 네트워크 이벤트 해제 완료");
        }
        
        base.OnDestroy();
    }
    
    #endregion

    #region Network Callbacks
    
    /// <summary>
    /// 네트워크 스폰 시 호출
    /// </summary>
    private void OnNetworkSpawn()
    {
        Debug.Log("[BasicGameState] 네트워크 스폰 완료");
        _isNetworkReady = true;
        _isServer = m_NetcodeHooks.IsServer;
        
        if (_isServer)
        {
            InitializeState();
            SyncInitialStateToClients();
        }
    }
    
    /// <summary>
    /// 네트워크 디스폰 시 호출
    /// </summary>
    private void OnNetworkDespawn()
    {
        Debug.Log("[BasicGameState] 네트워크 디스폰");
        _isNetworkReady = false;
    }
    
    #endregion

    #region Server Methods
    
    /// <summary>
    /// 상태 초기화
    /// </summary>
    private void InitializeState()
    {
        // _timer = 20.0f;
        // _wave = 1;
        // _money = 50;
        // _monsterCount = 0;
        // _isBossWave = false;
        // Debug.Log("[BasicGameState] 상태 초기화 완료");
    }
    
    /// <summary>
    /// 서버 상태 업데이트
    /// </summary>
    private void UpdateServerState()
    {
        UpdateTimer();
    }
    

    private void UpdateTimer()
    {
        // 네트워크 핸들러 null 체크
        if (_networkHandler == null) 
        {
            Debug.LogError("[BasicGameState] NetworkHandler가 null입니다!");
            return;
        }
        
        if (_timer > 0)
        {
            _timer -= Time.deltaTime;
            _timer = Mathf.Max(_timer, 0); // 음수 방지
            
            // 주기적으로 클라이언트에 타이머 동기화
            if (Time.time - _lastTimerUpdate >= _timerUpdateInterval)
            {
                _lastTimerUpdate = Time.time;
                _networkHandler.UpdateTimerClientRpc(_timer);
            }
            
            // 타이머가 0이 되면 새 웨이브 시작
            if (_timer <= 0)
            {
                // StartNextWave();
            }
        }
    }
    

    
    /// <summary>
    /// 초기 상태를 클라이언트에 동기화
    /// </summary>
    private void SyncInitialStateToClients()
    {
        if (_networkHandler == null) return;
        
        // 기본 게임 상태 동기화
        _networkHandler.SyncInitialStateClientRpc(
            _timer,
            0, // wave
            0, // money
            0, // monsterCount
            false // isBossWave
        );
        
        // 각 플레이어의 점수 정보 개별 전송
        foreach (var entry in _playerScores)
        {
            ulong clientId = entry.Key;
            int score = entry.Value;
            
            // 플레이어 이름 가져오기
            string playerName = $"Player{clientId+1}";
            string playerId = _sessionManager.GetPlayerId(clientId);
            if (!string.IsNullOrEmpty(playerId))
            {
                var playerData = _sessionManager.GetPlayerData(playerId);
                if (playerData.HasValue)
                    playerName = playerData.Value.PlayerName;
            }
            
            // 각 플레이어 점수 정보 전송
            _networkHandler.SyncPlayerScoreClientRpc(clientId, score, playerName);
        }
        
        Debug.Log("[BasicGameState] 초기 상태 동기화 완료 (스코어 포함)");
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// 게임 리소스 불러오기
    /// </summary>
    public void Load()
    {
        // 게임 리소스 로드 로직 구현
    }
    
    /// <summary>
    /// 아이템 획득에 따른 플레이어 점수 업데이트
    /// </summary>
    /// <param name="clientId">클라이언트 ID</param>
    /// <param name="itemType">획득 아이템 타입</param>
    public void UpdatePlayerScore(ulong clientId, string itemType)
    {
        if (!IsServerReady() || _networkHandler == null) return;
        
        // 아이템 점수값 확인
        if (!_itemScoreValues.TryGetValue(itemType, out int scoreValue))
        {
            Debug.LogWarning($"[BasicGameState] 정의되지 않은 아이템 타입: {itemType}");
            return;
        }
        
        // 플레이어 스코어 초기화 및 업데이트
        if (!_playerScores.ContainsKey(clientId))
            _playerScores[clientId] = 0;
            
        // 현재 점수 저장
        int currentScore = _playerScores[clientId];
        
        // 점수가 음수가 되지 않도록 제한
        if (currentScore + scoreValue < 0)
        {
            _playerScores[clientId] = 0;
            Debug.Log($"[BasicGameState] 플레이어 {clientId}의 점수가 0 미만이 되지 않도록 제한: {currentScore} + {scoreValue} -> 0");
        }
        else
        {
            _playerScores[clientId] += scoreValue;
            Debug.Log($"[BasicGameState] 플레이어 {clientId}의 점수 업데이트: {currentScore} + {scoreValue} = {_playerScores[clientId]}");
        }
        
        // 플레이어 이름 가져오기
        string playerName = $"Player{clientId+1}";
        string playerId = _sessionManager.GetPlayerId(clientId);
        if (!string.IsNullOrEmpty(playerId))
        {
            var playerData = _sessionManager.GetPlayerData(playerId);
            if (playerData.HasValue)
                playerName = playerData.Value.PlayerName;
        }
        
        // 모든 클라이언트에 동기화
        _networkHandler.UpdateScoreClientRpc(clientId, _playerScores[clientId], playerName);
        
        // 호스트에서도 UI 이벤트 발생시키기 (중요 수정 사항)
        if (_isServer)
        {
            OnScoreUpdated?.Invoke(clientId, _playerScores[clientId], playerName);
            Debug.Log($"[BasicGameState] 호스트에서 직접 점수 UI 이벤트 발생: {playerName}({clientId}) 점수={_playerScores[clientId]}");
        }
        
        Debug.Log($"[BasicGameState] 플레이어 {playerName}({clientId})의 점수 업데이트: {itemType}({scoreValue}), 현재 점수: {_playerScores[clientId]}");
    }
    
    /// <summary>
    /// 클라이언트에서 점수 업데이트를 처리
    /// </summary>
    public void UpdateClientScore(ulong clientId, int newScore, string playerName)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            if (!_playerScores.ContainsKey(clientId))
                _playerScores[clientId] = 0;
                
            _playerScores[clientId] = newScore;
            OnScoreUpdated?.Invoke(clientId, newScore, playerName);
            
            Debug.Log($"[BasicGameState] 클라이언트: 플레이어 {playerName}({clientId})의 점수 업데이트됨: {newScore}");
        }
    }
    
    /// <summary>
    /// 돈 획득 메서드
    /// </summary>
    /// <param name="value">획득할 금액</param>
    /// <param name="type">호스트 타입</param>
    public void GetMoney(int value, HostType type = HostType.All)
    {
        if (!IsServerReady() || _networkHandler == null) return;
        
        if (type == HostType.All)
        {
            // _networkHandler.UpdateMoneyClientRpc(_money);
            // Debug.Log($"[BasicGameState] 돈 추가: +{value}, 현재 돈: {_money}");
        }
    }
    
    /// <summary>
    /// 게임 오버 이벤트 발생
    /// </summary>
    public void OnGameOverEvent()
    {
        Debug.Log("[BasicGameState] 게임 오버 이벤트 발생");
        Time.timeScale = 0.0f;
        OnGameOver?.Invoke();
    }
    
    #endregion
    
    #region Client State Update Methods
    
    /// <summary>
    /// 클라이언트 타이머 업데이트
    /// </summary>
    public void UpdateClientTimer(float timer)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _timer = timer;
            OnTimerUp?.Invoke();
        }
    }
    
    /// <summary>
    /// 클라이언트 웨이브 정보 업데이트
    /// </summary>
    public void UpdateClientWave(int wave, float timer, bool isBossWave)
    {
        // if (!_isServer) // 클라이언트만 값 업데이트
        // {
        //     _wave = wave;
        //     _timer = timer;
        //     _isBossWave = isBossWave;
            
        //     OnWaveChanged?.Invoke(_isBossWave);
        //     OnTimerUp?.Invoke();
        // }
    }
    
 
    /// <summary>
    /// 클라이언트 초기 상태 동기화
    /// </summary>
    public void SyncClientInitialState(float timer, int wave, int money, int monsterCount, bool isBossWave, Dictionary<ulong, int> playerScores, Dictionary<ulong, string> playerNames)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _timer = timer;
            // _wave = wave;
            // _money = money;
            // _monsterCount = monsterCount;
            // _isBossWave = isBossWave;
            
            // 스코어 초기화
            _playerScores = new Dictionary<ulong, int>(playerScores);
            
            // 모든 플레이어 스코어 이벤트 발생
            foreach (var playerScore in _playerScores)
            {
                string playerName = playerNames.TryGetValue(playerScore.Key, out string name) ? name : $"Player{playerScore.Key+1}";
                OnScoreUpdated?.Invoke(playerScore.Key, playerScore.Value, playerName);
            }
            
            OnTimerUp?.Invoke();
            OnMoneyUp?.Invoke();
            
            Debug.Log("[BasicGameState] 초기 상태 동기화 완료");
        }
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// 의존성 주입 확인
    /// </summary>
    private void CheckDependencyInjection()
    {
        if (_resourceManager != null)
        {
            Debug.Log("[BasicGameState] ResourceManager 주입 성공");
        }
        else
        {
            Debug.LogError("[BasicGameState] ResourceManager 주입 실패");
        }
        

        
        Debug.Log($"[BasicGameState] ID: {GetInstanceID()}, 이름: {gameObject.name}");
    }
    
    /// <summary>
    /// 서버 준비 상태 확인
    /// </summary>
    private bool IsServerReady()
    {
        return _isNetworkReady && _isServer;
    }
    
    /// <summary>
    /// 클라이언트에서 초기 플레이어 점수 추가
    /// </summary>
    public void AddInitialPlayerScore(ulong clientId, int score, string playerName)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _playerScores[clientId] = score;
            OnScoreUpdated?.Invoke(clientId, score, playerName);
            
            Debug.Log($"[BasicGameState] 초기 스코어 설정: 플레이어 {playerName}({clientId})의 점수={score}");
        }
    }
    
    #endregion
}


