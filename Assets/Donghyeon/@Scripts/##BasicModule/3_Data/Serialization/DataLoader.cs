using VContainer;
using VContainer.Unity;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System;
using Unity.Assets.Scripts.Resource;
namespace Unity.Assets.Scripts.Data
{

    public interface ILoader<Key, Value>
    {
        Dictionary<Key, Value> MakeDict();
    }
    
    public class DataLoader : IStartable
    {

        [Inject] private DebugClassFacade _debugClassFacade;
        [Inject] private ResourceManager _resourceManager;

        private bool _isInitialized = false;
        public event Action OnInitialized;
        public bool IsInitialized => _isInitialized;

        public Dictionary<int, Data.BallData> BallDic { get; private set; } = new Dictionary<int, Data.BallData>();
        public Dictionary<int, Data.HeroData> HeroDic { get; private set; } = new Dictionary<int, Data.HeroData>();
        public Dictionary<int, Data.SkillData> SkillDic { get; private set; } = new Dictionary<int, Data.SkillData>();
        public Dictionary<int, Data.ProjectileData> ProjectileDic { get; private set; } = new Dictionary<int, Data.ProjectileData>();
        public Dictionary<int, Data.EnvData> EnvDic { get; private set; } = new Dictionary<int, Data.EnvData>();
        public Dictionary<int, Data.EffectData> EffectDic { get; private set; } = new Dictionary<int, Data.EffectData>();
        public Dictionary<int, Data.AoEData> AoEDic { get; private set; } = new Dictionary<int, Data.AoEData>();
        public static DataLoader instance;



        public void Start() // VContainer가 자동으로 호출
        {
            instance = this;
            _resourceManager.OnLoadingCompleted += OnResourceLoadingCompleted;

            if (_resourceManager.Resources.Count > 0)
            {
                Debug.Log("[DataLoader] 리소스가 이미 로드되어 있습니다. 바로 초기화합니다.");
                Init();
            }
            else
            {
                Debug.Log("[DataLoader] ResourceManager의 리소스 로딩 완료를 기다립니다.");
            }
        }
        
        private void OnResourceLoadingCompleted()
        {            
            // 이미 초기화되었는지 확인
            if (_isInitialized)
            {
                Debug.Log("[DataLoader] 이미 초기화되었습니다. 중복 초기화를 방지합니다.");
                return;
            }
            Init();
            _resourceManager.OnLoadingCompleted -= OnResourceLoadingCompleted;
        }

        public void Init()
        {
            if (_isInitialized)
            {
                Debug.Log("[DataLoader] 이미 초기화되었습니다. 중복 초기화를 방지합니다.");
                return;
            }
            
 
            // BallDic = LoadJsonToResoureManager<Data.BallDataLoader, int, Data.BallData>("BallData").MakeDict();
            // HeroDic = LoadJsonToResoureManager<Data.HeroDataLoader, int, Data.HeroData>("HeroData").MakeDict();
            // SkillDic = LoadJsonToResoureManager<Data.SkillDataLoader, int, Data.SkillData>("SkillData").MakeDict();
            // EffectDic = LoadJsonToResoureManager<Data.EffectDataLoader, int, Data.EffectData>("EffectData").MakeDict();
            // AoEDic = LoadJsonToResoureManager<Data.AoEDataLoader, int, Data.AoEData>("AoEData").MakeDict();
            _isInitialized = true;
            
            // 초기화 완료 이벤트 발생
            OnInitialized?.Invoke();
        }


        private Loader LoadJsonToResoureManager<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
        {
            TextAsset textAsset = _resourceManager.LoadJson<TextAsset>(path);
            return JsonConvert.DeserializeObject<Loader>(textAsset.text);
        }



    }
}