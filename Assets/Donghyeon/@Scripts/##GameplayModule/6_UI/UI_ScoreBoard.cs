using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VContainer;
using Unity.Netcode;

/// <summary>
/// 게임 내 스코어보드 UI를 관리하는 클래스
/// </summary>
public class UI_ScoreBoard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreboardTitle;     // 스코어보드 제목
    [SerializeField] private TextMeshProUGUI playerScoreTextPrefab;  // 플레이어 점수 텍스트 프리팹
    [SerializeField] private Transform playerScoreContainer;       // 점수 텍스트를 담을 컨테이너
    
    [Header("Settings")]
    [SerializeField] private string titleFormat = "SCOREBOARD";
    [SerializeField] private string playerScoreFormat = "{0}: {1}";
    [SerializeField] private Color positiveScoreColor = Color.green;
    [SerializeField] private Color negativeScoreColor = Color.red;
    [SerializeField] private Color neutralScoreColor = Color.white;
    
    [Header("Layout Settings")]
    [SerializeField] private float verticalSpacing = 60f;            // 점수 텍스트 간 세로 간격 (증가)
    [SerializeField] private float initialOffset = 0f;               // 제목과 첫 번째 플레이어 텍스트 사이의 간격 (0으로 설정)
    
    private BasicGameState _gameState;
    private Dictionary<ulong, TextMeshProUGUI> _playerTextElements = new Dictionary<ulong, TextMeshProUGUI>();
    private Dictionary<ulong, Color> _playerColors = new Dictionary<ulong, Color>();  // 플레이어별 색상 저장
    
    // 플레이어 점수 정보를 관리하기 위한 구조체
    private struct PlayerScoreInfo
    {
        public ulong ClientId;
        public string PlayerName;
        public int Score;
        public TextMeshProUGUI TextElement;
    }
    
    // 플레이어 점수 정보를 담는 리스트
    private List<PlayerScoreInfo> _playerScoreInfos = new List<PlayerScoreInfo>();
    
    [Inject]
    private void Construct(BasicGameState gameState)
    {
        _gameState = gameState;
    }
    
    private void Start()
    {
        if (_gameState == null)
        {
            _gameState = FindObjectOfType<BasicGameState>();
            if (_gameState == null)
            {
                Debug.LogError("[UI_ScoreBoard] BasicGameState를 찾을 수 없습니다!");
                return;
            }
        }
        
        // 스코어보드 타이틀 설정
        if (scoreboardTitle != null)
            scoreboardTitle.text = titleFormat;
        
        // 스코어 업데이트 이벤트 구독
        _gameState.OnScoreUpdated += UpdateScoreUI;
        
        // 시작할 때 모든 클라이언트 정보 수동으로 요청 (중요 추가 사항)
        StartCoroutine(DelayedInitialization());
        
        Debug.Log("[UI_ScoreBoard] 초기화 완료");
    }
    
    // 지연된 초기화 코루틴 추가
    private IEnumerator DelayedInitialization()
    {
        // 네트워크 초기화를 위해 잠시 대기
        yield return new WaitForSeconds(1.0f);
        
        // 현재 점수 정보를 즉시 표시
        if (_gameState != null)
        {
            Dictionary<ulong, int> allScores = _gameState.GetAllPlayerScores();
            Debug.Log($"[UI_ScoreBoard] 초기화 - 현재 플레이어 수: {allScores.Count}");
            
            foreach (var score in allScores)
            {
                ulong clientId = score.Key;
                int playerScore = score.Value;
                
                // 플레이어 이름 가져오기
                string playerName = $"Player{clientId+1}";
                
                // 세션 매니저에서 이름 확인 시도
                var sessionManager = SessionManager<SessionPlayerData>.Instance;
                if (sessionManager != null)
                {
                    string playerId = sessionManager.GetPlayerId(clientId);
                    if (!string.IsNullOrEmpty(playerId))
                    {
                        var playerData = sessionManager.GetPlayerData(playerId);
                        if (playerData.HasValue)
                            playerName = playerData.Value.PlayerName;
                    }
                }
                
                Debug.Log($"[UI_ScoreBoard] 초기화 - 플레이어 추가: {playerName}({clientId}), 점수: {playerScore}");
                UpdateScoreUI(clientId, playerScore, playerName);
            }
        }
    }
    
    // 플레이어 ID에 기반한 랜덤 색상 생성
    private Color GenerateRandomColor(ulong clientId)
    {
        // 고정된 랜덤 시드 - 항상 동일한 clientId에 대해 동일한 색상 반환
        System.Random random = new System.Random((int)clientId * 1000);
        
        // HSV 색상 공간에서 고채도, 고명도 색상 생성 (더 선명한 색상)
        float h = (float)random.NextDouble(); // 0~1 사이 색상값
        float s = 0.8f + (float)random.NextDouble() * 0.2f; // 0.8~1.0 사이 채도값
        float v = 0.8f + (float)random.NextDouble() * 0.2f; // 0.8~1.0 사이 명도값
        
        // HSV에서 RGB로 변환
        Color color = Color.HSVToRGB(h, s, v);
        
        return color;
    }
    
    private void UpdateScoreUI(ulong clientId, int newScore, string playerName)
    {
        Debug.Log($"[UI_ScoreBoard] UpdateScoreUI 호출됨: clientId={clientId}, newScore={newScore}, playerName={playerName}, 서버여부={NetworkManager.Singleton.IsServer}");
        
        // 해당 클라이언트 ID에 대한 UI 요소가 없으면 생성
        if (!_playerTextElements.ContainsKey(clientId))
        {
            Debug.Log($"[UI_ScoreBoard] 새 UI 요소 생성 필요: clientId={clientId}");
            CreatePlayerScoreText(clientId, playerName, newScore);
        }
        else
        {
            // 기존 UI 요소 업데이트
            TextMeshProUGUI textElement = _playerTextElements[clientId];
            
            // 플레이어 ID에 고유한 색상 적용 (이미 저장된 색상 사용)
            Color playerColor = _playerColors.ContainsKey(clientId) ? _playerColors[clientId] : Color.white;
            
            textElement.text = string.Format(playerScoreFormat, playerName, newScore);
            textElement.color = playerColor;
            
            // 플레이어 점수 정보 업데이트
            for (int i = 0; i < _playerScoreInfos.Count; i++)
            {
                if (_playerScoreInfos[i].ClientId == clientId)
                {
                    var info = _playerScoreInfos[i];
                    info.Score = newScore;
                    info.PlayerName = playerName;
                    _playerScoreInfos[i] = info;
                    break;
                }
            }
            
            Debug.Log($"[UI_ScoreBoard] 점수 업데이트 완료: {playerName}({clientId}) -> {newScore}");
        }
        
        // 모든 플레이어 점수를 기준으로 순위 재정렬
        UpdatePlayerRankings();
    }
    
    // 새 플레이어를 위한 점수 텍스트 생성
    private void CreatePlayerScoreText(ulong clientId, string playerName, int score)
    {
        // playerScoreTextPrefab이 없거나 컨테이너가 없으면 오류 로그 출력
        if (playerScoreTextPrefab == null || playerScoreContainer == null)
        {
            Debug.LogError("[UI_ScoreBoard] 플레이어 텍스트 프리팹 또는 컨테이너가 설정되지 않았습니다!");
            return;
        }
        
        // 프리팹을 기반으로 새 텍스트 요소 생성
        TextMeshProUGUI newTextElement = Instantiate(playerScoreTextPrefab, playerScoreContainer);
        
        // 플레이어 ID에 고유한 랜덤 색상 생성 및 저장
        if (!_playerColors.ContainsKey(clientId))
        {
            Color playerColor = GenerateRandomColor(clientId);
            _playerColors.Add(clientId, playerColor);
            Debug.Log($"[UI_ScoreBoard] 플레이어 {playerName}({clientId})에 색상 할당: R={playerColor.r:F2}, G={playerColor.g:F2}, B={playerColor.b:F2}");
        }
        
        // 저장된 플레이어 색상 사용
        Color textColor = _playerColors[clientId];
        
        // 텍스트 및 색상 설정
        newTextElement.text = string.Format(playerScoreFormat, playerName, score);
        newTextElement.color = textColor;
        
        // 텍스트 요소의 초기 위치 설정
        RectTransform rectTransform = newTextElement.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 첫 번째 플레이어는 고정 위치, 두 번째부터는 아래로 누적
            rectTransform.anchorMin = new Vector2(1.15f, -4f);
            rectTransform.anchorMax = new Vector2(1.15f, -4f);
            rectTransform.pivot = new Vector2(1.15f, -4f);
            int index = _playerScoreInfos.Count;
            if (index == 0)
            {
                rectTransform.anchoredPosition = new Vector2(-40, -50); // 첫 번째는 고정
            }
            else
            {
                rectTransform.anchoredPosition = new Vector2(-20, -60 - (index * verticalSpacing)); // 아래로 누적
            }
        }
        
        // 플레이어 정보 추가
        PlayerScoreInfo playerInfo = new PlayerScoreInfo
        {
            ClientId = clientId,
            PlayerName = playerName,
            Score = score,
            TextElement = newTextElement
        };
        
        // 플레이어 점수 정보 리스트에 추가
        _playerScoreInfos.Add(playerInfo);
        
        // 사전에 추가
        _playerTextElements.Add(clientId, newTextElement);
        
        Debug.Log($"[UI_ScoreBoard] 새 플레이어 추가: {playerName}({clientId}), 초기 점수: {score}");
    }
    
    // 점수에 따라 플레이어 순위 업데이트
    private void UpdatePlayerRankings()
    {
        // 점수 내림차순으로 정렬 (높은 점수가 위에 표시)
        _playerScoreInfos.Sort((a, b) => b.Score.CompareTo(a.Score));
        // 1위는 Score바 바로 아래, 2위부터는 아래로 쌓임
        for (int i = 0; i < _playerScoreInfos.Count; i++)
        {
            RectTransform rectTransform = _playerScoreInfos[i].TextElement.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(-10, -50 - (i * verticalSpacing));
            }
        }
        Debug.Log("[UI_ScoreBoard] 점수 순위대로 UI 위치 재배치 완료");
    }
    
    /// <summary>
    /// 점수 정보를 문자열로 변환
    /// </summary>
    public string GetScoreboardText()
    {
        string result = titleFormat + "\n";
        
        // 정렬된 플레이어 정보로 텍스트 생성
        foreach (var playerInfo in _playerScoreInfos)
        {
            result += string.Format(playerScoreFormat, playerInfo.PlayerName, playerInfo.Score) + "\n";
        }
        
        return result;
    }
    
    private void OnDestroy()
    {
        if (_gameState != null)
        {
            _gameState.OnScoreUpdated -= UpdateScoreUI;
        }
    }
} 