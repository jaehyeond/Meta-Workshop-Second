using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Assets.Scripts.Data;

namespace Unity.Assets.Scripts.Data
{
    public interface IDataRepository
    {
        Task<UserGameData> LoadAllUserData();
        Task SaveAllUserData(UserGameData userData);
        
        // 개별 데이터 업데이트를 위한 메서드들
        Task SaveUserInfo(UserData userData);
        Task SaveCurrencies(Dictionary<string, long> currencies);
        Task SaveGameProgress(GameProgressData progress);
        Task SaveLimitations(LimitationData limitations);
    }
} 