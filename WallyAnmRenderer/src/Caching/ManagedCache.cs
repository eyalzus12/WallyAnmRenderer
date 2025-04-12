using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

public abstract class ManagedCache<K, V> where K : notnull
{
    private readonly record struct QueueItem(Task<V> Task, CancellationTokenSource Source);
    private readonly ConcurrentDictionary<K, QueueItem> _cache = [];

    public bool TryGetCached(K k, [MaybeNullWhen(false)] out V v)
    {
        if (_cache.TryGetValue(k, out QueueItem item) && item.Task.IsCompletedSuccessfully)
        {
            v = item.Task.Result;
            return true;
        }
        v = default;
        return false;
    }

    public bool RemoveCached(K k)
    {
        if (_cache.Remove(k, out QueueItem item))
        {
            item.Source.Cancel();
            OnRemoveCached(k);
            return true;
        }
        return false;
    }

    protected virtual void OnRemoveCached(K k) { }

    public bool IsLoading(K k)
    {
        if (_cache.TryGetValue(k, out QueueItem item))
        {
            return !item.Task.IsCompleted;
        }
        return false;
    }

    protected abstract Task<V> LoadInternal(K k, CancellationToken ctoken);

    public Task<V> LoadThreaded(K k)
    {
        async Task<V> impl(K k, CancellationToken ctoken = default)
        {
            try
            {
                V v = await LoadInternal(k, ctoken);
                return v;
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                {
                    Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                    Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                }
                throw;
            }
        }

        return _cache.GetOrAdd(k, (k) =>
        {
            CancellationTokenSource source = new();
            return new QueueItem(impl(k, source.Token), source);
        }).Task;
    }

    public void Clear()
    {
        foreach ((_, QueueItem item) in _cache)
            item.Source.Cancel();
        _cache.Clear();
        OnCacheClear();
    }

    protected virtual void OnCacheClear() { }
}