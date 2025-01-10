using Ironcow;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public enum eUIPosition
{
    UI, // 기본 UI
    Popup, // 팝업
    Navigator, // 네비게이터
    Top, // 상단 UI
}

public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField] private List<Transform> parents;
    [SerializeField] private Transform worldParent;

    private List<UIBase> uiList = new List<UIBase>(); // 활성화 UI 리스트

    public static void SetWorldCanvas(Transform worldCanvas)
    {
        instance.worldParent = worldCanvas;
    }

    public static void SetParents(List<Transform> parents)
    {
        instance.parents = parents;
        instance.uiList.Clear();
    }

    /// <summary>
    /// UI 표시
    /// </summary>
    public static async Task<T> Show<T>(params object[] param) where T : UIBase
    {
        instance.uiList.RemoveAll(obj => obj == null); // null인 UI 제거

        // 이미 표시된 UI 찾기
        var ui = instance.uiList.Find(obj => obj.name == typeof(T).ToString());
        if (ui == null)
        {
#if USE_COROUTINE
            // 리소스에서 UI 프리팹 로드
            T prefab = null;
            ResourceManager.instance.LoadAsset<T>(typeof(T).ToString(), eAddressableType.UI, obj =>
            {
                prefab = obj;
            });
            await UniTask.WaitUntil(() => prefab != null);
#elif USE_ASYNC
            var prefab = await ResourceManager.instance.LoadAsset<T>(typeof(T).ToString(), eAddressableType.UI);
#else
            var prefab = await ResourceManager.instance.LoadAsset<T>(typeof(T).ToString(), eAddressableType.UI);
#endif
            // UI 인스턴스 생성
            ui = Instantiate(prefab, instance.parents[(int)prefab.uiPosition]);
            ui.name = ui.name.Replace("(Clone)", "");

            // 기본 UI인 경우 다른 기본 UI 비활성화
            if (ui.uiPosition == eUIPosition.UI)
            {
                instance.uiList.ForEach(obj =>
                {
                    if (obj.uiPosition == eUIPosition.UI) obj.gameObject.SetActive(false);
                });
            }
            instance.uiList.Add(ui);
        }

        // UI 활성화 및 이벤트 호출
        ui.SetActive(ui.uiOptions.isActiveOnLoad);
        ui.opened?.Invoke(param);
        ui.uiOptions.isActiveOnLoad = true;
        return (T)ui;
    }

    /// <summary>
    /// UI 숨기기
    /// </summary>
    public static void Hide<T>(params object[] param) where T : UIBase
    {
        // UI 찾기
        var ui = instance.uiList.Find(obj => obj.name == typeof(T).ToString());
        if (ui != null)
        {
            instance.uiList.Remove(ui);
            // 기본 UI 숨길 경우 이전 UI 활성화
            if (ui.uiPosition == eUIPosition.UI)
            {
                var prevUI = instance.uiList.FindLast(obj => obj.uiPosition == eUIPosition.UI);
                prevUI.SetActive(true);
            }

            // 종료 이벤트 호출 및 UI 비활성화
            ui.closed?.Invoke(param);
            if (ui.uiOptions.isDestroyOnHide)
            {
                Destroy(ui.gameObject);
            }
            else
            {
                ui.SetActive(false);
            }
        }
    }


    public static T Get<T>() where T : UIBase
    {
        return (T)instance.uiList.Find(obj => obj.name == typeof(T).ToString());
    }

    public static bool IsOpened<T>() where T : UIBase
    {
        var ui = instance.uiList.Find(obj => obj.name == typeof(T).ToString());
        return ui != null && ui.gameObject.activeInHierarchy;
    }

    public static void ShowIndicator()
    {

    }

    public static void HideIndicator()
    {

    }
    public static async void ShowAlert(string desc, string title = "", string okBtn = "OK", string cancelBtn = "Cancel", UnityAction okCallback = null, UnityAction cancelCallback = null)
    {
#if USE_ASYNC || USE_COROUTINE
        await 
#endif
        Show<PopupAlert>(desc, title, okBtn, cancelBtn, okCallback, cancelCallback);
    }

    public static async void ShowAlert<T>(string desc, string title = "", string okBtn = "OK", string cancelBtn = "Cancel", UnityAction okCallback = null, UnityAction cancelCallback = null, T image = default)
    {
#if USE_ASYNC || USE_COROUTINE
        await 
#endif
        Show<PopupAlert>(desc, title, okBtn, cancelBtn, okCallback, cancelCallback, image);
    }

    public static async void ShowInputAlert(string desc, string title = "", UnityAction<string> okCallback = null, UnityAction cancelCallback = null, string okBtn = "", string cancelBtn = "")
    {

#if USE_ASYNC || USE_COROUTINE
        await 
#endif
        Show<PopupAlert>(desc, title, okBtn, cancelBtn, okCallback, cancelCallback);
    }



    public static void HideAlert()
    {
        Hide<PopupAlert>();
    }


    int count = 0;
    void Update()
    {
        // ESC 5번 반복 시 소켓 연결 해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            count++;
        }
        if (count > 5)
        {
            count = 0;
            SocketManager.instance.Disconnect(false);
        }
    }
}
