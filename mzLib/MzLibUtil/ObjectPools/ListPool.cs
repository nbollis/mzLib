using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace MzLibUtil;

public class ListPool<T>
{
    private readonly ObjectPool<List<T>> _pool;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashSetPool{T}"/> class.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity for the pooled HashSet instances.</param>
    public ListPool(int initialCapacity = 16)
    {
        var policy = new ListPooledObjectPolicy<T>(initialCapacity);
        _pool = new DefaultObjectPool<List<T>>(policy, Environment.ProcessorCount * 2);
    }

    /// <summary>
    /// Retrieves a HashSet instance from the pool.
    /// </summary>
    /// <returns>A HashSet instance.</returns>
    public List<T> Get() => _pool.Get();

    /// <summary>
    /// Returns a HashSet instance back to the pool.
    /// </summary>
    /// <param name="list">The HashSet instance to return.</param>
    public void Return(List<T> list)
    {
        if (list == null) throw new ArgumentNullException(nameof(list));
        list.Clear(); // Ensure the HashSet is clean before returning it to the pool
        _pool.Return(list);
    }

    private class ListPooledObjectPolicy<TItem>(int initialCapacity) : PooledObjectPolicy<List<TItem>>
    {
        public override List<TItem> Create()
        {
            return new List<TItem>(capacity: initialCapacity);
        }

        public override bool Return(List<TItem> list)
        {
            // Ensure the HashSet can be safely reused
            list.Clear();
            return true;
        }
    }
}