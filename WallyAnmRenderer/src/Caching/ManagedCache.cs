using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

public abstract class ManagedCache<K, V> where K : notnull
{
    private readonly ConcurrentDictionary<K, LazyTask<V>> _cache = [];

    public bool TryGetCached(K k, [MaybeNullWhen(false)] out V v)
    {
        if (_cache.TryGetValue(k, out LazyTask<V>? task) && task.Value.IsCompletedSuccessfully)
        {
            v = task.Value.Result;
            return true;
        }
        v = default;
        return false;
    }

    public bool RemoveCached(K k)
    {
        if (_cache.Remove(k, out LazyTask<V>? task))
        {
            task.Value.Cancel();
            OnRemoveCached(k);
            return true;
        }
        return false;
    }

    protected virtual void OnRemoveCached(K k) { }

    public bool IsLoading(K k)
    {
        if (_cache.TryGetValue(k, out LazyTask<V>? item))
        {
            return !item.Value.IsCompleted;
        }
        return false;
    }

    protected abstract Task<V> LoadInternal(K k, CancellationToken ctoken);

    public async Task<V> LoadThreaded(K k)
    {
        async Task<V> impl(K k, CancellationToken ctoken = default)
        {
            try
            {
                V v = await LoadInternal(k, ctoken);
                return v;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                throw;
            }
        }

        return await _cache.GetOrAdd(k, (k) =>
        {
            return new LazyTask<V>(() =>
            {
                CancellationTokenSource source = new();
                return new CancellableTask<V>(impl(k, source.Token), source);
            });
        }).Value;
    }

    public void Clear()
    {
        foreach ((_, LazyTask<V> task) in _cache)
            task.Value.Cancel();
        _cache.Clear();
        OnCacheClear();
    }

    protected virtual void OnCacheClear() { }

    private sealed class LazyTask<T>(Func<CancellableTask<T>> valueFactory) : Lazy<CancellableTask<T>>(valueFactory);

    private sealed class CancellableTask<T>(Task<T> task, CancellationTokenSource tokenSource)
    {
        public Task<T> Task => task;

        public bool IsCompleted => task.IsCompleted;
        public bool IsCanceled => task.IsCanceled;
        public bool IsCompletedSuccessfully => task.IsCompletedSuccessfully;
        public T Result => task.Result;
        public TaskAwaiter<T> GetAwaiter() => task.GetAwaiter();

        public void Cancel() => tokenSource.Cancel();
        public bool IsCancellationRequested => tokenSource.IsCancellationRequested;
    }
}