using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Unity.Netcode;
using VContainer;

// 기존 풀 클래스에 NetworkObject 처리 추가
internal class Pool
{
	[Inject] private ObjectManagerFacade _objectManagerFacade;

	[Inject] private NetworkManager _networkManager;

	[Inject] private NetUtils _netUtils;
	private GameObject _prefab;
	private IObjectPool<GameObject> _pool;


    private bool _hasNetworkObject; // 추가: 네트워크 오브젝트 여부 플래그


	private Transform _root;
	private Transform Root
	{
		get
		{
			if (_root == null)
			{
				GameObject go = new GameObject() { name = $"@{_prefab.name}Pool" };
				_root = go.transform;
			}

			return _root;
		}
	}

	public Pool(GameObject prefab)
	{
		if (prefab == null)
		{
			Debug.LogError("[Pool] 프리팹이 null입니다.");
			throw new System.ArgumentNullException(nameof(prefab));
		}
		
		_prefab = prefab;		
		_hasNetworkObject = prefab.GetComponent<NetworkObject>() != null; // 네트워크 오브젝트 여부 확인

		try
		{
			_pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
			Debug.Log($"[Pool] '{prefab.name}' 풀 생성 성공");
			
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[Pool] 풀 생성 중 오류 발생: {e.Message}\n{e.StackTrace}");
			throw;
		}
	}

	public void Push(GameObject go)
	{
		if (go == null)
		{
			Debug.LogWarning("[Pool] Push: 게임 오브젝트가 null입니다.");
			return;
		}
		
		if (_pool == null)
		{
			Debug.LogError("[Pool] Push: 풀이 초기화되지 않았습니다.");
			return;
		}

        try
        {
            // NetworkObject 처리
            if (_hasNetworkObject)
            {
                NetworkObject networkObj = go.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.IsSpawned && _networkManager != null && _networkManager.IsListening)
                {
                    networkObj.Despawn(); // 네트워크에서 해제
                    Debug.Log($"[Pool] NetworkObject '{go.name}' Despawn 완료");
                }
            }
            
            // 풀에 반환
            if (go.activeSelf)
                _pool.Release(go);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Pool] Push 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }


	}

	public GameObject Pop(Vector3 position = default, Quaternion rotation = default)
	{
		if (_pool == null)
		{
			Debug.LogError("[Pool] Pop: 풀이 초기화되지 않았습니다.");
			return null;
		}
		
		try
		{
			GameObject go = _pool.Get();
			
			// 위치 설정
			go.transform.position = position;
			go.transform.rotation = rotation;
			
			return go;
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[Pool] Pop 중 오류 발생: {e.Message}\n{e.StackTrace}");
			
			// 풀에서 가져오기 실패 시 직접 생성
            GameObject go = GameObject.Instantiate(_prefab, position, rotation);
            go.transform.SetParent(Root);
            go.name = _prefab.name;
            go.SetActive(true);
            return go;
		}
	}

	#region Funcs
	private GameObject OnCreate()
	{
		GameObject go = GameObject.Instantiate(_prefab);
	
		go.name = _prefab.name;
		return go;
	}

	private void OnGet(GameObject go)
	{
		go.SetActive(true);
	}

	private void OnRelease(GameObject go)
	{
		go.SetActive(false);
	}

	private void OnDestroy(GameObject go)
	{
		GameObject.Destroy(go);
	}
	#endregion
}

// 풀 매니저
public class PoolManager
{
	private Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();

	public GameObject Pop(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
	{
		if (_pools.ContainsKey(prefab.name) == false)
			CreatePool(prefab);

		return _pools[prefab.name].Pop(position, rotation);
	}

	public bool Push(GameObject go)
	{
		if (_pools.ContainsKey(go.name) == false)
			return false;

		_pools[go.name].Push(go);
		return true;
	}

	public void Clear()
	{
		_pools.Clear();
	}
	
	// 네트워크 프리팹 풀 미리 준비
	private void CreatePool(GameObject original)
	{
		if (original == null)
		{
			Debug.LogError("[PoolManager] 풀 생성 실패: 프리팹이 null입니다.");
			return;
		}
		
		if (_pools.ContainsKey(original.name))
		{
			Debug.LogWarning($"[PoolManager] 이미 '{original.name}' 풀이 존재합니다.");
			return;
		}
		
		try
		{
			Pool pool = new Pool(original);
			_pools.Add(original.name, pool);
			Debug.Log($"[PoolManager] '{original.name}' 풀 생성 완료");
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[PoolManager] 풀 생성 중 오류 발생: {e.Message}\n{e.StackTrace}");
		}
	}
}