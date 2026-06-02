using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SlotCarRacingAR.Runtime.Infrastructure
{
    /// <summary>
    /// Writes lightweight evaluation telemetry for class testing.
    /// Events are mirrored to adb logcat with the [EVAL] prefix and appended to a CSV file.
    /// </summary>
    public static class EvaluationLog
    {
        private const string Prefix = "[EVAL]";
        private const string CleanExitKey = "Face2Race.Evaluation.CleanExit";
        private const string CrashAggregateKey = "Face2Race.Evaluation.UncleanExitCount";
        private const string RaceAttemptKey = "Face2Race.Evaluation.RaceAttempts";
        private const string RaceCompletedKey = "Face2Race.Evaluation.RacesCompletedByAll";

        private static bool _initialized;
        private static string _sessionId;
        private static string _csvPath;
        private static float _setupStartedAt = -1f;
        private static float _joinStartedAt = -1f;
        private static int _highestFlowStep;
        private static int _runtimeExceptionCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _sessionId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string directory = Path.Combine(Application.persistentDataPath, "Face2RaceEvaluation");
            Directory.CreateDirectory(directory);
            _csvPath = Path.Combine(directory, "evaluation_events_" + DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".csv");
            EnsureCsvHeader();

            if (PlayerPrefs.GetInt(CleanExitKey, 1) == 0)
            {
                int aggregateCrashes = PlayerPrefs.GetInt(CrashAggregateKey, 0) + 1;
                PlayerPrefs.SetInt(CrashAggregateKey, aggregateCrashes);
                Record("crashes_del_sistema", "previous_session_unclean_exit", 3, 1f, "La sesion anterior termino sin cierre limpio. aggregate_unclean_exits=" + aggregateCrashes);
            }

            MarkDirty();
            Application.logMessageReceived += HandleLogMessage;

            GameObject runnerObject = new GameObject("EvaluationLogRunner");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            runnerObject.AddComponent<EvaluationLogRunner>();

            Record("session", "start", 0, 0f, "csv=" + _csvPath);
        }

        public static void MarkFlowStep(int step, string label)
        {
            EnsureInitialized();
            if (step <= _highestFlowStep)
            {
                return;
            }

            _highestFlowStep = Mathf.Clamp(step, 1, 5);
            Record("tasa_completitud_flujo_principal", "step_" + _highestFlowStep, _highestFlowStep, _highestFlowStep, label);
        }

        public static void BeginSetup(string context)
        {
            EnsureInitialized();
            _setupStartedAt = Time.realtimeSinceStartup;
            _highestFlowStep = 0;
            Record("tiempo_total_setup", "start", 0, 0f, context);
        }

        public static void CompleteSetupAtRaceStart(int playerCount)
        {
            EnsureInitialized();
            if (_setupStartedAt < 0f)
            {
                Record("tiempo_total_setup", "race_started_without_setup_start", 0, 0f, "players=" + playerCount);
                return;
            }

            float seconds = Mathf.Max(0f, Time.realtimeSinceStartup - _setupStartedAt);
            int score = ScoreSetupSeconds(seconds);
            Record("tiempo_total_setup", "race_started", score, seconds, "players=" + playerCount + " rubric=" + DescribeSetupScore(score));
            _setupStartedAt = -1f;
        }

        public static void BeginJoinTiming(string context)
        {
            EnsureInitialized();
            _joinStartedAt = Time.realtimeSinceStartup;
            Record("tiempo_union_sesion", "start", 0, 0f, context);
        }

        public static void CompleteJoinTiming(string context)
        {
            EnsureInitialized();
            if (_joinStartedAt < 0f)
            {
                return;
            }

            float seconds = Mathf.Max(0f, Time.realtimeSinceStartup - _joinStartedAt);
            int score = ScoreJoinSeconds(seconds);
            Record("tiempo_union_sesion", "connected", score, seconds, context + " rubric=" + DescribeJoinScore(score));
            _joinStartedAt = -1f;
        }

        public static void RecordRaceStarted(int activePlayers)
        {
            EnsureInitialized();
            int attempts = PlayerPrefs.GetInt(RaceAttemptKey, 0) + 1;
            PlayerPrefs.SetInt(RaceAttemptKey, attempts);
            PlayerPrefs.Save();
            Record("carreras_terminadas_por_todos", "race_started", 0, attempts, "active_players=" + activePlayers);
        }

        public static void RecordRaceFinishedByAll(int activePlayers, int finishedPlayers, byte winnerPlayerId, float[] finishTimes)
        {
            EnsureInitialized();
            int attempts = Mathf.Max(1, PlayerPrefs.GetInt(RaceAttemptKey, 0));
            bool completedByAll = activePlayers > 0 && finishedPlayers >= activePlayers;
            int completed = PlayerPrefs.GetInt(RaceCompletedKey, 0);
            if (completedByAll)
            {
                completed++;
                PlayerPrefs.SetInt(RaceCompletedKey, completed);
                PlayerPrefs.Save();
            }

            float percent = attempts > 0 ? completed * 100f / attempts : 0f;
            int score = ScoreRaceCompletionPercent(percent);
            string times = FormatFinishTimes(finishTimes);
            Record(
                "carreras_terminadas_por_todos",
                completedByAll ? "race_finished_by_all" : "race_finished_incomplete",
                score,
                percent,
                "attempts=" + attempts
                    + " completed_by_all=" + completed
                    + " active_players=" + activePlayers
                    + " finished_players=" + finishedPlayers
                    + " winner=P" + winnerPlayerId
                    + " times=" + times);
        }

        private static void RecordCurrentCrashScore(string reason)
        {
            int score = _runtimeExceptionCount == 0 ? 5 : _runtimeExceptionCount == 1 ? 4 : 3;
            Record("crashes_del_sistema", reason, score, _runtimeExceptionCount, "runtime_exceptions=" + _runtimeExceptionCount + " rubric=" + DescribeCrashScore(score));
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }

            _runtimeExceptionCount++;
            Record("crashes_del_sistema", "runtime_exception", 4, _runtimeExceptionCount, condition);
        }

        private static int ScoreSetupSeconds(float seconds)
        {
            if (seconds <= 120f) return 5;
            if (seconds <= 150f) return 4;
            if (seconds <= 180f) return 3;
            if (seconds <= 240f) return 2;
            return 1;
        }

        private static int ScoreJoinSeconds(float seconds)
        {
            if (seconds <= 15f) return 5;
            if (seconds <= 30f) return 4;
            if (seconds <= 45f) return 3;
            if (seconds <= 60f) return 2;
            return 1;
        }

        private static int ScoreRaceCompletionPercent(float percent)
        {
            if (percent >= 90f) return 5;
            if (percent >= 75f) return 4;
            if (percent >= 60f) return 3;
            if (percent >= 40f) return 2;
            return 1;
        }

        private static string DescribeSetupScore(int score)
        {
            switch (score)
            {
                case 5: return "120s_o_menos";
                case 4: return "121s_a_150s";
                case 3: return "151s_a_180s";
                case 2: return "181s_a_240s";
                default: return "mas_de_240s";
            }
        }

        private static string DescribeJoinScore(int score)
        {
            switch (score)
            {
                case 5: return "15s_o_menos";
                case 4: return "16s_a_30s";
                case 3: return "31s_a_45s";
                case 2: return "46s_a_60s";
                default: return "mas_de_60s";
            }
        }

        private static string DescribeCrashScore(int score)
        {
            switch (score)
            {
                case 5: return "0_crasheos";
                case 4: return "1_fallo_menor_sin_cierre";
                case 3: return "1_crasheo_claro_o_varias_excepciones";
                case 2: return "2_crasheos";
                default: return "3_o_mas_crasheos";
            }
        }

        private static string FormatFinishTimes(float[] finishTimes)
        {
            if (finishTimes == null || finishTimes.Length == 0)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < finishTimes.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append('|');
                }

                builder.Append('P').Append(i + 1).Append('=');
                builder.Append(finishTimes[i].ToString("0.00", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static void Record(string metric, string eventName, int score, float value, string details)
        {
            EnsureInitialized();

            string valueText = value.ToString("0.###", CultureInfo.InvariantCulture);
            string line = Csv(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))
                + "," + Csv(_sessionId)
                + "," + Csv(metric)
                + "," + Csv(eventName)
                + "," + score.ToString(CultureInfo.InvariantCulture)
                + "," + Csv(valueText)
                + "," + Csv(details ?? string.Empty);

            try
            {
                File.AppendAllText(_csvPath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(Prefix + " Could not write CSV: " + ex.Message);
            }

            UnityEngine.Debug.Log(Prefix + " metric=" + metric + " event=" + eventName + " score=" + score + " value=" + valueText + " details=" + details);
        }

        private static void EnsureCsvHeader()
        {
            if (File.Exists(_csvPath))
            {
                return;
            }

            File.WriteAllText(_csvPath, "timestamp_utc,session_id,metric,event,score,value,details" + Environment.NewLine);
        }

        private static string Csv(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static void MarkDirty()
        {
            PlayerPrefs.SetInt(CleanExitKey, 0);
            PlayerPrefs.Save();
        }

        private static void MarkClean()
        {
            PlayerPrefs.SetInt(CleanExitKey, 1);
            PlayerPrefs.Save();
        }

        private sealed class EvaluationLogRunner : MonoBehaviour
        {
            private void OnApplicationPause(bool pauseStatus)
            {
                if (pauseStatus)
                {
                    RecordCurrentCrashScore("application_paused_cleanly");
                    MarkClean();
                }
                else
                {
                    MarkDirty();
                    Record("session", "application_resumed", 0, 0f, string.Empty);
                }
            }

            private void OnApplicationQuit()
            {
                RecordCurrentCrashScore("application_quit_cleanly");
                MarkClean();
                Application.logMessageReceived -= HandleLogMessage;
            }
        }
    }
}
