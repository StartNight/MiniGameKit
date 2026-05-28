/****************************************************
 * FileName:		AdAdapterFactory
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告适配器工厂，根据平台创建对应适配器
 *
*****************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

public static class AdAdapterFactory
{
    private static readonly Dictionary<AdPlatform, Func<IAdAdapter>> _creators = new Dictionary<AdPlatform, Func<IAdAdapter>>()
    {
        { AdPlatform.Editor, () => new EditorAdAdapter() },
        { AdPlatform.WeChatMiniGame, () => new WeChatAdAdapter() },
        { AdPlatform.DouyinMiniGame, () => new DouyinAdAdapter() },
        { AdPlatform.Web, () => new WebAdAdapter() },
        { AdPlatform.Android, () => new MobileAdAdapter() },
        { AdPlatform.iOS, () => new MobileAdAdapter() }
    };

    public static IAdAdapter Create(AdPlatform platform)
    {
        if (_creators.TryGetValue(platform, out var creator))
        {
            var adapter = creator();
            Debug.Log($"[Ad] 创建广告适配器: {adapter.PlatformName}");
            return adapter;
        }

        Debug.LogError($"[Ad] 不支持的平台: {platform}，降级为Editor适配器");
        return new EditorAdAdapter();
    }

    public static void RegisterCreator(AdPlatform platform, Func<IAdAdapter> creator)
    {
        _creators[platform] = creator;
    }
}
