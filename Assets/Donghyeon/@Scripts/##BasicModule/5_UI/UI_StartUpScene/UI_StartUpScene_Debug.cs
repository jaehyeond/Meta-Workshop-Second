using UnityEngine;

namespace Unity.Assets.Scripts.UI
{
    /// <summary>
    /// UI_StartUpScene의 디버그 관련 기능
    /// </summary>
    public partial class UI_StartUpScene
    {
        #region Debug Methods
        
        /// <summary>
        /// 디버그 정보 업데이트
        /// </summary>
        private void UpdateDebugInfo(string info)
        {
            // DebugManager.Instance.UpdateDebugInfo(info);
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void LogDebug(string message)
        {
            // _debugClassFacade?.LogInfo(GetType().Name, message);
            
            // DebugManager.Instance.Log(message);
        }
        
        /// <summary>
        /// 디버그 경고 로그 출력
        /// </summary>
        private void LogWarning(string message)
        {
            // DebugManager.Instance.Log(message, LogType.Warning);
        }
        
        /// <summary>
        /// 디버그 오류 로그 출력
        /// </summary>
        private void LogError(string message)
        {
            // DebugManager.Instance.Log(message, LogType.Error);
        }
        
        #endregion
    }
} 