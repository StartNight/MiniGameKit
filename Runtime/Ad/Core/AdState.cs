/****************************************************
 * FileName:		AdState
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告状态枚举定义
 *
*****************************************************/

namespace MGKit
{
    public enum AdState
    {
        None = 0,
        Loading = 1,
        Loaded = 2,
        Showing = 3,
        Closed = 4,
        Error = 5
    }
}