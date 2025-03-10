using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

public abstract class ManagedCache<K, V> where K : notnull
{
    private readonly ConcurrentDictionary<K, Task<V>> _cache = [];

    public bool TryGetCached(K k, [MaybeNullWhen(false)] out V v)
    {
        if (_cache.TryGetValue(k, out Task<V>? task) && task.IsCompletedSuccessfully)
        {
            v = task.Result;
            return true;
        }
        v = default;
        return false;
    }

    public bool RemoveCached(K k)
    {
        if (_cache.Remove(k, out Task<V>? task))
        {
            task.ContinueWith((task) =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    OnRemoveCached(k, task.Result);
                }
            });
            return true;
        }
        return false;
    }

    protected virtual void OnRemoveCached(K k, V v) { }

    public bool IsLoading(K k)
    {
        if (_cache.TryGetValue(k, out Task<V>? task))
        {
            return !task.IsCompleted;
        }
        return false;
    }

    protected abstract V LoadInternal(K k);

    public Task<V> LoadThreaded(K k)
    {
        return _cache.GetOrAdd(k, (k) =>
        {
            return Task.Run(() =>
            {
                try
                {
                    V v = LoadInternal(k);
                    return v;
                }
                catch (Exception e)
                {
                    Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                    Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                    throw;
                }
            });
        });
    }

    public void Clear()
    {
        _cache.Clear();
        OnCacheClear();
    }

    protected virtual void OnCacheClear() { }
}