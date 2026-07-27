using System.Text.RegularExpressions;
using MGKit.Editor;
using NUnit.Framework;

public class ManifestPackageSwitcherTests
{
    const string Sample = @"{
  ""dependencies"": {
    ""com.unity.ugui"": ""1.0.0"",
    ""com.qq.weixin.minigame"": ""https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk.git"",
    ""com.unity.modules.ai"": ""1.0.0""
  }
}";

    [Test]
    public void HasPackageInText_FindsWeChat()
    {
        Assert.IsTrue(ManifestPackageSwitcher.HasPackageInText(Sample, "com.qq.weixin.minigame"));
        Assert.IsFalse(ManifestPackageSwitcher.HasPackageInText(Sample, "com.missing"));
    }

    [Test]
    public void RemoveDependency_RemovesWeChatAndKeepsNeighbors()
    {
        var result = ManifestPackageSwitcher.RemoveDependency(Sample, "com.qq.weixin.minigame");
        Assert.IsFalse(ManifestPackageSwitcher.HasPackageInText(result, "com.qq.weixin.minigame"));
        Assert.IsTrue(result.Contains("com.unity.ugui"));
        Assert.IsTrue(result.Contains("com.unity.modules.ai"));
        Assert.IsFalse(Regex.IsMatch(result, ",\\s*}"));
    }

    [Test]
    public void InsertDependency_WorksOnCleanManifest()
    {
        const string clean = @"{
  ""dependencies"": {
    ""com.unity.ugui"": ""1.0.0""
  }
}";
        var result = ManifestPackageSwitcher.InsertDependency(clean, "com.qq.weixin.minigame",
            "https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk.git");
        Assert.IsTrue(ManifestPackageSwitcher.HasPackageInText(result, "com.qq.weixin.minigame"));
    }

    [Test]
    public void RemoveDependency_FirstAndLastItems()
    {
        const string first = @"{
  ""dependencies"": {
    ""com.qq.weixin.minigame"": ""https://x"",
    ""com.unity.ugui"": ""1.0.0""
  }
}";
        var r1 = ManifestPackageSwitcher.RemoveDependency(first, "com.qq.weixin.minigame");
        Assert.IsFalse(ManifestPackageSwitcher.HasPackageInText(r1, "com.qq.weixin.minigame"));
        Assert.IsFalse(Regex.IsMatch(r1, ",\\s*}"));

        const string last = @"{
  ""dependencies"": {
    ""com.unity.ugui"": ""1.0.0"",
    ""com.qq.weixin.minigame"": ""https://x""
  }
}";
        var r2 = ManifestPackageSwitcher.RemoveDependency(last, "com.qq.weixin.minigame");
        Assert.IsFalse(ManifestPackageSwitcher.HasPackageInText(r2, "com.qq.weixin.minigame"));
        Assert.IsFalse(Regex.IsMatch(r2, ",\\s*}"));
    }

    [Test]
    public void InsertAndRemove_DouyinBgdt_Symmetric()
    {
        const string clean = @"{
  ""dependencies"": {
    ""com.unity.ugui"": ""1.0.0""
  }
}";
        const string url = "https://github.com/StartNight/com.bytedance.bgdt.git#v3.0.271";
        var inserted = ManifestPackageSwitcher.InsertDependency(clean, "com.bytedance.bgdt", url);
        Assert.IsTrue(ManifestPackageSwitcher.HasPackageInText(inserted, "com.bytedance.bgdt"));
        Assert.IsTrue(inserted.Contains(url));

        var removed = ManifestPackageSwitcher.RemoveDependency(inserted, "com.bytedance.bgdt");
        Assert.IsFalse(ManifestPackageSwitcher.HasPackageInText(removed, "com.bytedance.bgdt"));
        Assert.IsTrue(removed.Contains("com.unity.ugui"));
        Assert.IsFalse(Regex.IsMatch(removed, ",\\s*}"));
    }
}
