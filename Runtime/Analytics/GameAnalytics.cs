using System.Collections.Generic;

namespace MGKit.Analytics
{
    public static class GameAnalytics
    {
        public static AnalyticsSession Session { get; } = new AnalyticsSession();

        private static IAnalyticsReporter _reporter;

        static GameAnalytics()
        {
            ResetReporterToDefault();
        }

        public static void Configure(IAnalyticsReporter reporter)
        {
            _reporter = reporter ?? new NullAnalyticsReporter();
        }

        public static void ResetReporterToDefault()
        {
#if UNITY_EDITOR
            _reporter = new EditorAnalyticsReporter();
#elif MGKIT_WECHAT
            _reporter = new WeChatAnalyticsReporter();
#else
            _reporter = new NullAnalyticsReporter();
#endif
        }

        public static void TrackEvent(string eventId, Dictionary<string, string> data)
        {
            _reporter?.ReportEvent(eventId, data);
        }

        public static void TrackLevelStart(int level, string ballType, bool isNewUser)
        {
            Session.BeginLevel(level);
            TrackEvent(AnalyticsEventIds.LevelStart, AnalyticsParamBuilder.Create()
                .Put("level", level)
                .Put("ball_type", ballType)
                .Put("attempt", Session.Attempt)
                .Put("is_new_user", isNewUser)
                .Build());
        }

        public static void TrackLevelComplete(int level, int durationSec, int remainHp, string ballType, int shootCount)
        {
            TrackEvent(AnalyticsEventIds.LevelComplete, AnalyticsParamBuilder.Create()
                .Put("level", level)
                .Put("duration_sec", durationSec)
                .Put("remain_hp", remainHp)
                .Put("ball_type", ballType)
                .Put("shoot_count", shootCount)
                .Build());
        }

        public static void TrackLevelFail(int level, int durationSec, string ballType, int shootCount)
        {
            TrackEvent(AnalyticsEventIds.LevelFail, AnalyticsParamBuilder.Create()
                .Put("level", level)
                .Put("duration_sec", durationSec)
                .Put("ball_type", ballType)
                .Put("shoot_count", shootCount)
                .Build());
        }

        public static void TrackLevelRetry(int level)
        {
            Session.IncrementAttempt();
            TrackEvent(AnalyticsEventIds.LevelRetry, AnalyticsParamBuilder.Create().Put("level", level).Build());
        }

        public static void TrackLevelNext(int level, bool adTriple)
        {
            TrackEvent(AnalyticsEventIds.LevelNext, AnalyticsParamBuilder.Create()
                .Put("level", level)
                .Put("ad_triple", adTriple)
                .Build());
            Session.ResetAttemptForNewLevel();
        }

        public static void TrackShootBall(int level, float forceRatio, string ballType)
        {
            Session.AddShoot(forceRatio);
            TrackEvent(AnalyticsEventIds.ShootBall, AnalyticsParamBuilder.Create()
                .Put("level", level)
                .Put("force_ratio", forceRatio)
                .Put("ball_type", ballType)
                .Build());
        }

        public static void TrackAdRequest(string scene, string adUnitId, int level) =>
            TrackEvent(AnalyticsEventIds.AdRequest, AnalyticsParamBuilder.Create()
                .Put("scene", scene).Put("ad_unit_id", adUnitId).Put("level", level).Build());

        public static void TrackAdComplete(string scene, string adUnitId, int level) =>
            TrackEvent(AnalyticsEventIds.AdComplete, AnalyticsParamBuilder.Create()
                .Put("scene", scene).Put("ad_unit_id", adUnitId).Put("level", level).Build());

        public static void TrackAdSkip(string scene, string adUnitId, string reason, int level) =>
            TrackEvent(AnalyticsEventIds.AdSkip, AnalyticsParamBuilder.Create()
                .Put("scene", scene).Put("ad_unit_id", adUnitId).Put("reason", reason).Put("level", level).Build());

        public static void TrackShareClick(string entry, int level = -1)
        {
            var b = AnalyticsParamBuilder.Create().Put("entry", entry);
            if (level > 0) b.Put("level", level);
            TrackEvent(AnalyticsEventIds.ShareClick, b.Build());
        }

        public static void TrackShareInvoke(string entry, string titleKey)
        {
            TrackEvent(AnalyticsEventIds.ShareInvoke, AnalyticsParamBuilder.Create()
                .Put("entry", entry).Put("title_key", titleKey).Build());
        }

        public static void TrackUIClick(string buttonId, string screen, int level = -1)
        {
            var b = AnalyticsParamBuilder.Create().Put("button_id", buttonId).Put("screen", screen);
            if (level > 0) b.Put("level", level);
            TrackEvent(AnalyticsEventIds.UiClick, b.Build());
        }

        public static void TrackSessionStart(int level, bool isNewUser)
        {
            Session.BeginSession();
            TrackEvent(AnalyticsEventIds.SessionStart, AnalyticsParamBuilder.Create()
                .Put("level", level).Put("is_new_user", isNewUser).Build());
        }

        public static void TrackSessionEnd()
        {
            var sec = Session.GetSessionDurationSec(UnityEngine.Time.realtimeSinceStartup);
            TrackEvent(AnalyticsEventIds.SessionEnd, AnalyticsParamBuilder.Create()
                .Put("play_duration_sec", sec).Build());
        }

        public static void TrackGameplayStart(int level, string entry)
        {
            Session.BeginGameplay();
            TrackEvent(AnalyticsEventIds.GameplayStart, AnalyticsParamBuilder.Create()
                .Put("level", level).Put("entry", entry).Build());
        }

        public static void TrackGameplayEnd(int level, string reason)
        {
            var sec = Session.GetGameplayDurationSec(UnityEngine.Time.realtimeSinceStartup);
            TrackEvent(AnalyticsEventIds.GameplayEnd, AnalyticsParamBuilder.Create()
                .Put("level", level).Put("duration_sec", sec).Put("reason", reason).Build());
            Session.EndGameplay();
        }
    }
}
