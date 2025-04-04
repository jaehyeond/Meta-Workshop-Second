using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class ObjectManager
{
	public HashSet<Hero> Heroes { get; } = new HashSet<Hero>();
	public HashSet<Monster> Monsters { get; } = new HashSet<Monster>();


	#region Roots
	public Transform GetRootTransform(string name)
	{
		GameObject root = GameObject.Find(name);
		if (root == null)
			root = new GameObject { name = name };

		return root.transform;
	}

	public Transform HeroRoot { get { return GetRootTransform("@Heroes"); } }
	public Transform MonsterRoot { get { return GetRootTransform("@Monsters"); } }

	#endregion

	
	public GameObject SpawnGameObject(Vector3 position, string prefabName)
	{
		GameObject go = Managers.Resource.Instantiate(prefabName, pooling: true);
		go.transform.position = position;
		return go;
	}



	public T Spawn<T>(Vector3 position, int templateID) where T : BaseObject
	{
		string prefabName = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate(prefabName);
		go.name = prefabName;
		go.transform.position = position;

		BaseObject obj = go.GetComponent<BaseObject>();

		if (obj.ObjectType == EObjectType.Hero)
		{
			obj.transform.parent = HeroRoot;
			Hero hero = go.GetComponent<Hero>();
			Heroes.Add(hero);
			hero.SetInfo(templateID);
		}
		else if (obj.ObjectType == EObjectType.Monster)
		{
			obj.transform.parent = HeroRoot;
			Monster monster = go.GetComponent<Monster>();
			Monsters.Add(monster);
			monster.SetInfo(templateID);
		}


		return obj as T;
	}

	public void Despawn<T>(T obj) where T : BaseObject
	{
		EObjectType objectType = obj.ObjectType;

		if (obj.ObjectType == EObjectType.Hero)
		{
			Hero hero = obj.GetComponent<Hero>();
			Heroes.Remove(hero);
		}
		else if (obj.ObjectType == EObjectType.Monster)
		{
			Monster monster = obj.GetComponent<Monster>();
			Monsters.Remove(monster);
		}


		Managers.Resource.Destroy(obj.gameObject);
	}

}
