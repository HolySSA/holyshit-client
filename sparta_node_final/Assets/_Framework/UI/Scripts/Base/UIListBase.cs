using Ironcow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 목록형 UI 기본 기능 제공
/// </summary>
public abstract class UIListBase<T> : UIBase, IUIList<T> where T : MonoBehaviour
{
    [SerializeField] protected Transform listParent;
    [SerializeField] protected T itemPrefab;
    protected List<T> items = new List<T>();

    /// <summary>
    /// 리스트 추가
    /// </summary>
    public T AddItem()
    {
        var item = Instantiate(itemPrefab, listParent);
        items.Add(item);
        return item;
    }

    /// <summary>
    /// 리스트 초기화
    /// </summary>
    public void ClearList()
    {
        items.ForEach(obj =>
        {
            Destroy(obj.gameObject);
        });
        items.Clear();
    }

    /// <summary>
    /// 리스트 데이터 설정
    /// </summary>
    public abstract void SetList();
}
