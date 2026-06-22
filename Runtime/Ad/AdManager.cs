/****************************************************
 * FileName:		AdManager
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告管理器，统一管理所有平台的广告加载/展示/销毁
 *
*****************************************************/

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MGKit
{
    public class AdManager : MonoBehaviour
    {
        private static AdManager _instance;

        public static AdManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[AdManager]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AdManager>();
                }
                return _instance;
            }
        }

        public MiniGamePlatform CurrentPlatform { get; private set; }
        public bool IsInitialized { get; private set; }
        public AdConfig Config { get; private set; }

        public IPlatformSDK PlatformSDK { get; private set; }
        private readonly Dictionary<string, IAdUnit> _adUnits = new Dictionary<string, IAdUnit>();
        private bool _isDestroyed;

        public event Action<MiniGamePlatform> OnPlatformChanged;

        public void Initialize(AdConfig config = null)
        {
            if (IsInitialized) return;

            Config = config ?? new AdConfig();
            CurrentPlatform = Config.CurrentPlatform != default ? Config.CurrentPlatform : AdPlatformDetector.Detect();

            PlatformSDK = PlatformSDKFactory.Create(CurrentPlatform);
            ((IMiniGamePlatform)PlatformSDK).Initialize();

            IsInitialized = true;
            OnPlatformChanged?.Invoke(CurrentPlatform);

            Debug.Log($"[AdManager] 初始化完成 | 平台: {AdPlatformDetector.GetPlatformName(CurrentPlatform)} | 广告开关: {Config.EnableAd}");
        }

        public void Initialize(MiniGamePlatform targetPlatform, AdConfig config = null)
        {
            if (IsInitialized) return;

            Config = config ?? new AdConfig();
            CurrentPlatform = targetPlatform;

            PlatformSDK = PlatformSDKFactory.Create(CurrentPlatform);
            ((IMiniGamePlatform)PlatformSDK).Initialize();

            IsInitialized = true;
            OnPlatformChanged?.Invoke(CurrentPlatform);

            Debug.Log($"[AdManager] 初始化完成 | 平台: {AdPlatformDetector.GetPlatformName(CurrentPlatform)}");
        }

        public IAdUnit LoadAd(AdType type, string adUnitId = null)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[AdManager] 尚未初始化，请先调用Initialize");
                return null;
            }

            if (_isDestroyed)
            {
                Debug.LogWarning("[AdManager] 实例已销毁");
                return null;
            }

            if (!Config.EnableAd)
            {
                Debug.Log("[AdManager] 广告已全局禁用");
                return null;
            }

            if (!PlatformSDK.IsAdSupported(type))
            {
                Debug.LogWarning($"[AdManager] 当前平台 {CurrentPlatform} 不支持 {type} 广告");
                return null;
            }

            if (string.IsNullOrEmpty(adUnitId))
            {
                adUnitId = Config.GetAdUnitId(type, CurrentPlatform);
            }

            if (string.IsNullOrEmpty(adUnitId))
            {
                Debug.LogError($"[AdManager] 广告位ID为空: type={type}, platform={CurrentPlatform}");
                return null;
            }

            string cacheKey = GetCacheKey(type, adUnitId);

            if (_adUnits.TryGetValue(cacheKey, out var existingAd))
            {
                if (existingAd.State == AdState.Loaded || existingAd.State == AdState.Showing)
                {
                    return existingAd;
                }
                if (existingAd.State == AdState.Closed)
                {
                    existingAd.Load();
                    return existingAd;
                }
                if (existingAd.State == AdState.Loading)
                {
                    return existingAd;
                }
                existingAd.Dispose();
                _adUnits.Remove(cacheKey);
            }

            var adUnit = PlatformSDK.CreateAd(type, adUnitId);
            if (adUnit == null)
            {
                Debug.LogError($"[AdManager] 创建广告失败: type={type}, adUnitId={adUnitId}");
                return null;
            }

            adUnit.OnError += OnAdError;
            adUnit.OnClosed += OnAdClosed;

            _adUnits[cacheKey] = adUnit;
            adUnit.Load();

            Debug.Log($"[AdManager] 加载广告: type={type}, adUnitId={adUnitId}");
            return adUnit;
        }

        public void ShowAd(AdType type, string adUnitId = null)
        {
            if (_isDestroyed) return;
            var ad = LoadAd(type, adUnitId);
            if (ad == null)
            {
                Debug.LogWarning($"[AdManager] 无法展示广告: type={type}, platform={CurrentPlatform}");
                return;
            }

            if (ad.State == AdState.Loaded)
            {
                ad.Show();
                return;
            }

            void OnLoadedHandler(IAdUnit unit)
            {
                if (unit != ad) return;
                ad.OnLoaded -= OnLoadedHandler;
                if (ad.State == AdState.Loaded)
                {
                    ad.Show();
                }
            }

            ad.OnLoaded += OnLoadedHandler;
        }

        public void HideAd(AdType type, string adUnitId = null)
        {
            if (_isDestroyed) return;

            if (string.IsNullOrEmpty(adUnitId))
            {
                adUnitId = Config.GetAdUnitId(type, CurrentPlatform);
            }

            string cacheKey = GetCacheKey(type, adUnitId);
            if (_adUnits.TryGetValue(cacheKey, out var ad))
            {
                ad.Hide();
            }
        }

        public void ShowRewardedVideo(string adUnitId, Action<bool> onRewardResult, Action onAdDisplayed = null, CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[AdManager] 尚未初始化，请先调用 Initialize");
                onRewardResult?.Invoke(false);
                return;
            }

            if (_isDestroyed)
            {
                Debug.LogWarning("[AdManager] 实例已销毁");
                onRewardResult?.Invoke(false);
                return;
            }

            if (!Config.EnableAd)
            {
                onRewardResult?.Invoke(true);
                return;
            }

            var ad = LoadAd(AdType.RewardedVideo, adUnitId);
            if (ad is not IRewardedVideoAdUnit rewardedAd)
            {
                Debug.LogWarning($"[AdManager] 激励视频不可用: platform={CurrentPlatform}, adUnitId={adUnitId ?? Config.GetAdUnitId(AdType.RewardedVideo, CurrentPlatform)}");
                onRewardResult?.Invoke(false);
                return;
            }

            var finished = false;
            var rewardedCallbackArrived = false;

            void Finish(bool rewarded)
            {
                if (finished) return;
                finished = true;
                rewardedAd.OnRewarded -= OnRewardedHandler;
                rewardedAd.OnClosed -= OnClosedHandler;
                rewardedAd.OnLoaded -= OnLoadedHandler;
                rewardedAd.OnError -= OnErrorHandler;
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log($"[AdManager] 激励视频结果已忽略（请求已取消）: adUnitId={adUnitId}, rewarded={rewarded}");
                    return;
                }

                onRewardResult?.Invoke(rewarded);
            }

            void OnRewardedHandler(IRewardedVideoAdUnit unit, bool isEnded)
            {
                rewardedCallbackArrived = true;
                Debug.Log($"[AdManager] 激励视频关闭: adUnitId={adUnitId}, isEnded={isEnded}");
                Finish(isEnded);
            }

            void OnClosedHandler(IAdUnit unit)
            {
                if (unit != rewardedAd || rewardedCallbackArrived) return;
                Debug.LogWarning($"[AdManager] 激励视频仅收到关闭事件，按未完整观看处理: adUnitId={adUnitId}");
                Finish(false);
            }

            void OnErrorHandler(IAdUnit unit, string error)
            {
                if (unit != rewardedAd) return;
                Debug.LogWarning($"[AdManager] 激励视频错误: adUnitId={adUnitId}, error={error}");
                Finish(false);
            }

            void OnLoadedHandler(IAdUnit unit)
            {
                if (unit != rewardedAd) return;
                rewardedAd.OnLoaded -= OnLoadedHandler;
                if (rewardedAd.State == AdState.Loaded)
                {
                    rewardedAd.Show(onAdDisplayed);
                }
            }

            rewardedAd.OnRewarded += OnRewardedHandler;
            rewardedAd.OnClosed += OnClosedHandler;
            rewardedAd.OnError += OnErrorHandler;

            if (rewardedAd.State == AdState.Loaded)
            {
                rewardedAd.Show(onAdDisplayed);
            }
            else
            {
                rewardedAd.OnLoaded += OnLoadedHandler;
            }
        }

        public T GetAdUnit<T>(AdType type, string adUnitId = null) where T : class, IAdUnit
        {
            if (string.IsNullOrEmpty(adUnitId))
            {
                adUnitId = Config.GetAdUnitId(type, CurrentPlatform);
            }

            string cacheKey = GetCacheKey(type, adUnitId);
            if (_adUnits.TryGetValue(cacheKey, out var ad))
            {
                return ad as T;
            }
            return null;
        }

        public void SetBannerPosition(int left, int top, string adUnitId = null)
        {
            var banner = GetAdUnit<IBannerAdUnit>(AdType.Banner, adUnitId);
            banner?.SetPosition(left, top);
        }

        public void SetBannerSize(int width, int height, string adUnitId = null)
        {
            var banner = GetAdUnit<IBannerAdUnit>(AdType.Banner, adUnitId);
            banner?.SetSize(width, height);
        }

        public bool IsAdLoaded(AdType type, string adUnitId = null)
        {
            if (string.IsNullOrEmpty(adUnitId))
            {
                adUnitId = Config.GetAdUnitId(type, CurrentPlatform);
            }

            string cacheKey = GetCacheKey(type, adUnitId);
            return _adUnits.TryGetValue(cacheKey, out var ad) && ad.State == AdState.Loaded;
        }

        public void SetEnableAd(bool enable)
        {
            Config.EnableAd = enable;
            Debug.Log($"[AdManager] 广告开关: {(enable ? "开启" : "关闭")}");
        }

        public void PreloadAll()
        {
            if (!IsInitialized || !Config.EnableAd) return;

            foreach (AdType type in Enum.GetValues(typeof(AdType)))
            {
                if (type == AdType.Custom) continue;

                var adUnitId = Config.GetAdUnitId(type, CurrentPlatform);
                if (!string.IsNullOrEmpty(adUnitId) && PlatformSDK.IsAdSupported(type))
                {
                    LoadAd(type, adUnitId);
                }
            }
        }

        private void OnAdError(IAdUnit adUnit, string error)
        {
            if (_isDestroyed) return;
            Debug.LogError($"[AdManager] 广告错误: type={adUnit.Type}, id={adUnit.AdUnitId}, error={error}");
        }

        private void OnAdClosed(IAdUnit adUnit)
        {
            if (_isDestroyed) return;
            Debug.Log($"[AdManager] 广告关闭: type={adUnit.Type}, id={adUnit.AdUnitId}");
        }

        private static string GetCacheKey(AdType type, string adUnitId)
        {
            return $"{type}_{adUnitId}";
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            foreach (var kvp in _adUnits)
            {
                kvp.Value.Dispose();
            }
            _adUnits.Clear();

            PlatformSDK?.Dispose();
            IsInitialized = false;

            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}