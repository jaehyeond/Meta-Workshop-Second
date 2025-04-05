using UnityEngine;

/// <summary>
/// DebugManager의 정보를 화면에 표시하는 클래스
/// 필요할 때만 생성하여 사용
/// </summary>
public class DebugGUIHandler : MonoBehaviour
{
    private DebugManager _debugManager;
    
    public static DebugGUIHandler Create(DebugManager debugManager)
    {
        GameObject go = new GameObject("@DebugGUIHandler");
        DebugGUIHandler handler = go.AddComponent<DebugGUIHandler>();
        handler._debugManager = debugManager;
        DontDestroyOnLoad(go);
        return handler;
    }
    
    private void OnGUI()
    {
        if (_debugManager == null || !_debugManager.IsDebugMode() || !_debugManager.IsGUIVisible())
            return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = _debugManager.GetDebugTextColor();
        style.wordWrap = true;
        
        GUI.Label(new Rect(10, 10, Screen.width - 20, 200), _debugManager.GetDebugInfo(), style);
    }
} 