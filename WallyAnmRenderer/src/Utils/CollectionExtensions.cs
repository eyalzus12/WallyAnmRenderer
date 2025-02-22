using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WallyAnmRenderer;

internal static class CollectionExtensions
{
    internal static V AddOrUpdate<K, V>(this Dictionary<K, V> dict, K key, Func<K, V> adder, Func<V, V> update) where K : notnull
    {
        ref V? value = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
        if (exists)
        {
            value = update(value!);
        }
        else
        {
            value = adder(key);
        }
        return value;
    }

    internal static V AddOrUpdate<K, V>(this Dictionary<K, V> dict, K key, V defaultValue, Func<V, V> update) where K : notnull
    {
        ref V? value = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
        if (exists)
        {
            value = update(value!);
        }
        else
        {
            value = defaultValue;
        }
        return value;
    }
}