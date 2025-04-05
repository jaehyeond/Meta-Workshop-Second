using System;
using UnityEngine;

/// <summary>
/// 디버깅 관련 기능을 제공하는 클래스
/// </summary>
/// 
    /*
    // DebugClassFacade 사용 예시
    
    // 1. 특정 클래스 로깅 활성화 및 색상 설정
    DebugClassFacade.Instance.EnableClass("ResourceInstaller", Color.blue);
    DebugClassFacade.Instance.EnableClass(typeof(PlayerController), Color.green);
    DebugClassFacade.Instance.EnableClass<UIManager>(Color.cyan);
    
    // 2. 로그 출력
    DebugClassFacade.Instance.LogInfo("ResourceInstaller", "리소스 로딩 시작");
    DebugClassFacade.Instance.LogWarning("ResourceInstaller", "리소스 로딩 지연");
    DebugClassFacade.Instance.LogError("ResourceInstaller", "리소스 로딩 실패");
    
    // 3. 타입으로 로그 출력
    DebugClassFacade.Instance.Log(typeof(PlayerController), "플레이어 초기화 완료");
    
    // 4. 특정 클래스 로깅 비활성화
    DebugClassFacade.Instance.DisableClass("ResourceInstaller");
    
    // 5. 클래스 색상 변경
    DebugClassFacade.Instance.SetClassColor("UIManager", Color.yellow);
    
    // 6. 모든 클래스 로깅 비활성화
    DebugClassFacade.Instance.DisableAllClasses();
    
    // 7. 활성화된 클래스 목록 가져오기
    string[] enabledClasses = DebugClassFacade.Instance.GetEnabledClasses();
    */
    
    /*
    // DI 방식으로 사용하는 예시
    
    // VContainer 등록 예시
    public class DebugInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // DebugManager를 싱글톤으로 등록
            builder.Register<DebugManager>(Lifetime.Singleton);
            
            // DebugClassFacade를 싱글톤으로 등록
            builder.Register<DebugClassFacade>(Lifetime.Singleton);
        }
    }
    
    // 사용 예시
    public class ResourceInstaller
    {
        private readonly DebugClassFacade _logger;
        
        // 생성자 주입
        public ResourceInstaller(DebugClassFacade logger)
        {
            _logger = logger;
            
            // 이 클래스의 로깅 활성화 (파란색)
            _logger.EnableClass(GetType(), Color.blue);
            
            // 로그 출력
            _logger.LogInfo(GetType().Name, "ResourceInstaller 생성됨");
        }
    }
    */
    

public class DebugManager
{
    private static DebugManager s_Instance;
    public static DebugManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = new DebugManager();
            }
            return s_Instance;
        }
    }
    
    // 디버그 모드 설정
    protected bool _debugMode = true;
    protected Color _debugTextColor = Color.yellow;
    protected bool _showGUI = true;
    protected string _debugInfo = "";
    
    // GUI 핸들러 Example
    //     public class GameManager : MonoBehaviour
    // {
    //     private void Start()
    //     {
    //         // 디버그 매니저 설정
    //         DebugManager.Instance.CreateGUIHandler();
    //         DebugManager.Instance.SetGUIVisible(true);
            
    //         // 디버그 모드 설정 (필요한 경우)
    //         DebugManager.Instance.SetDebugMode(true);
            
    //         // 디버그 텍스트 색상 설정 (필요한 경우)
    //         DebugManager.Instance.SetDebugTextColor(Color.green);
    //     }
    // }
    private DebugGUIHandler _guiHandler;
    
    // 디버그 로그 이벤트
    public event Action<string> OnDebugLogUpdated;
    
    // 생성자
    public DebugManager()
    {
        // 기본 초기화
        // DebugClassFacade.Initialize();
        // // 순환 참조 방지: DebugClassFacade에 현재 인스턴스 설정
        // DebugClassFacade.Instance.SetDebugManager(this);
    }
    
    /// <summary>
    /// GUI 핸들러 생성
    /// </summary>
    public void CreateGUIHandler()
    {
        if (_guiHandler == null)
        {
            _guiHandler = DebugGUIHandler.Create(this);
        }
    }
    
    /// <summary>
    /// GUI 핸들러 제거
    /// </summary>
    public void DestroyGUIHandler()
    {
        if (_guiHandler != null)
        {
            GameObject.Destroy(_guiHandler.gameObject);
            _guiHandler = null;
        }
    }
    
    /// <summary>
    /// GUI 표시 여부 설정
    /// </summary>
    public void SetGUIVisible(bool isVisible)
    {
        _showGUI = isVisible;
        
        if (isVisible && _guiHandler == null)
        {
            CreateGUIHandler();
        }
        else if (!isVisible && _guiHandler != null)
        {
            _guiHandler.gameObject.SetActive(false);
        }
        else if (isVisible && _guiHandler != null)
        {
            _guiHandler.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// GUI 표시 여부 확인
    /// </summary>
    public bool IsGUIVisible()
    {
        return _showGUI;
    }
    
    /// <summary>
    /// 디버그 모드 활성화 여부 설정
    /// </summary>
    public virtual void SetDebugMode(bool isActive)
    {
        _debugMode = isActive;
    }
    
    /// <summary>
    /// 디버그 텍스트 색상 설정
    /// </summary>
    public virtual void SetDebugTextColor(Color color)
    {
        _debugTextColor = color;
    }
    
    /// <summary>
    /// 디버그 정보 업데이트
    /// </summary>
    public virtual void UpdateDebugInfo(string info, bool logToConsole = true)
    {
        if (!_debugMode) return;
        
        _debugInfo = info;
        OnDebugLogUpdated?.Invoke(info);
        
        if (logToConsole)
        {
            Debug.Log($"[DebugManager] {info}");
        }
    }
    
    /// <summary>
    /// 디버그 정보 가져오기
    /// </summary>
    public virtual string GetDebugInfo()
    {
        return _debugInfo;
    }
    
    /// <summary>
    /// 디버그 텍스트 색상 가져오기
    /// </summary>
    public virtual Color GetDebugTextColor()
    {
        return _debugTextColor;
    }
    
    /// <summary>
    /// 디버그 모드 여부 확인
    /// </summary>
    public virtual bool IsDebugMode()
    {
        return _debugMode;
    }
    
    /// <summary>
    /// 디버그 로그 출력
    /// </summary>
    public virtual void Log(string message, LogType logType = LogType.Log)
    {
        if (!_debugMode) return;
        
        switch (logType)
        {
            case LogType.Log:
                Debug.Log($"[DebugManager] {message}");
                break;
            case LogType.Warning:
                Debug.LogWarning($"[DebugManager] {message}");
                break;
            case LogType.Error:
                Debug.LogError($"[DebugManager] {message}");
                break;
        }
    }
    
    /// <summary>
    /// 디버그 정보를 화면에 표시
    /// </summary>
    private void OnGUI()
    {
        // 디버그 모드가 비활성화되어 있거나 GUI 표시가 비활성화되어 있으면 표시하지 않음
        if (!_debugMode || !_showGUI) 
            return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = _debugTextColor;
        style.wordWrap = true;
        
        GUI.Label(new Rect(10, 10, Screen.width - 20, 200), _debugInfo, style);
    }
    

} 