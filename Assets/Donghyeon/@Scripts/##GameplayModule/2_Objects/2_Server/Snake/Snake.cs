using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

    public class Snake : MonoBehaviour
    {
        [field: SerializeField] public SnakeHead Head { get; private set;}
        [field: SerializeField] public SnakeBody Body { get; private set; }

        
        public GameObject RemoveDetail() => 
            Body.RemoveLastDetail();

        public void ResetRotation() => 
            Head.ResetRotation();



        public IEnumerable<Vector3> GetBodyDetailPositions()
        {
            yield return Head.transform.position;
            foreach (var detailPosition in Body.GetBodyDetailPositions())
                yield return detailPosition;
        }
    }
