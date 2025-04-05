// BasicGameState.cs
using System;
using System.Collections.Generic;
using Unity.Netcode;
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
    private float _timer = 20.0f;
    private int _wave = 1;
    private int _money = 50;
    private int _monsterCount = 0;
    private bool _isBossWave = false;
    
    [Header("Game Settings")]
    public int HeroCount;
    public int HeroMaximumCount = 25;
    public List<ServerMonster> monsters = new List<ServerMonster>();
    public int UpgradeMoney = 100;
    public int MonsterLimitCount = 100;
    private float _nextWaveTimer = 60.0f;  // 웨이브 시간 설정
    
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
    /// 현재 웨이브
    /// </summary>
    public int Wave => _wave;
    
    /// <summary>
    /// 현재 보유 자금
    /// </summary>
    public int Money => _money;
    
    /// <summary>
    /// 현재 몬스터 수
    /// </summary>
    public int MonsterCount => _monsterCount;
    
    /// <summary>
    /// 현재 보스 웨이브 여부
    /// </summary>
    public bool IsBossWave => _isBossWave;
    
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
    private SessionManager<SessionPlayerData> _sessionManager;

    #endregion
    
    #region Dependencies

    [Inject] public NetworkManager _networkManager;
    [Inject] public ResourceManager _resourceManager;
    [Inject] private ObjectManager _objectManager;
    [Inject] private MapManager _mapManager;

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
        SyncGameStateToPlayer(clientId);
    }

    private void SyncGameStateToPlayer(ulong clientId)
    {
        if (_networkHandler == null) return;
        
        // 특정 플레이어에게만 상태 동기화
        _networkHandler.SyncStateToClientRpc(
            _timer,
            _wave,
            _money,
            _monsterCount,
            _isBossWave,
            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } }
        );
    }

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
        _timer = 20.0f;
        _wave = 1;
        _money = 50;
        _monsterCount = 0;
        _isBossWave = false;
        Debug.Log("[BasicGameState] 상태 초기화 완료");
    }
    
    /// <summary>
    /// 서버 상태 업데이트
    /// </summary>
    private void UpdateServerState()
    {
        UpdateTimer();
    }
    
    /// <summary>
    /// 타이머 업데이트
    /// </summary>
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
                StartNextWave();
            }
        }
    }
    
    /// <summary>
    /// 다음 웨이브 시작
    /// </summary>
    private void StartNextWave()
    {
        if (_networkHandler == null) return;
        
        _wave++;
        _timer = _nextWaveTimer;
        
        // 10의 배수 웨이브는 보스 웨이브
        _isBossWave = (_wave % 10 == 0);
        
        // 클라이언트에 웨이브 변경 알림
        _networkHandler.WaveChangedClientRpc(_wave, _timer, _isBossWave);
        
        Debug.Log($"[BasicGameState] 새 웨이브 시작: Wave={_wave}, 보스={_isBossWave}");
    }
    
    /// <summary>
    /// 초기 상태를 클라이언트에 동기화
    /// </summary>
    private void SyncInitialStateToClients()
    {
        if (_networkHandler == null) return;
        
        _networkHandler.SyncInitialStateClientRpc(
            _timer,
            _wave,
            _money,
            _monsterCount,
            _isBossWave
        );
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
    /// 돈 획득 메서드
    /// </summary>
    /// <param name="value">획득할 금액</param>
    /// <param name="type">호스트 타입</param>
    public void GetMoney(int value, HostType type = HostType.All)
    {
        if (!IsServerReady() || _networkHandler == null) return;
        
        if (type == HostType.All)
        {
            _money += value;
            _networkHandler.UpdateMoneyClientRpc(_money);
            Debug.Log($"[BasicGameState] 돈 추가: +{value}, 현재 돈: {_money}");
        }
    }
    
    /// <summary>
    /// 몬스터 제거 메서드
    /// </summary>
    /// <param name="monster">제거할 몬스터</param>
    /// <param name="Boss">보스 여부</param>
    public void RemoveMonster(ServerMonster monster, bool Boss = false)
    {
        if (!IsServerReady() || _networkHandler == null) return;
        
        if (monster != null && monsters.Contains(monster))
        {
            monsters.Remove(monster);
            _monsterCount = monsters.Count;
            
            _networkHandler.UpdateMonsterCountClientRpc(_monsterCount);
            Debug.Log($"[BasicGameState] 몬스터 제거: 현재 몬스터 수: {_monsterCount}");
        }
    }
    
    /// <summary>
    /// 몬스터 추가 메서드
    /// </summary>
    /// <param name="monster">추가할 몬스터</param>
    public void SetMonster(ServerMonster monster)
    {
        if (!IsServerReady() || _networkHandler == null) return;
        
        if (monster != null && !monsters.Contains(monster))
        {
            monsters.Add(monster);
            _monsterCount = monsters.Count;
            
            _networkHandler.UpdateMonsterCountClientRpc(_monsterCount);
            Debug.Log($"[BasicGameState] 몬스터 추가: 현재 몬스터 수: {_monsterCount}");
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
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _wave = wave;
            _timer = timer;
            _isBossWave = isBossWave;
            
            OnWaveChanged?.Invoke(_isBossWave);
            OnTimerUp?.Invoke();
        }
    }
    
    /// <summary>
    /// 클라이언트 돈 업데이트
    /// </summary>
    public void UpdateClientMoney(int money)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _money = money;
            OnMoneyUp?.Invoke();
        }
    }
    
    /// <summary>
    /// 클라이언트 몬스터 수 업데이트
    /// </summary>
    public void UpdateClientMonsterCount(int count)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _monsterCount = count;
        }
    }
    
    /// <summary>
    /// 클라이언트 초기 상태 동기화
    /// </summary>
    public void SyncClientInitialState(float timer, int wave, int money, int monsterCount, bool isBossWave)
    {
        if (!_isServer) // 클라이언트만 값 업데이트
        {
            _timer = timer;
            _wave = wave;
            _money = money;
            _monsterCount = monsterCount;
            _isBossWave = isBossWave;
            
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
        
        if (_objectManager != null)
        {
            Debug.Log("[BasicGameState] ObjectManager 주입 성공");
        }
        else
        {
            Debug.LogError("[BasicGameState] ObjectManager 주입 실패");
        }
        
        if (_mapManager != null)
        {
            Debug.Log("[BasicGameState] MapManager 주입 성공");
        }
        else
        {
            Debug.LogError("[BasicGameState] MapManager 주입 실패");
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
    
    #endregion
}


