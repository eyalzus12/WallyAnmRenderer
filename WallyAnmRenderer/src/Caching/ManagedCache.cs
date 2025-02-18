using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

public abstract class ManagedCache<K, V> where K : notnull
{
    private readonly ConcurrentDictionary<K, V> _cache = [];
    private readonly HashSet<K> _loading = [];

    public bool TryGetCached(K k, [MaybeNullWhen(false)] out V v)
    {
        return _cache.TryGetValue(k, out v);
    }

    public bool RemoveCached(K k)
    {
        if (_cache.Remove(k, out V? v))
        {
            OnRemoveCached(k, v);
            return true;
        }
        return false;
    }

    protected virtual void OnRemoveCached(K k, V v) { }


    public bool IsLoading(K k) => _loading.Contains(k);

    protected abstract V LoadInternal(K k);

    public void Load(K k)
    {
        if (_cache.ContainsKey(k))
            return;
        V v = LoadInternal(k);
        _cache[k] = v;
    }

    public void LoadInThread(K k)
    {
        if (_cache.ContainsKey(k))
            return;
        lock (_loading)
        {
            if (_loading.Contains(k))
                return;
            _loading.Add(k);
        }

        Task.Run(() =>
        {
            try
            {
                Load(k);
                lock (_loading) _loading.Remove(k);
            }
            catch (Exception e)
            {
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                throw;
            }
        });
    }

    public void Clear()
    {
        _cache.Clear();
        OnCacheClear();
    }

    protected virtual void OnCacheClear() { }
}