/****************************************************
 * FileName:		IAdUnit
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告单元基础接口，所有广告类型均实现此接口
 *
*****************************************************/

using System;

namespace MGKit
{
    public interface IAdUnit : IDisposable
    {
        string AdUnitId { get; }
        AdType Type { get; }
        AdState State { get; }

        void Load();

        void Show();

        void Hide();

        event Action<IAdUnit> OnLoaded;

        event Action<IAdUnit, string> OnError;

        event Action<IAdUnit> OnClosed;

        event Action<IAdUnit> OnClicked;
    }

    public interface IBannerAdUnit : IAdUnit
    {
        void SetPosition(int left, int top);

        void SetSize(int width, int height);
    }

    public interface IInterstitialAdUnit : IAdUnit
    { }

    public interface IRewardedVideoAdUnit : IAdUnit
    {
        event Action<IRewardedVideoAdUnit, bool> OnRewarded;
    }

    public interface ICustomAdUnit : IAdUnit
    {
        void SetPosition(int left, int top);

        void SetSize(int width, int height);
    }
}