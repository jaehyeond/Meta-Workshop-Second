using UnityEngine;
using VContainer;

/// <summary>
/// DebugManager와 DebugClassFacade 사용 예시
/// </summary>
public class DebugExample : MonoBehaviour
{
    [Inject] private DebugManager _debugManager;
    [Inject] private DebugClassFacade _debugClassFacade;

    private void Start()
    {
        // DebugManager 초기화 및 GUI 핸들러 생성
        _debugManager.CreateGUIHandler();
        
        // 디버그 모드 설정
        _debugManager.SetDebugMode(true);
        
        // GUI 표시 설정
        _debugManager.SetGUIVisible(true);
        
        // 디버그 텍스트 색상 설정
        _debugManager.SetDebugTextColor(Color.yellow);
        
        // 디버그 정보 업데이트
        _debugManager.UpdateDebugInfo("디버그 시작");
        
        // 로그 출력
        _debugManager.Log("일반 로그 메시지");
        _debugManager.Log("경고 메시지", LogType.Warning);
        _debugManager.Log("에러 메시지", LogType.Error);
        
        // 특정 클래스 로깅 활성화 및 색상 설정
        _debugClassFacade.EnableClass("ResourceInstaller", Color.blue);
        _debugClassFacade.EnableClass(typeof(DebugExample), Color.green);
        
        // 로그 출력
        _debugClassFacade.LogInfo("ResourceInstaller", "리소스 로딩 시작");
        _debugClassFacade.LogWarning("ResourceInstaller", "리소스 로딩 지연");
        _debugClassFacade.LogError("ResourceInstaller", "리소스 로딩 실패");
        
        // 타입으로 로그 출력
        _debugClassFacade.Log(typeof(DebugExample), "디버그 예제 초기화 완료");
        
        // 특정 클래스 로깅 비활성화
        _debugClassFacade.DisableClass("ResourceInstaller");
        
        // 비활성화된 클래스 로그 출력 (출력되지 않음)
        _debugClassFacade.LogInfo("ResourceInstaller", "이 메시지는 출력되지 않음");
        
        // 클래스 색상 변경
        _debugClassFacade.EnableClass("ResourceInstaller", Color.cyan);
        _debugClassFacade.SetClassColor("ResourceInstaller", Color.magenta);
        
        // 다시 활성화된 클래스 로그 출력
        _debugClassFacade.LogInfo("ResourceInstaller", "색상이 변경된 로그");
        
        // 활성화된 클래스 목록 가져오기
        string[] enabledClasses = _debugClassFacade.GetEnabledClasses();
        string classesInfo = "활성화된 클래스: " + string.Join(", ", enabledClasses);
        Debug.Log(classesInfo);
    }
    
    private void Update()
    {
        // 키 입력에 따라 GUI 표시 여부 토글
        if (Input.GetKeyDown(KeyCode.G))
        {
            bool isVisible = _debugManager.IsGUIVisible();
            _debugManager.SetGUIVisible(!isVisible);
            Debug.Log($"GUI 표시: {!isVisible}");
        }
        
        // 키 입력에 따라 디버그 모드 토글
        if (Input.GetKeyDown(KeyCode.D))
        {
            bool isDebugMode = _debugManager.IsDebugMode();
            _debugManager.SetDebugMode(!isDebugMode);
            Debug.Log($"디버그 모드: {!isDebugMode}");
        }
    }
    
    private void OnDestroy()
    {
        // GUI 핸들러 제거
        _debugManager.DestroyGUIHandler();
    }
} 