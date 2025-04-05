using System;
using UnityEngine;

namespace Unity.Assets.Scripts.Objects
{
    [Serializable]
    public abstract class GuidScriptableObject : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        byte[] m_Guid;
        
        // 인스펙터에 표시할 GUID 문자열 필드 추가
        [Header("고유 식별자")]
        [SerializeField]
        private string guidString = "자동 생성됨";
        
        // 읽기 전용으로 만들기
        private bool showReadOnlyWarning = false;

        public Guid Guid => new Guid(m_Guid);

        void OnValidate()
        {
            if (m_Guid == null || m_Guid.Length == 0)
            {
                m_Guid = Guid.NewGuid().ToByteArray();
                UpdateGuidString();
            }
            
            // guidString이 변경되었는지 확인
            if (guidString != Guid.ToString())
            {
                // 원래 값으로 되돌림
                UpdateGuidString();
                showReadOnlyWarning = true;
            }
            else
            {
                showReadOnlyWarning = false;
            }
        }
        
        void OnEnable()
        {
            if (m_Guid != null && m_Guid.Length > 0)
            {
                UpdateGuidString();
            }
        }
        
        private void UpdateGuidString()
        {
            if (m_Guid != null && m_Guid.Length > 0)
            {
                guidString = Guid.ToString();
            }
        }
        
        // 에디터에서 경고 표시
        #if UNITY_EDITOR
        void OnGUI()
        {
            if (showReadOnlyWarning)
            {
                UnityEditor.EditorGUILayout.HelpBox("GUID는 수정할 수 없습니다.", UnityEditor.MessageType.Warning);
                showReadOnlyWarning = false;
            }
        }
        #endif
    }
}