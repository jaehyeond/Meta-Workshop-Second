using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using System.Linq;
using VContainer;
namespace Unity.Assets.Scripts.Resource
{


    public class ResourceManager
    {
        [Inject] private DebugClassFacade _debugClassFacade;

        [Inject] private PoolManager _poolManager;
        // 리소스 로딩 진행 상황을 알려주는 이벤트 (진행률 0.0f ~ 1.0f, 현재 로드된 항목, 총 항목 수)
        public event Action<float, string, int, int> OnLoadingProgressChanged;
        
        // 리소스 로딩 완료를 알려주는 이벤트
        public event Action OnLoadingCompleted;

        private Dictionary<string, UnityEngine.Object> _resources = new Dictionary<string, UnityEngine.Object>();
        private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
        
        // _resources 딕셔너리에 접근할 수 있는 속성 추가
        public IReadOnlyDictionary<string, UnityEngine.Object> Resources => _resources;

        #region Load Resource
        // 타입에 따른 키 접미사를 반환하는 메서드


        public T LoadJson<T>(string key) where T : Object
        {
            if (_resources.TryGetValue(key, out Object resource))
                return resource as T;
            if (typeof(T) == typeof(Sprite) && key.Contains(".sprite") == false)
            {
                if (_resources.TryGetValue($"{key}.sprite", out resource))
                    return resource as T;
            }

            return null;
        }


        public T Load<T>(string key) where T : Object
        {
            _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] Load<{typeof(T).Name}> 호출: 키 '{key}");
            
            // 1. 원래 키로 먼저 시도
            if (_resources.TryGetValue(key, out Object resource))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] 원래 키로 리소스 찾음: '{key}'");
                #endif
                return resource as T;
            }
            
            // 2. 타입 키로 시도
            string typeKey = GetTypeKey(key, typeof(T));
            if (_resources.TryGetValue(typeKey, out resource))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] 타입 키로 리소스 찾음: '{typeKey}'");
                #endif
                return resource as T;
            }
            
            // 3. 스프라이트 특수 처리 (기존 코드 유지)
            if (typeof(T) == typeof(Sprite) && !key.Contains(".sprite"))
            {
                if (_resources.TryGetValue($"{key}.sprite", out resource))
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] 스프라이트 특수 처리로 리소스 찾음: '{key}.sprite'");
                    #endif
                    return resource as T;
                }
            }
            
            
            return null;
        }

        public GameObject Instantiate(string key, Transform parent = null, bool pooling = false, Vector3 position = default, Quaternion rotation = default)
        {
            GameObject prefab = Load<GameObject>(key);
            if (prefab == null)
            {
                _debugClassFacade.LogError(GetType().Name, $"Failed to load prefab : {key}");
                return null;
            }

            if (pooling)
            	return _poolManager.Pop(prefab, position, rotation);

            GameObject go = Object.Instantiate(prefab, position, rotation, parent);
            go.name = prefab.name;

            return go;
        }

        public void Destroy(GameObject go)
        {
            if (go == null)
                return;

            if (_poolManager.Push(go))
            	return;

            Object.Destroy(go);
        }
        #endregion

        #region Addressable
        public void LoadAsync<T>(string key, Action<T> callback = null) where T : UnityEngine.Object
        {
            // Cache
            if (_resources.TryGetValue(key, out Object resource))
            {
                _debugClassFacade.LogInfo(GetType().Name, $"캐시에서 에셋 로드: {key}, 타입: {typeof(T).Name}");
                //[ResourceManager] 에셋 로드: green_slime (AnimatorController) - 키: green_slime, 타입 키: green_slime.animatorcontroller
                callback?.Invoke(resource as T);
                return;
            }

            string loadKey = key;
            if (key.Contains(".sprite"))
                loadKey = $"{key}[{key.Replace(".sprite", "")}]";

            _debugClassFacade.LogInfo(GetType().Name, $"Addressable에서 에셋 로드 시도: {loadKey}, 타입: {typeof(T).Name}");
            var asyncOperation = Addressables.LoadAssetAsync<T>(loadKey);
            asyncOperation.Completed += (op) =>
            {
                if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    _debugClassFacade.LogInfo(GetType().Name, $"에셋 로드 성공: {key}, 타입: {typeof(T).Name}");
                    _resources.Add(key, op.Result);
                    _handles.Add(key, asyncOperation);
                    callback?.Invoke(op.Result);
                }
                else
                {
                    _debugClassFacade.LogError(GetType().Name, $"에셋 로드 실패: {key}, 타입: {typeof(T).Name}, 오류: {op.OperationException?.Message}");
                    callback?.Invoke(null);
                }
            };
        }

        public void LoadAllAsync<T>(string label, Action<string, int, int> callback) where T : UnityEngine.Object
        {
            _debugClassFacade.LogInfo(GetType().Name, $"라벨 '{label}' 로드 시작");
            var operation = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            operation.Completed += op =>
            {
                if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    if (op.Result.Count == 0)
                    {
                        _debugClassFacade.LogWarning(GetType().Name, $"라벨 '{label}'에 해당하는 에셋이 없습니다.");
                        OnLoadingCompleted?.Invoke();
                        return;
                    }
                    
                    _debugClassFacade.LogInfo(GetType().Name, $"라벨 '{label}' 로드 성공: {op.Result.Count}개 에셋 발견");
                    
                    var loadOperations = new List<AsyncOperationHandle>();
                    int totalCount = op.Result.Count;
                    int loadedCount = 0;

                    foreach (var result in op.Result)
                    {
                        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(result);
                        handle.Completed += obj =>
                        {
                            loadedCount++;
                            string key = result.PrimaryKey;
                            
                            if (obj.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                            {
                                // 타입 정보를 포함한 고유한 키 생성
                                string typeKey = GetTypeKey(key, obj.Result.GetType());
                                
                                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                                string typeName = obj.Result.GetType().Name;
                                string assetName = obj.Result.name;
                                _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] 에셋 로드: {assetName} ({typeName}) - 키: {key}, 타입 키: {typeKey}");
                                //[ResourceManager] 에셋 로드: green_slime (AnimatorController) - 키: green_slime, 타입 키: green_slime.animatorcontroller
                                #endif
                                
                                // 원래 키와 타입 키 모두 저장
                                if (!_resources.ContainsKey(typeKey))
                                {
                                    // 타입 키로 저장
                                    _resources.Add(typeKey, obj.Result);
                                    _handles.Add(typeKey, handle);
                                    
                                    // 원래 키로도 저장 (이미 존재하지 않는 경우에만)
                                    if (!_resources.ContainsKey(key))
                                    {
                                        _resources.Add(key, obj.Result);
                                    }
                                }
                                else if (obj.Result is GameObject && !(_resources[typeKey] is GameObject))
                                {
                                    // GameObject인 경우 덮어쓰기
                                    _resources[typeKey] = obj.Result;
                                }
                                
                                // 콜백 호출
                                callback?.Invoke(typeKey, loadedCount, totalCount);
                                
                                // 진행 상황 이벤트 발생
                                float progress = (float)loadedCount / totalCount;
                                OnLoadingProgressChanged?.Invoke(progress, typeKey, loadedCount, totalCount);
                            }
                            else
                            {
                                _debugClassFacade?.LogError(GetType().Name, $"[ResourceManager] 에셋 로드 실패: {key}, 오류: {obj.OperationException?.Message}");
                                
                                // 실패해도 로드 카운트는 증가시켜야 함
                                callback?.Invoke(key, loadedCount, totalCount);
                                
                                // 진행 상황 이벤트 발생
                                float progress = (float)loadedCount / totalCount;
                                OnLoadingProgressChanged?.Invoke(progress, key, loadedCount, totalCount);
                            }
                            
                            // 모든 리소스 로드 완료
                            if (loadedCount >= totalCount)
                            {
                                Debug.Log($"[ResourceManager] 라벨 '{label}' 로드 완료: 총 {loadedCount}개 에셋");
                                OnLoadingCompleted?.Invoke();
                            }
                        };
                        loadOperations.Add(handle);
                    }
                }
                else
                {
                    OnLoadingCompleted?.Invoke();
                }
            };
        }
        

                // 타입 정보를 포함한 키 생성
        private string GetTypeKey(string key, Type type)
        {
            return $"{key}.{GetTypeSuffix(type)}";
        }

        private string GetTypeSuffix(Type type)
        {
            if (type == typeof(GameObject)) return "prefab";
            if (type == typeof(RuntimeAnimatorController)) return "controller";
            if (type == typeof(AnimationClip)) return "anim";
            if (type == typeof(Sprite)) return "sprite";
            if (type == typeof(Texture)) return "texture";
            if (type == typeof(Material)) return "material";
            if (type == typeof(AudioClip)) return "audio";
            return type.Name.ToLower();
        }


        // 디버그용 메서드: 로드된 모든 에셋 출력
        public void Clear()
        {
            _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] Clear 시작: {_resources.Count}개 리소스, {_handles.Count}개 핸들");
            
            // ScriptableObject 초기화
            
            // 핸들 해제
            int releasedCount = 0;
            int errorCount = 0;
            
            foreach (var handleEntry in _handles)
            {
                try
                {
                    string key = handleEntry.Key;
                    AsyncOperationHandle handle = handleEntry.Value;
                    
                    // 유효한 핸들인지 확인
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        releasedCount++;
                    }
                    else
                    {
                        _debugClassFacade?.LogWarning(GetType().Name, $"[ResourceManager] 유효하지 않은 핸들 무시: {key}");
                    }
                }
                catch (Exception ex)
                {
                    _debugClassFacade?.LogWarning(GetType().Name, $"[ResourceManager] 핸들 해제 중 오류 발생: {ex.Message}");
                    errorCount++;
                }
            }
            
            _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] 핸들 해제 완료: 성공 {releasedCount}개, 오류 {errorCount}개");
            
            // 컬렉션 초기화
            _resources.Clear();
            _handles.Clear();
            
            _debugClassFacade?.LogInfo(GetType().Name, "[ResourceManager] Clear 완료");
        }
        

        // 간단한 디버그용 메서드: 리소스 딕셔너리의 키와 값만 출력
        public void DebugSimpleResources()
        {
            Debug.Log($"[ResourceManager] === 리소스 딕셔너리 내용 (총 {_resources.Count}개) ===");
            
            // 타입별 통계
            var typeStats = _resources.Values
                .GroupBy(v => v.GetType().Name)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            // 타입별 통계 출력
            _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] 타입별 통계:");
            foreach (var stat in typeStats)
            {
                _debugClassFacade?.LogInfo(GetType().Name, $"- {stat.Type}: {stat.Count}개");
            }
            
            // 키 패턴 출력
            _debugClassFacade?.LogInfo(GetType().Name, $"[ResourceManager] 키 패턴 샘플 (최대 10개):");
            var sampleKeys = _resources.Keys.Take(10).ToList();
            foreach (var key in sampleKeys)
            {
                var value = _resources[key];
                _debugClassFacade?.LogInfo(GetType().Name, $"- {key} => {value.name} ({value.GetType().Name})");
            }
            
        }
        #endregion
    }
}