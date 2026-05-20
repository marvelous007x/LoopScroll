using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameObjectPool
{
    public GameObject template;
    private Queue<GameObject> cachedObjs;

    private static Transform s_PoolRoot;

    public GameObject Get()
    {
        GameObject obj;
        if (cachedObjs == null || cachedObjs.Count == 0)
        {
            obj = GameObject.Instantiate(template);
        }
        else
        {
            obj = cachedObjs.Dequeue();
        }
        obj.SetActive(true);
        return obj;
    }

    public void Release(GameObject go)
    {
        cachedObjs ??= new Queue<GameObject>();
        cachedObjs.Enqueue(go);
        if (!s_PoolRoot)
        {
            s_PoolRoot = new GameObject("LoopPool").transform;
            GameObject.DontDestroyOnLoad(s_PoolRoot);
        }
        go.transform.SetParent(s_PoolRoot.transform, false);
        go.SetActive(false);
    }

    public void Clear()
    {
        if (cachedObjs != null)
        {
            while (cachedObjs.Count > 0)
            {
                var obj = cachedObjs.Dequeue();
                GameObject.Destroy(obj);
            }
        }
    }
}