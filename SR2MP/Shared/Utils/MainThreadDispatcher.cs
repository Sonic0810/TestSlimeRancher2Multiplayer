using UnityEngine;
using System.Collections.Concurrent;
using System;
using Il2CppInterop.Runtime.Injection;

namespace SR2MP.Shared.Utils;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private static readonly ConcurrentQueue<Action> actionQueue = new();

    static MainThreadDispatcher()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MainThreadDispatcher>();
    }

    public static void Initialize()
    {
        if (instance != null) return;

        var go = new GameObject("SR2MP_Dispatcher");
        instance = go.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(go);

        SrLogger.LogMessage("Main thread dispatcher initialized", SrLogger.LogTarget.Both);
    }

    private void Update()
    {
        while (actionQueue.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error executing main thread action: {ex}", SrLogger.LogTarget.Both);
            }
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        actionQueue.Enqueue(action);
    }

    private void OnDestroy()
    {
        instance = null;
    }
}