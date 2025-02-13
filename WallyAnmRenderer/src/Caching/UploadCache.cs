using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace WallyAnmRenderer;

// K - cache by
// I - CPU bound value created from K
// V - GPU bound value created from I
public abstract class UploadCache<K, I, V> where K : notnull
{
    protected abstract IEqualityComparer<K>? KeyEqualityComparer { get; }

    private readonly Dictionary<K, V> _cache;
    private readonly Queue<V> _deleteQueue = [];
    private readonly Queue<(K, I)> _queue = [];
    private readonly HashSet<K> _queueSet;

    public UploadCache()
    {
        _cache = new(KeyEqualityComparer);
        _queueSet = new(KeyEqualityComparer);
    }

    protected abstract I LoadIntermediate(K k);
    protected abstract V IntermediateToValue(I i);
    protected abstract void UnloadIntermediate(I i);
    protected abstract void UnloadValue(V v);

    public bool TryGetCached(K k, [MaybeNullWhen(false)] out V? v)
    {
        return _cache.TryGetValue(k, out v);
    }

    public void Load(K k)
    {
        if (_cache.ContainsKey(k))
            return;
        I i = LoadIntermediate(k);
        V v = IntermediateToValue(i);
        UnloadIntermediate(i);
        _cache[k] = v;
    }

    public void LoadInThread(K k)
    {
        if (_queueSet.Contains(k) || _cache.ContainsKey(k))
            return;
        _queueSet.Add(k);

        Task.Run(() =>
        {
            try
            {
                I i = LoadIntermediate(k);
                lock (_queue) _queue.Enqueue((k, i));
            }
            catch (Exception e)
            {
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

        lock (_queue)
        {
            amount = Math.Clamp(amount, 0, _queue.Count);
            for (int j = 0; j < amount; j++)
            {
                (K k, I i) = _queue.Dequeue();
                _queueSet.Remove(k);
                if (!_cache.ContainsKey(k))
                {
                    V v = IntermediateToValue(i);
                    _cache[k] = v;
                }
                UnloadIntermediate(i);
            }
        }
    }

    private void Unload()
    {
        lock (_deleteQueue)
        {
            while (_deleteQueue.Count > 0)
            {
                V v = _deleteQueue.Dequeue();
                UnloadValue(v);
            }
        }
    }

    public void Clear()
    {
        lock (_deleteQueue)
        {
            foreach ((_, V v) in _cache)
                _deleteQueue.Enqueue(v);
            _cache.Clear();
        }

        _queueSet.Clear();
        lock (_queue)
        {
            while (_queue.Count > 0)
            {
                (_, I i) = _queue.Dequeue();
                UnloadIntermediate(i);
            }
        }
    }

    public void InsertIntermediate(K k, I i)
    {
        if (_queueSet.Contains(k) || _cache.ContainsKey(k))
            return;

        _queueSet.Add(k);
        lock (_queue) _queue.Enqueue((k, i));
    }
}