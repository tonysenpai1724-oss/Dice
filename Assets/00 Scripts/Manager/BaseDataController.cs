using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// overide Instance
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IController<T> where T : IController<T>, new()
{
    public static T Instance { get; } = new T();
    public IEnumerator IEInit();
    public IEnumerator IEClearData();

}
public abstract class BaseDataController
{
    public abstract string KeyData();
    public abstract string KeyEvent();

    protected bool waitSaveData;
    protected bool firstTimeInited;
    protected bool firstTicked;
    public abstract IEnumerator IEInit();
    public abstract IEnumerator IEClearData();
    public virtual IEnumerator IEFetchData()
    {
        yield return null;
    }
    public virtual IEnumerator IEFetchConfigs()
    {
        yield return null;
    }
    protected virtual void OnInitSuccess()
    {
        if (!firstTimeInited)
        {
            firstTimeInited = true;
            FirstTimeInit();
        }
        SaveData();
    }

    protected virtual void FirstTimeInit()
    {
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_UPDATE, OnUpdate);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_NEW_DAY, OnNextDay);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_NEW_WEEK, OnNextWeek);
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_NEW_MONTH, OnNextMonth);
    }

    protected virtual void OnFirstTick()
    {
        firstTicked = true;
    }

    protected virtual void OnTick()
    {
        if (!firstTicked)
            OnFirstTick();
    }
    protected virtual void OnNextDay()
    {
    }
    protected virtual void OnNextWeek()
    {
    }
    protected virtual void OnNextMonth()
    {
    }
    protected virtual void OnUpdate()
    {
        if (waitSaveData)
        {
            SaveData();
            waitSaveData = false;
        }
    }

    protected abstract void SaveData();

    public static D LoadLocalCachedData<D>(string keyData) where D : class, IControllerCachedData, new()
    {
        D loadedData;
        string data = GameManager.Instance.GetLocalData(keyData);
        if (string.IsNullOrEmpty(data))
        {
            loadedData = new D();
            loadedData.OnNewData();
        }
        else
        {
            loadedData = Newtonsoft.Json.JsonConvert.DeserializeObject<D>(data);
            if (loadedData == null)
            {
                loadedData = new D();
                loadedData.OnNewData();
            }
        }
        loadedData.InitFirsTime();
        return loadedData;
    }

    public static void SaveLocalCachedData<D>(string keyData, D cachedData) where D : class, IControllerCachedData, new()
    {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(cachedData);
        GameManager.Instance.SaveLocalData(keyData, json);
    }

    public static void ClearLocalCachedData(string keyData)
    {
        GameManager.Instance.SaveLocalData(keyData, string.Empty);
    }

    public virtual void OnValueChange()
    {
        waitSaveData = true;
        DebugCustom.LogColor($"keyEvent: {KeyEvent()}");
        TigerForge.EventManager.EmitEvent(KeyEvent());
    }

    public virtual void OnValueChangeSaveImmediately()
    {
        SaveData();
        waitSaveData = false;
        DebugCustom.LogColor($"keyEvent: {KeyEvent()}");
        TigerForge.EventManager.EmitEvent(KeyEvent());
    }
}
public abstract class BaseLocalController<D> : BaseDataController
    where D : class, IControllerCachedData, new()
{
    protected D cachedData;

    public override IEnumerator IEInit()
    {
        LoadingPanel.Instance.ShowTextLoading($"Loading {KeyData().Replace("_", " ")}");
        Debug.Log($"Loading {KeyData().Replace("_", " ")}");
        yield return GameManager.Instance.StartCoroutine(IEFetchData());
        yield return GameManager.Instance.StartCoroutine(IEFetchConfigs());
        cachedData = LoadLocalCachedData<D>(KeyData());
        yield return null;
        OnInitSuccess();
    }
    public override IEnumerator IEClearData()
    {
        ClearLocalCachedData(KeyData());
        yield return null;
    }
    protected override void SaveData()
    {
        SaveLocalCachedData(KeyData(), cachedData);
    }
}
public abstract class BaseServerController : BaseDataController
{
    protected override void OnUpdate()
    {

    }
    protected override void OnTick()
    {
        base.OnTick();
        if (waitSaveData)
        {
            SaveData();
            waitSaveData = false;
        }
    }
}
public abstract class CommonServerController<D> : BaseServerController
    where D : class, IControllerCachedData, new()
{
    protected D cachedData;
    public override IEnumerator IEInit()
    {
        LoadingPanel.Instance.ShowTextLoading($"Loading {KeyData().Replace("_", " ")}");
        Debug.Log($"Loading {KeyData().Replace("_", " ")}");
        yield return GameManager.Instance.StartCoroutine(IEFetchData());
        yield return GameManager.Instance.StartCoroutine(IEFetchConfigs());
        cachedData = null;
        int count = 5;
        void TryGetData()
        {
            HTTPManager.Instance.GetCommonData(KeyData(), s =>
            {
                s = Helper.DecompressFromBase64GzipData(s);
                if (string.IsNullOrEmpty(s))
                {
                    cachedData = new D();
                    cachedData.OnNewData();
                }
                else
                    cachedData = Newtonsoft.Json.JsonConvert.DeserializeObject<D>(s);
            }, e =>
            {
                count--;
                if (count > 0)
                    TryGetData();

            });
        }
        TryGetData();
        yield return new WaitUntil(() => cachedData != null);
        cachedData.InitFirsTime();
        OnInitSuccess();
    }
    public override IEnumerator IEClearData()
    {
        yield return GameManager.Instance.StartCoroutine(HTTPManager.Instance.SetCommonData(KeyData(), ""));
    }
    protected override void SaveData()
    {
        GameManager.Instance.StartCoroutine(HTTPManager.Instance.SetCommonData(KeyData(), Helper.CompressToBase64GzipData(Newtonsoft.Json.JsonConvert.SerializeObject(cachedData))));
    }

}
public interface IControllerCachedData
{
    public void InitFirsTime();
    public void OnNewData();
}
