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
    [SerializeField] private float verticalSpacing = 30f;         // 플레이어 텍스트 사이의 수직 간격
    [SerializeField] private float initialOffset = 40f;           // 제목과 첫 번째 플레이어 텍스트 사이의 간격
    
    private BasicGameState _gameState;
    private Dictionary<ulong, TextMeshProUGUI> _playerTextElements = new Dictionary<ulong, TextMeshProUGUI>();
    
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
            
            // 점수에 따라 색상 설정
            Color textColor = neutralScoreColor;
            if (newScore > 0) textColor = positiveScoreColor;
            else if (newScore < 0) textColor = negativeScoreColor;
            
            textElement.text = string.Format(playerScoreFormat, playerName, newScore);
            textElement.color = textColor;
            
            Debug.Log($"[UI_ScoreBoard] 점수 업데이트 완료: {playerName}({clientId}) -> {newScore}");
        }
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
        
        // 점수에 따라 색상 설정
        Color textColor = neutralScoreColor;
        if (score > 0) textColor = positiveScoreColor;
        else if (score < 0) textColor = negativeScoreColor;
        
        // 텍스트 및 색상 설정
        newTextElement.text = string.Format(playerScoreFormat, playerName, score);
        newTextElement.color = textColor;
        
        // 텍스트 요소의 위치를 조정하여 겹치지 않도록 함
        RectTransform rectTransform = newTextElement.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 플레이어 텍스트 개수에 따라 위치 설정
            float yPosition = -initialOffset - (_playerTextElements.Count * verticalSpacing);
            Vector3 localPosition = rectTransform.localPosition;
            localPosition.y = yPosition;
            rectTransform.localPosition = localPosition;
            
            Debug.Log($"[UI_ScoreBoard] 플레이어 텍스트 위치 설정: {playerName} -> y={yPosition}");
        }
        
        // 사전에 추가
        _playerTextElements.Add(clientId, newTextElement);
        
        Debug.Log($"[UI_ScoreBoard] 새 플레이어 추가: {playerName}({clientId}), 초기 점수: {score}");
    }
    
    // 점수가 변경되면 전체 텍스트 위치 재정렬
    private void RearrangeAllTextElements()
    {
        int index = 0;
        foreach (var entry in _playerTextElements)
        {
            TextMeshProUGUI textElement = entry.Value;
            RectTransform rectTransform = textElement.GetComponent<RectTransform>();
            
            if (rectTransform != null)
            {
                float yPosition = -initialOffset - (index * verticalSpacing);
                Vector3 localPosition = rectTransform.localPosition;
                localPosition.y = yPosition;
                rectTransform.localPosition = localPosition;
                
                index++;
            }
        }
    }
    
    /// <summary>
    /// 점수 정보를 문자열로 변환
    /// </summary>
    public string GetScoreboardText()
    {
        string result = titleFormat + "\n";
        foreach (var entry in _playerTextElements)
        {
            result += entry.Value.text + "\n";
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