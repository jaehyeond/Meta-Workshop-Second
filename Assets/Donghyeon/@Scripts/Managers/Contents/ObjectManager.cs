using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class ObjectManager
{
	public HashSet<Hero> Heroes { get; } = new HashSet<Hero>();
	public HashSet<Monster> Monsters { get; } = new HashSet<Monster>();
	public HashSet<SnakeController> Snakes { get; } = new HashSet<SnakeController>();
	public HashSet<SnakeBody> SnakeBodies { get; } = new HashSet<SnakeBody>();
	public HashSet<Food> Foods { get; } = new HashSet<Food>();


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
	public Transform SnakeRoot { get { return GetRootTransform("@Snakes"); } }
	public Transform FoodRoot { get { return GetRootTransform("@Foods"); } }

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

		GameObject go = Managers.Resource.Instantiate("Head.prefab");
		// go.name = prefabName;
		go.transform.position = position;

		BaseObject obj = go.GetComponent<BaseObject>();

		if (obj.ObjectType == EObjectType.Snake)
		{
			obj.transform.parent = SnakeRoot;
			SnakeController snake = go.GetComponent<SnakeController>();
			Snakes.Add(snake);
			// snake.SetInfo(templateID);
		}


		// if (obj.ObjectType == EObjectType.Hero)
		// {
		// 	obj.transform.parent = HeroRoot;
		// 	Hero hero = go.GetComponent<Hero>();
		// 	Heroes.Add(hero);
		// 	hero.SetInfo(templateID);
		// }
		// else if (obj.ObjectType == EObjectType.Monster)
		// {
		// 	obj.transform.parent = HeroRoot;
		// 	Monster monster = go.GetComponent<Monster>();
		// 	Monsters.Add(monster);
		// 	monster.SetInfo(templateID);
		// }
		// else if (obj.ObjectType == EObjectType.Snake)
		// {
		// 	obj.transform.parent = SnakeRoot;
		// 	SnakeController snake = go.GetComponent<SnakeController>();
		// 	Snakes.Add(snake);
		// 	snake.SetInfo(templateID);
		// }
		// else if (obj.ObjectType == EObjectType.SnakeBody)
		// {
		// 	obj.transform.parent = SnakeRoot;
		// 	SnakeBody body = go.GetComponent<SnakeBody>();
		// 	SnakeBodies.Add(body);
		// }
		// else if (obj.ObjectType == EObjectType.Food)
		// {
		// 	obj.transform.parent = FoodRoot;
		// 	Food food = go.GetComponent<Food>();
		// 	Foods.Add(food);
		// 	food.SetInfo(templateID);
		// }




 // 박재현 하하하하하하하하하하핳 오브젝트 매니저 추가해줘

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
		else if (obj.ObjectType == EObjectType.Snake)
		{
			SnakeController snake = obj.GetComponent<SnakeController>();
			Snakes.Remove(snake);
		}
		else if (obj.ObjectType == EObjectType.SnakeBody)
		{
			SnakeBody body = obj.GetComponent<SnakeBody>();
			SnakeBodies.Remove(body);
		}
		else if (obj.ObjectType == EObjectType.Food)
		{
			Food food = obj.GetComponent<Food>();
			Foods.Remove(food);
		}


		Managers.Resource.Destroy(obj.gameObject);
	}

}
