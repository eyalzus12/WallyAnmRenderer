using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

// K - cache by
// I - CPU bound value created from K
// V - GPU bound value created from I
public abstract class UploadCache<K, I, V> where K : notnull
{
    private readonly record struct QueueElement(K Key, I Intermediate, ulong Version);

    protected abstract IEqualityComparer<K>? KeyEqualityComparer { get; }

    protected ulong CacheVersion { get; private set; } = 0;
    private readonly Dictionary<K, V> _cache;
    private readonly HashSet<K> _errored;
    private readonly ConcurrentQueue<V> _deleteQueue = [];
    private readonly ConcurrentQueue<QueueElement> _queue = [];
    private readonly HashSet<K> _queueSet;

    public UploadCache()
    {
        _cache = new(KeyEqualityComparer);
        _errored = new(KeyEqualityComparer);
        _queueSet = new(KeyEqualityComparer);
    }

    protected abstract I LoadIntermediate(K k);
    protected abstract V IntermediateToValue(I i);
    protected abstract void UnloadIntermediate(I i);
    protected virtual void InitValue(V v) { }
    protected abstract void UnloadValue(V v);

    public bool TryGetCached(K k, [MaybeNullWhen(false)] out V? v)
    {
        return _cache.TryGetValue(k, out v);
    }

    public bool DidError(K k) => _errored.Contains(k);

    public void Load(K k)
    {
        if (_cache.ContainsKey(k))
            return;
        I i = LoadIntermediate(k);
        V v = IntermediateToValue(i);
        UnloadIntermediate(i);
        InitValue(v);
        _cache[k] = v;
    }

    public void LoadInThread(K k)
    {
        if (_queueSet.Contains(k) || _cache.ContainsKey(k))
            return;
        _queueSet.Add(k);

        ulong currentVersion = CacheVersion;
        Task.Run(() =>
        {
            try
            {
                I i = LoadIntermediate(k);
                if (currentVersion != CacheVersion)
                {
                    UnloadIntermediate(i);
                    return;
                }

                _queue.Enqueue(new(k, i, currentVersion));
            }
            catch (Exception e)
            {
                lock (_errored) _errored.Add(k);

                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                throw;
            }
        });
    }

    /// <summary>
    /// ONLY CALL FROM MAIN THREAD!
    /// </summary>
    public void Upload(int amount)
    {
        Unload();

        for (int j = 0; j < amount; j++)
        {
            if (!_queue.TryDequeue(out QueueElement queued))
                break;
            (K k, I i, ulong version) = queued;

            _errored.Remove(k);
            _queueSet.Remove(k);

            if (version == CacheVersion && !_cache.ContainsKey(k))
            {
                V v = IntermediateToValue(i);
                _cache[k] = v;
                UnloadIntermediate(i);
                InitValue(v);
            }
            else
            {
                UnloadIntermediate(i);
            }
        }
    }

    private void Unload()
    {
        while (_deleteQueue.TryDequeue(out V? v))
            UnloadValue(v);
    }

    public void Clear()
    {
        CacheVersion++;

        foreach ((_, V v) in _cache)
            _deleteQueue.Enqueue(v);
        _cache.Clear();
        _errored.Clear();

        _queueSet.Clear();

        while (_queue.TryDequeue(out QueueElement queued))
            UnloadIntermediate(queued.Intermediate);
    }
}