using UnityEngine;
using VContainer;
using System;
using System.Threading.Tasks;

namespace Unity.Assets.Scripts.Data
{
    public class GameDataManager
    {
        public event Action<UserGameData> OnGameDataUpdated;
        
        [Inject] private IDataRepository _dataRepository;
        [Inject] private UserDataManager _userDataManager;
        [Inject] private CurrencyManager _currencyManager;
        
        private UserGameData _gameData;
        
        [Inject]
        public void Initialize()
        {
            LoadAllData();
        }
        
        private async void LoadAllData()
        {
            try
            {
                _gameData = await _dataRepository.LoadAllUserData();
                
                // 각 매니저에 데이터 분배
                if (_gameData != null)
                {
                    await _userDataManager.UpdateUserData(_gameData.UserInfo);
                    _currencyManager.InitializeCurrencies(_gameData.Currencies);
                    
                    OnGameDataUpdated?.Invoke(_gameData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"데이터 로드 중 오류 발생: {e.Message}");
            }
        }
        
        public async Task SaveAllData()
        {
            if (_gameData == null)
                return;
                
            try
            {
                // 최신 데이터로 업데이트
                _gameData.UserInfo = _userDataManager.CurrentUserData;
                _gameData.Currencies = _currencyManager.GetAllCurrencies();
                
                await _dataRepository.SaveAllUserData(_gameData);
            }
            catch (Exception e)
            {
                Debug.LogError($"데이터 저장 중 오류 발생: {e.Message}");
            }
        }
        
        public GameProgressData GetProgress() => _gameData.Progress;
        public LimitationData GetLimitations() => _gameData.Limitations;
        
        public async Task UpdateProgress(GameProgressData progress)
        {
            _gameData.Progress = progress;
            await _dataRepository.SaveGameProgress(progress);
            OnGameDataUpdated?.Invoke(_gameData);
        }
        
        public async Task UpdateLimitations(LimitationData limitations)
        {
            _gameData.Limitations = limitations;
            await _dataRepository.SaveLimitations(limitations);
            OnGameDataUpdated?.Invoke(_gameData);
        }
    }
} 