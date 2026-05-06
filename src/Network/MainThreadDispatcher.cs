using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace RagebateMobs.Network
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public static void Initialize()
        {
            if (_instance != null) return;
            var go = new GameObject("RagebateMobs.MainThreadDispatcher");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThreadDispatcher>();
        }

        public static void Enqueue(Action action)
        {
            if (action == null) return;
            _queue.Enqueue(action);
        }

        private void Update()
        {
            while (_queue.TryDequeue(out var a))
            {
                try { a(); }
                catch (Exception ex)
                {
                    RagebateMobsPlugin.Logger?.LogError($"[Ragebait] dispatcher error: {ex}");
                }
            }
        }
    }
}
