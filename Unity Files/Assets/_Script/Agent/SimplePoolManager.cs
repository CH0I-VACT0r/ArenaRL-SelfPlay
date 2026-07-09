using System.Collections.Generic;
using UnityEngine;

public static class SimplePoolManager
{
    // 풀 키(문자열)를 기준으로 비활성화된 오브젝트들을 보관하는 큐(Queue)
    private static Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();

    // 사전 생성
    public static void Prewarm(string key, int count)
    {
        if (!pools.ContainsKey(key)) pools[key] = new Queue<GameObject>();

        GameObject prefab = Resources.Load<GameObject>(key);
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Object.Instantiate(prefab);
            obj.SetActive(false); // 생성 직후 비활성화하여 대기
            pools[key].Enqueue(obj);
        }
    }

    // 오브젝트 요청
    public static GameObject Spawn(string key)
    {
        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
        }

        while (pools[key].Count > 0)
        {
            GameObject obj = pools[key].Dequeue();
            if (obj != null)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        // Resources 폴더에서 프리팹 로드
        GameObject prefab = Resources.Load<GameObject>(key);

        if (prefab != null)
        {
            return Object.Instantiate(prefab);
        }
        else
        {
            // 프리팹을 찾지 못한 경우 오류 출력 후 임시 큐브(원) 생성
            Debug.LogError($"[SimplePoolManager] 'Assets/Resources/{key}' 프리팹을 찾을 수 없습니다! 투명한 객체 생성을 방지하기 위해 임시 스프라이트를 부착합니다.");

            GameObject tempObj = new GameObject(key);
            SpriteRenderer sr = tempObj.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = Color.magenta;

            return tempObj;
        }
    }

    // 사용이 끝난 오브젝트를 풀에 반납
    public static void ReturnToPool(string key, GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);

        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
        }
        pools[key].Enqueue(obj);
    }
}