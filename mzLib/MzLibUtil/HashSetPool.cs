using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace MzLibUtil
{
    /// <summary>
    /// Represents a pool of <see cref="HashSet{T}"/> objects with a specified default capacity.
    /// </summary>
    /// <typeparam name="T">The type of elements in the <see cref="HashSet{T}"/>.</typeparam>
    /// <param name="defaultCapacity">The default capacity of the <see cref="HashSet{T}"/> objects in the pool.</param>
    public class HashSetPool<T>(int defaultCapacity = 100)
        : DefaultObjectPool<HashSet<T>>(new HashSetPoolPolicy<T>(defaultCapacity));

    // Custom policy for managing HashSet<double> objects in the pool
    public class HashSetPoolPolicy<T>(int defaultCapacity = 100) : PooledObjectPolicy<HashSet<T>>
    {
        public override HashSet<T> Create()
        {
            return new HashSet<T>(defaultCapacity);
        }

        public override bool Return(HashSet<T> obj)
        {
            obj.Clear(); // Ensure the HashSet is cleared before returning it to the pool
            return true;
        }
    }



    public class DictionaryPool<TKey, TValue>(int defaultCapacity = 100)
        : DefaultObjectPool<Dictionary<TKey, TValue>>(new DictionaryPoolPolicy<TKey, TValue>(defaultCapacity));
    public class DictionaryPoolPolicy<TKey, TValue>(int defaultCapacity = 100) : PooledObjectPolicy<Dictionary<TKey, TValue>>
    {
        public override Dictionary<TKey, TValue> Create()
        {
            return new Dictionary<TKey, TValue>(defaultCapacity);
        }
        public override bool Return(Dictionary<TKey, TValue> obj)
        {
            obj.Clear();
            return true;
        }
    }
}
