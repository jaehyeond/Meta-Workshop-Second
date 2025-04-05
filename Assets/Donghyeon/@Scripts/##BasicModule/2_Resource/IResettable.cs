using UnityEngine;

namespace Unity.Assets.Scripts.Resource
{
    /// <summary>
    /// 게임 모드 종료 시 초기화가 필요한 객체에 구현하는 인터페이스입니다.
    /// 주로 ScriptableObject에 구현하여 게임 모드 간 상태를 초기화합니다.
    /// </summary>
    public interface IResettable
    {
        /// <summary>
        /// 객체의 상태를 초기화합니다.
        /// 게임 모드 종료 시 ResourceManager에 의해 호출됩니다.
        /// </summary>
        void Reset();
    }
} 