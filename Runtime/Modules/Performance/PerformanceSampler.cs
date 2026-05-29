using Unity.Profiling;
using UnityEngine;

namespace UniTerminal.Modules
{
    /// <summary>
    /// Continuously samples FPS and (when available) render statistics via
    /// <see cref="ProfilerRecorder"/>. Cheap: a handful of recorders and a smoothed
    /// frame counter, no per-frame allocations.
    /// </summary>
    public sealed class PerformanceSampler : MonoBehaviour
    {
        public struct Snapshot
        {
            public float Fps;
            public float FrameMs;
            public long TotalAllocatedBytes;
            public long MonoBytes;
            public long ManagedBytes;
            public int GcCount;
            public long DrawCalls;
            public long Batches;
            public long SetPassCalls;
            public long Triangles;
            public long Vertices;
            public bool RenderStatsValid;
        }

        private float _accumTime;
        private int _accumFrames;
        private float _fps;
        private float _frameMs;

        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _vertices;

        private void OnEnable()
        {
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        }

        private void OnDisable()
        {
            _drawCalls.Dispose();
            _setPass.Dispose();
            _batches.Dispose();
            _triangles.Dispose();
            _vertices.Dispose();
        }

        private void Update()
        {
            _accumTime += Time.unscaledDeltaTime;
            _accumFrames++;

            if (_accumTime >= 0.5f)
            {
                _fps = _accumFrames / _accumTime;
                _frameMs = (_accumTime / _accumFrames) * 1000f;
                _accumTime = 0f;
                _accumFrames = 0;
            }
        }

        public Snapshot Get()
        {
            return new Snapshot
            {
                Fps = _fps,
                FrameMs = _frameMs,
                TotalAllocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                MonoBytes = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),
                ManagedBytes = System.GC.GetTotalMemory(false),
                GcCount = System.GC.CollectionCount(0),
                DrawCalls = _drawCalls.Valid ? _drawCalls.LastValue : -1,
                SetPassCalls = _setPass.Valid ? _setPass.LastValue : -1,
                Batches = _batches.Valid ? _batches.LastValue : -1,
                Triangles = _triangles.Valid ? _triangles.LastValue : -1,
                Vertices = _vertices.Valid ? _vertices.LastValue : -1,
                RenderStatsValid = _drawCalls.Valid
            };
        }
    }
}
