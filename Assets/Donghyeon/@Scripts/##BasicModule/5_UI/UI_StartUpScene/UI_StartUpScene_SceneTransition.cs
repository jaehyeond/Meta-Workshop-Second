using System.Collections;
using UnityEngine;
using Unity.Assets.Scripts.Scene;

namespace Unity.Assets.Scripts.UI
{
    /// <summary>
    /// UI_StartUpScene의 씬 전환 관련 기능
    /// </summary>
    public partial class UI_StartUpScene
    {
        #region Scene Transition
        
        /// <summary>
        /// 로딩 완료 처리
        /// </summary>
        private void OnLoadingComplete()
        {
            LogDebug("[UI_StartUpScene] 로딩 완료");
            
            try
            {
                _isResourceLoaded = true;
                
                // 인증 상태 확인 및 표시
                string authStatus = _isAuthenticated 
                    ? $"인증 완료: {_authManager?.PlayerId ?? "Unknown"}" 
                    : "오프라인 모드";
                
                UpdateDebugInfo($"Loading Complete\n{authStatus}");
                
                // 잠시 후 다음 씬으로 전환
                StartCoroutine(LoadNextSceneAfterDelay());
            }
            catch (System.Exception e)
            {
                LogError($"[UI_StartUpScene] 로딩 완료 처리 중 오류 발생: {e.Message}");
                UpdateDebugInfo($"Error: {e.Message}");
            }
        }
        
        /// <summary>
        /// 지정된 시간 후 다음 씬으로 전환
        /// </summary>
        private IEnumerator LoadNextSceneAfterDelay()
        {
            
            for (float t = 0; t < NEXT_SCENE_DELAY; t += 0.1f)
            {
                // 인증 상태 표시
                string authStatus = _isAuthenticated 
                    ? $"인증: {_authManager?.PlayerId ?? "Unknown"}" 
                    : "오프라인 모드";
                
                UpdateDebugInfo($"Load Finish! {NEXT_SCENE_DELAY - t:F1}초 후 이동\n{authStatus}");
                yield return new WaitForSeconds(0.1f);
            }
            
            UpdateDebugInfo("MainMenu");
            
            try
            {
                _startUpScene.LoadMainMenuScene();
            }
            catch (System.Exception e)
            {
                LogError($"[UI_StartUpScene] 씬 전환 중 오류 발생: {e.Message}");
                UpdateDebugInfo($"Error: {e.Message}");
            }
        }
        
        #endregion
    }
} 