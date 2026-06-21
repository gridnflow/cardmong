using System.Collections.Generic;
using UnityEngine;

namespace Cardmong.Util
{
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool = new();

        public ObjectPool(T prefab, Transform parent, int initialSize = 10)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
                CreateNew();
        }

        public T Get()
        {
            if (_pool.Count == 0) CreateNew();
            var obj = _pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }

        private void CreateNew()
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}
