using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
        [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 4;
        public int maxSize = 8;
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

void Awake()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

public GameObject SpawnFromPool(string tag, Transform parent, bool worldPositionStays = false)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = null;
        Queue<GameObject> pool = poolDictionary[tag];
        Pool poolConfig = pools.Find(p => p.tag == tag);

        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                objectToSpawn = obj;
                break;
            }
        }

        if (objectToSpawn == null)
        {
            if (pool.Count < poolConfig.maxSize)
            {
                objectToSpawn = Instantiate(poolConfig.prefab);
                objectToSpawn.SetActive(false);
                pool.Enqueue(objectToSpawn);
                Debug.Log($"Pool {tag} expanded: {pool.Count}/{poolConfig.maxSize}");
            }
            else
            {
                Debug.LogWarning($"Pool {tag} reached max size ({poolConfig.maxSize}). Cannot spawn more objects.");
                return null;
            }
        }

        objectToSpawn.transform.SetParent(parent, worldPositionStays);
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
    }
}
