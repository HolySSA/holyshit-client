using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;

namespace Ironcow
{
    public class PoolManager : MonoSingleton<PoolManager>
    {
        Dictionary<string, Queue<ObjectPoolBase>> pools = new Dictionary<string, Queue<ObjectPoolBase>>();
        public bool isInit = false;

#if USE_ASYNC
        public async void Init()
        {
            foreach (var data in ObjectPoolDataSO.SharedInstance.objectPoolDatas)
            {
                data.prefab = Resources.Load<ObjectPoolBase>(data.rCode);
                if (data.prefab == null)
                {
                    Debug.LogError($"[PoolManager] Failed to load prefab for rCode: {data.rCode}");
                    continue;
                }

                data.parent = new GameObject(data.rCode + "_Pool").transform;
                data.parent.parent = transform;
                // 풀 큐 초기화
                Queue<ObjectPoolBase> queue = new Queue<ObjectPoolBase>();
                pools.Add(data.rCode, queue);
                // 초기 오브젝트 생성
                for (int i = 0; i < data.count; i++)
                {
                    var obj = Instantiate(data.prefab, data.parent);
                    obj.name = data.rCode; 
                    obj.SetActive(false);
                    queue.Enqueue(obj);
                }
            }

            isInit = true;
            //Debug.Log("[PoolManager] All pools initialized successfully");
        }
#elif USE_COROUTINE
        public void Init()
        {
            StartCoroutine(InitCoroutine());
        }

        private IEnumerator InitCoroutine()
        {
            foreach (var data in ObjectPoolDataSO.SharedInstance.objectPoolDatas)
            {
                // Resources에서 프리팹 로드
                data.prefab = Resources.Load<ObjectPoolBase>(data.rCode);
                if (data.prefab == null)
                {
                    Debug.LogError($"[PoolManager] Failed to load prefab for rCode: {data.rCode}");
                    continue;
                }

                // 부모 오브젝트 생성
                data.parent = new GameObject(data.rCode + "_Pool").transform;
                data.parent.parent = transform;

                // 풀 큐 초기화
                Queue<ObjectPoolBase> queue = new Queue<ObjectPoolBase>();
                pools.Add(data.rCode, queue);

                // 초기 오브젝트 생성
                for (int i = 0; i < data.count; i++)
                {
                    var obj = Instantiate(data.prefab, data.parent);
                    obj.name = data.rCode;
                    obj.SetActive(false);
                    queue.Enqueue(obj);

                    // 매 10개 생성마다 1프레임 대기
                    if (i % 10 == 0)
                        yield return null;
                }
            }
            isInit = true;
            //Debug.Log("[PoolManager] All pools initialized successfully");
        }
#else
        public void Init()
        {
            foreach (var data in ObjectPoolDataSO.SharedInstance.objectPoolDatas)
            {
                // Resources에서 프리팹 로드
                data.prefab = Resources.Load<ObjectPoolBase>(data.rCode);
                if (data.prefab == null)
                {
                    Debug.LogError($"[PoolManager] Failed to load prefab for rCode: {data.rCode}");
                    continue;
                }

                // 부모 오브젝트 생성
                data.parent = new GameObject(data.rCode + "_Pool").transform;
                data.parent.parent = transform;

                // 풀 큐 초기화
                Queue<ObjectPoolBase> queue = new Queue<ObjectPoolBase>();
                pools.Add(data.rCode, queue);

                // 초기 오브젝트 생성
                for (int i = 0; i < data.count; i++)
                {
                    var obj = Instantiate(data.prefab, data.parent);
                    obj.name = data.rCode;
                    obj.SetActive(false);
                    queue.Enqueue(obj);
                }
            }
            isInit = true;
            //Debug.Log("[PoolManager] All pools initialized successfully");
        }
#endif

        public T Spawn<T>(string rcode, params object[] param) where T : ObjectPoolBase
        {
            if (pools[rcode].Count == 0)
            {
                var data = ObjectPoolDataSO.SharedInstance.objectPoolDatas.Find(obj => obj.rCode == rcode);
                for (int i = 0; i < data.count; i++)
                {
                    var obj = Instantiate(data.prefab, data.parent);
                    obj.name.Replace("(Clone)", "");
                    pools[rcode].Enqueue(obj);
                }
            }
            var retObj = (T)pools[rcode].Dequeue();
            retObj.SetActive(true);
            retObj.Init(param);
            return retObj;
        }

        public T Spawn<T>(string rcode, Transform parent, params object[] param) where T : ObjectPoolBase
        {
            var obj = Spawn<T>(rcode, param);
            obj.transform.parent = parent;
            return obj;
        }

        public T Spawn<T>(string rcode, Vector3 position, Transform parent, params object[] param) where T : ObjectPoolBase
        {
            var obj = Spawn<T>(rcode, parent, param);
            obj.transform.position = position;
            return obj;
        }

        public T Spawn<T>(string rcode, Vector3 position, Quaternion rotation, Transform parent, params object[] param) where T : ObjectPoolBase
        {
            var obj = Spawn<T>(rcode, position, parent, param);
            obj.transform.rotation = rotation;
            return obj;
        }

        public void Release(ObjectPoolBase item)
        {
            item.SetActive(false);
            var data = ObjectPoolDataSO.SharedInstance.objectPoolDatas.Find(obj => obj.rCode == item.name);
            item.transform.parent = data.parent;
            pools[item.name].Enqueue(item);
        }
    }
}