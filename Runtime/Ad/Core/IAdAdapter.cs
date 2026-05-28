/****************************************************
 * FileName:		IAdAdapter
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告平台适配器接口，每个平台实现此接口
 *
*****************************************************/

public interface IAdAdapter
{
    AdPlatform Platform { get; }
    string PlatformName { get; }

    void Initialize();
    void Dispose();

    IAdUnit CreateAd(AdType type, string adUnitId);
    bool IsAdSupported(AdType type);
    bool IsInitialized { get; }
}
