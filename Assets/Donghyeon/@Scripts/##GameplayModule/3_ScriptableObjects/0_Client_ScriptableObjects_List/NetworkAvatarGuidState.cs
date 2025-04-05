using System;
// using Unity.Assets.Scripts.Gameplay.Configuration;
// using Unity.Assets.Scripts.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Assets.Scripts.Infrastructure;
// using Avatar = Unity.Assets.Scripts.Gameplay.Configuration.Avatar;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 이거 걍 서로 상대방에게  자신의 아바타 동기화하는 코드드
    /// </summary>
    /// <remarks>
    /// - 디버깅 포인트 1: AvatarGuid.Value가 유효한 GUID인지 확인
    /// - 디버깅 포인트 2: m_AvatarRegistry가 올바르게 참조되어 있는지 확인
    /// - 디버깅 포인트 3: RegisteredAvatar 프로퍼티 호출 시 m_Avatar가 null이 아닌지 확인
    /// </remarks>
    public class NetworkAvatarGuidState : NetworkBehaviour
    {
        /// <summary>
        /// 네트워크를 통해 동기화되는 아바타의 GUID입니다.
        /// 이 값이 변경되면 모든 클라이언트에 자동으로 동기화됩니다.
        /// </summary>
        [FormerlySerializedAs("AvatarGuidArray")]
        [HideInInspector]
        public NetworkVariable<NetworkGuid> AvatarGuid = new NetworkVariable<NetworkGuid>();

        /// <summary>
        /// 사용 가능한 아바타들의 레지스트리 참조입니다.
        /// Inspector에서 설정되어야 합니다.
        /// </summary>
        // [SerializeField]
        // AvatarRegistry m_AvatarRegistry;
        [SerializeField]
        // MonsterAvatarList m_MonsterAvatarList;
        /// <summary>
        /// 현재 등록된 아바타 인스턴스입니다.
        /// </summary>

        public void SetRandomAvatar()
        {
            Debug.Log("Setting random avatar...");
            // AvatarGuid.Value = m_AvatarRegistry.GetRandomAvatar().Guid.ToNetworkGuid();
        }

        /// <summary>
        /// 주어진 GUID에 해당하는 아바타를 등록합니다.
        /// </summary>
        /// <param name="guid">등록할 아바타의 GUID</param>
        /// <remarks>
        /// 디버깅 체크리스트:
        /// 1. guid가 Empty가 아닌지 확인
        /// 2. m_AvatarRegistry에서 아바타를 찾을 수 있는지 확인
        /// 3. 중복 등록이 발생하지 않는지 확인
        /// </remarks>
        // void RegisterAvatar(Guid guid)
        // {
        //     if (guid.Equals(Guid.Empty))
        //     {
        //         Debug.LogWarning("Attempted to register avatar with empty GUID");
        //         return;
        //     }

        //     // 레지스트리에서 아바타를 찾아 등록
        //     if (!m_MonsterAvatarList.TryGetAvatar(guid, out var avatar))
        //     {
        //         Debug.LogError($"Avatar not found for GUID: {guid}");
        //         return;
        //     }

        //     if (m_MonsterAvatar != null)
        //     {
        //         Debug.Log($"Avatar already registered with GUID: {guid}");
        //         return;
        //     }

        //     m_MonsterAvatar= avatar;
        //     // if (TryGetComponent<ServerCharacter>(out var serverCharacter))
        //     // {
        //     //     serverCharacter.CharacterClass = avatar.CharacterClass;
        //     // }
        //     Debug.Log($"Successfully registered avatar with GUID: {guid}");
        // }
    }
}
