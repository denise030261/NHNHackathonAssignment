#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NHNHackathon.EditorTools
{
    [InitializeOnLoad]
    public static class WebGLPerformanceProfiler
    {
        private const string RequestedKey = "NHN.WebGLProfile.Requested";
        private const string OriginalQualityKey = "NHN.WebGLProfile.OriginalQuality";
        private const double WarmupSeconds = 5.0;
        private const double CaptureSeconds = 10.0;

        private static readonly List<long> mainThread = new();
        private static readonly List<long> renderThread = new();
        private static readonly List<long> gcAllocated = new();
        private static readonly List<long> drawCalls = new();
        private static readonly List<long> setPassCalls = new();
        private static readonly List<long> triangles = new();

        private static ProfilerRecorder gcRecorder;
        private static readonly FrameTiming[] frameTimings = new FrameTiming[1];

        private static bool initialized;
        private static bool recording;
        private static double warmupEndsAt;
        private static double captureEndsAt;

        static WebGLPerformanceProfiler()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/NHN Hackathon/Performance/Profile Current Level1 %#F9")]
        public static void ProfileCurrentLevel1()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before starting a WebGL profile capture.");
                return;
            }
            if (SceneManager.GetActiveScene().name != "Level1")
            {
                Debug.LogWarning("Open Level1 before starting a WebGL profile capture.");
                return;
            }

            SessionState.SetBool(RequestedKey, true);
            UseWebGLQualityLevel();
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RequestedKey, false)
                || !EditorApplication.isPlaying)
            {
                return;
            }

            if (!initialized)
            {
                initialized = true;
                warmupEndsAt = EditorApplication.timeSinceStartup + WarmupSeconds;
                Debug.Log($"WEBGL_PROFILE_WARMUP {WarmupSeconds:0}s");
                return;
            }

            if (!recording)
            {
                if (EditorApplication.timeSinceStartup < warmupEndsAt)
                {
                    return;
                }

                StartRecorders();
                recording = true;
                captureEndsAt = EditorApplication.timeSinceStartup + CaptureSeconds;
                Debug.Log($"WEBGL_PROFILE_CAPTURE_STARTED {CaptureSeconds:0}s");
                return;
            }

            CaptureSample();
            if (EditorApplication.timeSinceStartup >= captureEndsAt)
            {
                FinishCapture();
            }
        }

        private static void StartRecorders()
        {
            ClearSamples();
            gcRecorder = Start(
                ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private static ProfilerRecorder Start(
            ProfilerCategory category, string statName)
        {
            return ProfilerRecorder.StartNew(category, statName, 1);
        }

        private static void CaptureSample()
        {
            mainThread.Add((long)Math.Round(
                Time.unscaledDeltaTime * 1000000000.0));
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, frameTimings) > 0)
            {
                double gpuMilliseconds = frameTimings[0].gpuFrameTime;
                if (gpuMilliseconds > 0.0 && gpuMilliseconds < 1000.0)
                {
                    renderThread.Add((long)Math.Round(
                        gpuMilliseconds * 1000000.0));
                }
            }
            AddIfValid(gcRecorder, gcAllocated);
            drawCalls.Add(UnityStats.drawCalls);
            setPassCalls.Add(UnityStats.setPassCalls);
            triangles.Add(UnityStats.triangles);
        }

        private static void AddIfValid(
            ProfilerRecorder recorder, ICollection<long> samples)
        {
            if (recorder.Valid && recorder.Count > 0)
            {
                samples.Add(recorder.LastValue);
            }
        }

        private static void FinishCapture()
        {
            recording = false;
            initialized = false;
            SessionState.SetBool(RequestedKey, false);
            DisposeRecorders();

            WebGLProfileReport report = new()
            {
                capturedAt = DateTime.Now.ToString("O"),
                scene = SceneManager.GetActiveScene().name,
                qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()],
                resolution = $"{Screen.width}x{Screen.height}",
                sampleCount = mainThread.Count,
                mainThreadMs = BuildMetric(mainThread, 0.000001),
                renderThreadMs = BuildMetric(renderThread, 0.000001),
                gcAllocatedBytes = BuildMetric(gcAllocated, 1.0),
                drawCalls = BuildMetric(drawCalls, 1.0),
                setPassCalls = BuildMetric(setPassCalls, 1.0),
                triangles = BuildMetric(triangles, 1.0)
            };

            string logsDirectory = Path.GetFullPath("Logs");
            Directory.CreateDirectory(logsDirectory);
            string path = Path.Combine(
                logsDirectory,
                $"WebGLProfile_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log(
                $"WEBGL_PROFILE_COMPLETE path={path}\n"
                + JsonUtility.ToJson(report, true));
            RestoreQualityLevel();
            EditorApplication.ExitPlaymode();
        }

        private static void UseWebGLQualityLevel()
        {
            int current = QualitySettings.GetQualityLevel();
            SessionState.SetInt(OriginalQualityKey, current);
            int webGLLevel = Array.IndexOf(QualitySettings.names, "High");
            if (webGLLevel >= 0 && webGLLevel != current)
            {
                QualitySettings.SetQualityLevel(webGLLevel, true);
            }
        }

        private static void RestoreQualityLevel()
        {
            int original = SessionState.GetInt(
                OriginalQualityKey, QualitySettings.GetQualityLevel());
            if (original != QualitySettings.GetQualityLevel())
            {
                QualitySettings.SetQualityLevel(original, true);
            }
        }

        private static ProfileMetric BuildMetric(
            IReadOnlyCollection<long> source, double multiplier)
        {
            if (source.Count == 0)
            {
                return new ProfileMetric();
            }

            long[] sorted = source.OrderBy(value => value).ToArray();
            int percentileIndex = Mathf.Clamp(
                Mathf.CeilToInt(sorted.Length * 0.95f) - 1,
                0, sorted.Length - 1);
            return new ProfileMetric
            {
                average = sorted.Average() * multiplier,
                p95 = sorted[percentileIndex] * multiplier,
                maximum = sorted[^1] * multiplier
            };
        }

        private static void ClearSamples()
        {
            mainThread.Clear();
            renderThread.Clear();
            gcAllocated.Clear();
            drawCalls.Clear();
            setPassCalls.Clear();
            triangles.Clear();
        }

        private static void DisposeRecorders()
        {
            gcRecorder.Dispose();
        }

        [Serializable]
        private sealed class WebGLProfileReport
        {
            public string capturedAt;
            public string scene;
            public string qualityLevel;
            public string resolution;
            public int sampleCount;
            public ProfileMetric mainThreadMs;
            public ProfileMetric renderThreadMs;
            public ProfileMetric gcAllocatedBytes;
            public ProfileMetric drawCalls;
            public ProfileMetric setPassCalls;
            public ProfileMetric triangles;
        }

        [Serializable]
        private sealed class ProfileMetric
        {
            public double average;
            public double p95;
            public double maximum;
        }
    }
}
#endif
