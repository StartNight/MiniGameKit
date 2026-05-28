/****************************************************
 * FileName:		EditorAdAdapter
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		Editor平台广告适配器，仅用于编辑器调试
 *
*****************************************************/

using UnityEngine;
using System;
public class EditorAdAdapter : IAdAdapter
{
    public AdPlatform Platform => AdPlatform.Editor;
    public string PlatformName => "Editor(模拟)";
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        IsInitialized = true;
        Debug.Log("[Ad] Editor适配器初始化完成");
    }

    public void Dispose()
    {
        IsInitialized = false;
    }

    public IAdUnit CreateAd(AdType type, string adUnitId)
    {
        return new EditorAdUnit(type, adUnitId);
    }

    public bool IsAdSupported(AdType type)
    {
        return true;
    }

    private class EditorAdUnit : IAdUnit
    {
        public string AdUnitId { get; }
        public AdType Type { get; }
        public AdState State { get; private set; }

        public event Action<IAdUnit> OnLoaded;
        public event Action<IAdUnit, string> OnError;
        public event Action<IAdUnit> OnClosed;
        public event Action<IAdUnit> OnClicked;

        public EditorAdUnit(AdType type, string adUnitId)
        {
            Type = type;
            AdUnitId = adUnitId;
            State = AdState.None;
        }

        public void Load()
        {
            State = AdState.Loading;
            Debug.Log($"[Ad-Editor] 加载 {Type} 广告: {AdUnitId}");
            State = AdState.Loaded;
            OnLoaded?.Invoke(this);
        }

        public void Show()
        {
            if (State != AdState.Loaded)
            {
                Debug.LogWarning($"[Ad-Editor] 广告未加载，无法展示: {Type}");
                return;
            }
            State = AdState.Showing;
            Debug.Log($"[Ad-Editor] 展示 {Type} 广告: {AdUnitId}");
        }

        public void Hide()
        {
            Debug.Log($"[Ad-Editor] 隐藏 {Type} 广告: {AdUnitId}");
        }

        public void Dispose()
        {
            State = AdState.None;
        }
    }
}
