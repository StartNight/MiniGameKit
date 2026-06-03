using UnityEngine;

namespace MGKit.Analytics
{
    public class AnalyticsSession
    {
        public int CurrentLevel { get; private set; } = 1;
        public int Attempt { get; private set; } = 1;
        public int ShootCount { get; private set; }
        public float LastForceRatio { get; private set; }

        private float _levelStartRealtime = -1f;
        private float _gameplayStartRealtime = -1f;
        private float _sessionStartRealtime = -1f;

        public void BeginSession()
        {
            if (_sessionStartRealtime < 0f)
                _sessionStartRealtime = Time.realtimeSinceStartup;
        }

        public float GetSessionDurationSec(float nowRealtime)
        {
            if (_sessionStartRealtime < 0f) return 0f;
            return Mathf.Max(0f, nowRealtime - _sessionStartRealtime);
        }

        public void BeginLevel(int level)
        {
            CurrentLevel = level;
            _levelStartRealtime = Time.realtimeSinceStartup;
            ShootCount = 0;
        }

        public void ResetAttemptForNewLevel() => Attempt = 1;

        public void IncrementAttempt() => Attempt++;

        public int GetLevelDurationSec(float nowRealtime)
        {
            if (_levelStartRealtime < 0f) return 0;
            return Mathf.Max(0, Mathf.RoundToInt(nowRealtime - _levelStartRealtime));
        }

        public void BeginGameplay() => _gameplayStartRealtime = Time.realtimeSinceStartup;

        public int GetGameplayDurationSec(float nowRealtime)
        {
            if (_gameplayStartRealtime < 0f) return 0;
            return Mathf.Max(0, Mathf.RoundToInt(nowRealtime - _gameplayStartRealtime));
        }

        public void EndGameplay() => _gameplayStartRealtime = -1f;

        public void AddShoot(float forceRatio)
        {
            ShootCount++;
            LastForceRatio = forceRatio;
        }
    }
}
