using System;
using System.Collections.Generic;

namespace Unity.Assets.Scripts.Data
{
    [Serializable]
    public class UserGameData
    {
        // 기본 유저 정보
        public UserData UserInfo;
        
        // 재화 정보
        public Dictionary<string, long> Currencies;
        
        // 게임 진행 정보
        public GameProgressData Progress;
        
        // 제한 정보
        public LimitationData Limitations;
    }

    
    [Serializable]
    public class UserData
    {
        public string UserId;
        public string Nickname;
        public int Level;
        public long Experience;
        public DateTime LastLoginTime;
    }




    [Serializable]
    public class GameProgressData
    {
        public int CurrentStage;
        public int HighestStage;
        public Dictionary<string, int> ChapterProgress;
        public List<string> CompletedAchievements;
        public Dictionary<string, int> QuestProgress;
    }

    [Serializable]
    public class LimitationData
    {
        public int RemainingEnergy;
        public int MaxEnergy;
        public DateTime EnergyLastRefillTime;
        public int DailyPlayCount;
        public DateTime DailyResetTime;
        public Dictionary<string, DateTime> CooldownTimers;
    }
} 