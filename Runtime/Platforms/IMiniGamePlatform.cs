using System;
namespace MGKit
{


public interface IMiniGamePlatform
{
    event Action OnShow;
    event Action OnHide;

    void Initialize();
    void Destroy();
    void GetBannerRect(int defaultLeft, int defaultTop, int defaultWidth, int defaultHeight, out int left, out int top, out int width, out int height);
    void ShareApp(string title, string query);
    void OpenCustomerService();
    void OpenBusinessView(string businessType, Action<string> fail, Action<string> success);
    void VibrateShort();
    void VibrateLong();
    void ReportGameStart();
}

}
