using System;

public interface IPlatformSDK : IMiniGamePlatform, IAdAdapter
{
    new void Initialize();
}
