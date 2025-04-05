using UnityEngine;
using UnityEngine.UI;

namespace Unity.Assets.Scripts.UI
{
    /// <summary>
    /// UI_StartUpScene의 진행률 UI 관련 기능
    /// </summary>
    public partial class UI_StartUpScene
    {
        #region Progress Management
        
        /// <summary>
        /// 진행률과 상태를 업데이트합니다.
        /// </summary>
        public void UpdateProgress(float progress, string status)
        {
            Progress = Mathf.Clamp01(progress);
            Status = status;
            
            LogDebug($"[UI_StartUpScene] Progress Update: {Progress:P0} - {Status} (Step: {_currentStep})");
            OnProgressChanged?.Invoke(Progress, Status);

            if (Mathf.Approximately(Progress, 1f))
            {
                OnProgressComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// UI 업데이트 메서드
        /// </summary>
        private void UpdateProgressUI(float progress, string status)
        {
            try
            {
                UpdateFillImage(progress);
                UpdateStatusText(status);
                UpdateDebugInfo($"Process: {progress * 100:F0}%\n {status}\n {_currentStep}");
            }
            catch (System.Exception e)
            {
                LogError($"[UI_StartUpScene] UI 업데이트 중 오류 발생: {e.Message}");
                UpdateDebugInfo($"Error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Fill 이미지 업데이트
        /// </summary>
        private void UpdateFillImage(float progress)
        {
            Image fillImage = GetImage((int)Images.Fill);
            if (fillImage == null) return;
            
            RectTransform rectTransform = fillImage.rectTransform;
            if (rectTransform == null) return;
            
            // 앵커 설정 확인 및 조정 (처음 한 번만)
            if (!_isProgressBarInitialized)
            {
                InitializeProgressBar(rectTransform);
            }
            
            // 부모 RectTransform 가져오기
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                // 부모 너비에 비례하여 Fill 이미지 너비 설정
                float targetWidth = parentRect.rect.width * progress;
                Vector2 sizeDelta = rectTransform.sizeDelta;
                sizeDelta.x = targetWidth;
                rectTransform.sizeDelta = sizeDelta;
                
                // LogDebug($"[UI_StartUpScene] Fill 이미지 너비 설정: {progress * 100:F0}% (너비: {targetWidth})");
            }
        }
        
        /// <summary>
        /// 프로그레스 바 초기화
        /// </summary>
        private void InitializeProgressBar(RectTransform rectTransform)
        {
            // 왼쪽 정렬을 위한 앵커 설정
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);
            
            // 부모 RectTransform 가져오기
            RectTransform parentRectTransform = rectTransform.parent as RectTransform;
            if (parentRectTransform != null)
            {
                // 초기 위치 설정 (왼쪽 정렬)
                rectTransform.anchoredPosition = new Vector2(0, 0);
                
                // 초기 너비는 0
                Vector2 sizeDelta = rectTransform.sizeDelta;
                sizeDelta.x = 0;
                rectTransform.sizeDelta = sizeDelta;
                
                // 높이는 부모와 동일하게
                sizeDelta.y = parentRectTransform.rect.height;
                rectTransform.sizeDelta = sizeDelta;
            }
            
            _isProgressBarInitialized = true;
        }
        
        /// <summary>
        /// 상태 텍스트 업데이트
        /// </summary>
        private void UpdateStatusText(string status)
        {
            var displayText = GetText((int)Texts.DisplayText);
            if (displayText != null)
            {
                displayText.text = status;
            }
        }
        
        #endregion
    }
} 