using UnityEngine;


    public class SnakeSkin : MonoBehaviour
    {
        [SerializeField] public MeshRenderer[] _elements;
        
        public void ChangeTo(Material targetMaterial)
        {
            foreach (var element in _elements) 
                element.material = targetMaterial;
        }
    }
