using UnityEngine;
using VContainer;
using System;
using System.Threading.Tasks;

namespace Unity.Assets.Scripts.Data
{
    public class UserDataManager
    {
        public event Action<UserData> OnUserDataChanged;
        
        [Inject] private IDataRepository _dataRepository;
        private UserData _userData;

        public UserData CurrentUserData => _userData;

        public async Task UpdateUserData(UserData newData)
        {
            _userData = newData;
            await _dataRepository.SaveUserInfo(_userData);
            OnUserDataChanged?.Invoke(_userData);
        }
    }


} 