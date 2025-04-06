using System;
using UnityEngine;


    [CreateAssetMenu(fileName = "SnakeStaticData", menuName = "GameData/SnakeStaticData", order = 0)]
    public class SnakeStaticData : ScriptableObject
    {
        public SnakeData Data;
        public SnakeSkins Skins;
    }


    [Serializable]
    public class SnakeData
    {
        [Min(0)] public float MovementSpeed;
        [Min(0)] public float RotationSpeed;
    }

    [Serializable]
    public class SnakeSkins
    {
        public Material[] Materials;
    }