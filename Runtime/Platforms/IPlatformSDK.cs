namespace MGKit
{
    public interface IPlatformSDK : IMiniGamePlatform, IAdAdapter
    {
        new void Initialize();
    }
}