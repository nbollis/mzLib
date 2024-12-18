using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;

namespace MzLibUtil;
public class DictionaryPool<TKey, TValue>
{
    private readonly ObjectPool<Dictionary<TKey, TValue>> _pool;

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryPool{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity for the pooled Dictionary instances.</param>
    public DictionaryPool(int initialCapacity = 16)
    {
        var policy = new DictionaryPooledObjectPolicy<TKey, TValue>(initialCapacity);
        _pool = new DefaultObjectPool<Dictionary<TKey, TValue>>(policy, Environment.ProcessorCount * 2);
    }

    /// <summary>
    /// Retrieves a Dictionary instance from the pool.
    /// </summary>
    /// <returns>A Dictionary instance.</returns>
    public Dictionary<TKey, TValue> Get() => _pool.Get();

    /// <summary>
    /// Returns a Dictionary instance back to the pool.
    /// </summary>
    /// <param name="dictionary">The Dictionary instance to return.</param>
    public void Return(Dictionary<TKey, TValue> dictionary)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        dictionary.Clear(); // Ensure the Dictionary is clean before returning it to the pool
        _pool.Return(dictionary);
    }

    private class DictionaryPooledObjectPolicy<TKeyItem, TValueItem>(int initialCapacity)
        : PooledObjectPolicy<Dictionary<TKeyItem, TValueItem>>
    {
        public override Dictionary<TKeyItem, TValueItem> Create()
        {
            return new Dictionary<TKeyItem, TValueItem>(capacity: initialCapacity);
        }

        public override bool Return(Dictionary<TKeyItem, TValueItem> obj)
        {
            // Ensure the Dictionary can be safely reused
            obj.Clear();
            return true;
        }
    }
}
