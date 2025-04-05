using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Resource;
using VContainer;

/// <summary>
/// 특정 클래스만 디버깅할 수 있는 Facade 클래스
/// </summary>
public class DebugClassFacade
{
    // 싱글톤 인스턴스
    [Inject] private DebugManager _debugManager;

    private static DebugClassFacade s_instance;
    public static DebugClassFacade Instance
    {
        get
        {
            if (s_instance == null)
            {
                Initialize();
            }
            return s_instance;
        }
    }

    // DebugManager 인스턴스
    // private DebugManager _debugManager;

    // 클래스별 디버깅 설정 저장
    private Dictionary<string, Color> _classColors = new Dictionary<string, Color>();
    private HashSet<string> _enabledClasses = new HashSet<string>();
    
    // 기본 생성자는 private으로 설정 (싱글톤 패턴)
    private DebugClassFacade()
    {            
        Initialize();
        EnableClass("ResourceInstaller", Color.blue);
        EnableClass("UIManager", Color.cyan);
        EnableClass("ResourceManager", Color.cyan);
        EnableClass("ConnectionManager", Color.green);
        EnableClass("NetworkManager", Color.magenta);
        // ----------------------------------------------------
        EnableClass("ConnectionState", Color.magenta);
        EnableClass("OfflineState", Color.magenta);
        EnableClass("LobbyConnectingState", Color.magenta);
        EnableClass("ClientConnectingState", Color.magenta);
        EnableClass("ClientConnectedState", Color.magenta);
        EnableClass("ClientReconnectingState", Color.magenta);
        EnableClass("StartingHostState", Color.magenta);
        EnableClass("HostingState", Color.magenta);
        EnableClass("ConnectionState", Color.magenta);
        EnableClass("SceneManagerEx", Color.magenta);


        // -----------------------------------------------------
        // EnableClass("ObjectManagerFacade", Color.magenta);
        // EnableClass("MapSpawnerFacade", Color.magentas);
        // EnableClass("ObjectManager", Color.gray);
        // -----------------------------------------------------
        EnableClass("ServerMonster", Color.green);

        // -----------------------------------------------------
        EnableClass("LobbyUIMediator", Color.red);
            // s_instance.EnableClass(typeof(UIManager), Color.cyan);
        // 순환 참조 방지: 생성자에서 DebugManager.Instance를 직접 호출하지 않음
        // _debugManager는 나중에 SetDebugManager 메서드를 통해 설정됨
    }
    

    
    /// <summary>
    /// DebugClassFacade 초기화 및 기본 클래스 설정
    /// </summary>
    public static void Initialize()
    {

    }
    
    /// <summary>
    /// 색상을 HEX 문자열로 변환
    /// </summary>
    private string ColorToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }
    
    /// <summary>
    /// 클래스 로깅 활성화 및 색상 설정
    /// </summary>
    /// <param name="className">클래스 이름</param>
    /// <param name="color">로그 색상</param>
    public void EnableClass(string className, Color color)
    {
        _classColors[className] = color;
        _enabledClasses.Add(className);
    }
    
    /// <summary>
    /// 타입으로 클래스 로깅 활성화 및 색상 설정
    /// </summary>
    /// <param name="classType">클래스 타입</param>
    /// <param name="color">로그 색상</param>
    public void EnableClass(Type classType, Color color)
    {
        EnableClass(classType.Name, color);
    }
    
    /// <summary>
    /// 제네릭 타입으로 클래스 로깅 활성화 및 색상 설정
    /// </summary>
    /// <typeparam name="T">클래스 타입</typeparam>
    /// <param name="color">로그 색상</param>
    public void EnableClass<T>(Color color)
    {
        EnableClass(typeof(T).Name, color);
    }
    
    /// <summary>
    /// 클래스 로깅 비활성화
    /// </summary>
    /// <param name="className">클래스 이름</param>
    public void DisableClass(string className)
    {
        _enabledClasses.Remove(className);
    }
    
    /// <summary>
    /// 타입으로 클래스 로깅 비활성화
    /// </summary>
    /// <param name="classType">클래스 타입</param>
    public void DisableClass(Type classType)
    {
        DisableClass(classType.Name);
    }
    
    /// <summary>
    /// 제네릭 타입으로 클래스 로깅 비활성화
    /// </summary>
    /// <typeparam name="T">클래스 타입</typeparam>
    public void DisableClass<T>()
    {
        DisableClass(typeof(T).Name);
    }
    
    /// <summary>
    /// 클래스 로깅 활성화 여부 확인
    /// </summary>
    /// <param name="className">클래스 이름</param>
    /// <returns>활성화 여부</returns>
    public bool IsClassEnabled(string className)
    {
        return _enabledClasses.Contains(className);
    }
    
    /// <summary>
    /// 클래스 로그 출력
    /// </summary>
    /// <param name="className">클래스 이름</param>
    /// <param name="message">로그 메시지</param>
    /// <param name="logType">로그 타입</param>
    public void Log(string className, string message, LogType logType = LogType.Log)
    {
        // 디버그 정보 출력

        
        // DebugManager가 설정되지 않았거나 디버그 모드가 비활성화되어 있거나 해당 클래스가 활성화되어 있지 않으면 로깅하지 않음
        if (_debugManager == null)
        {
            // _debugManager가 null인 경우에도 로그 출력
            if (IsClassEnabled(className))
            {
                // 클래스 색상 가져오기
                Color fallbackColor = Color.white;
                if (_classColors.TryGetValue(className, out Color foundColor))
                {
                    fallbackColor = foundColor;
                }
                
                // 색상 HEX 코드 가져오기
                string fallbackColorHex = ColorToHex(fallbackColor);
                
                // 클래스 이름을 포함한 메시지 로깅 (Rich Text 태그 사용)
                string fallbackMessage = $"<color=#{fallbackColorHex}>[{className}] {message}</color>";
                
                // 직접 Debug.Log 사용
                switch (logType)
                {
                    case LogType.Log:
                        Debug.Log(fallbackMessage);
                        break;
                    case LogType.Warning:
                        Debug.LogWarning(fallbackMessage);
                        break;
                    case LogType.Error:
                        Debug.LogError(fallbackMessage);
                        break;
                }
            }
            return;
        }
        
        // DebugManager가 비활성화되어 있거나 해당 클래스가 활성화되어 있지 않으면 로깅하지 않음
        if (!_debugManager.IsDebugMode() || !IsClassEnabled(className))
            return;
            
        // 클래스 색상 가져오기
        Color classColor = Color.white;
        if (_classColors.TryGetValue(className, out Color color))
        {
            classColor = color;
        }
        
        // 색상 HEX 코드 가져오기
        string colorHex = ColorToHex(classColor);
        
        // 클래스 이름을 포함한 메시지 로깅 (Rich Text 태그 사용)
        string formattedMessage = $"<color=#{colorHex}>[{className}] {message}</color>";
        
        // 디버그 매니저를 통해 로그 출력
        switch (logType)
        {
            case LogType.Log:
                Debug.Log(formattedMessage);
                break;
            case LogType.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            case LogType.Error:
                Debug.LogError(formattedMessage);
                break;
        }
        
        // 디버그 정보 업데이트 (화면에 표시)
        if (_debugManager != null)
        {
            _debugManager.UpdateDebugInfo(formattedMessage, false);
        }
    }
    
    /// <summary>
    /// 타입으로 클래스 로그 출력
    /// </summary>
    /// <param name="classType">클래스 타입</param>
    /// <param name="message">로그 메시지</param>
    /// <param name="logType">로그 타입</param>
    public void Log(Type classType, string message, LogType logType = LogType.Log)
    {
        Log(classType.Name, message, logType);
    }
    
    /// <summary>
    /// 일반 로그 출력
    /// </summary>
    /// <param name="className">클래스 이름</param>
    /// <param name="message">로그 메시지</param>
    public void LogInfo(string className, string message)
    {
        Log(className, message, LogType.Log);
    }
    
    /// <summary>
    /// 경고 로그 출력
    /// </summary>
    /// <param name="className">클래스 이름</param>
    /// <param name="message">로그 메시지</param>
    public void LogWarning(string className, string message)
    {
        Log(className, message, LogType.Warning);
    }
    
    /// <summary>
    /// 에러 로그 출력
    /// </summary>
    /// <param name="className">클래스 이름</param>
    /// <param name="message">로그 메시지</param>
    public void LogError(string className, string message)
    {
        Log(className, message, LogType.Error);
    }
    
    /// <summary>
    /// 모든 클래스 로깅 비활성화
    /// </summary>
    public void DisableAllClasses()
    {
        _enabledClasses.Clear();
    }
    
    /// <summary>
    /// 클래스 색상 변경
    /// </summary>
    /// <param name="className">클래스 이름</param>
    /// <param name="color">새 색상</param>
    public void SetClassColor(string className, Color color)
    {
        if (_classColors.ContainsKey(className))
        {
            _classColors[className] = color;
        }
    }
    
    /// <summary>
    /// 모든 활성화된 클래스 목록 가져오기
    /// </summary>
    /// <returns>활성화된 클래스 목록</returns>
    public string[] GetEnabledClasses()
    {
        string[] classes = new string[_enabledClasses.Count];
        _enabledClasses.CopyTo(classes);
        return classes;
    }

}