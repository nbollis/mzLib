using System;
using System.Collections.Generic;
using NUnit.Framework;
using MzLibUtil;

namespace Test.MzLibUtil;

[TestFixture]
public class HashSetPoolTests
{
    [Test]
    public void Get_ReturnsHashSetInstance()
    {
        var pool = new HashSetPool<int>();
        var hashSet = pool.Get();
        Assert.That(hashSet, Is.Not.Null);
        Assert.That(hashSet, Is.TypeOf<HashSet<int>>());
    }

    [Test]
    public void Return_ClearsAndReturnsHashSetToPool()
    {
        var pool = new HashSetPool<int>();
        var hashSet = pool.Get();
        hashSet.Add(1);
        pool.Return(hashSet);

        var newHashSet = pool.Get();
        Assert.That(newHashSet, Is.Empty);
    }
}

[TestFixture]
public class DictionaryPoolTests
{
    [Test]
    public void Get_ReturnsDictionaryInstance()
    {
        var dictionaryPool = new DictionaryPool<string, int>();
        var dictionary = dictionaryPool.Get();
        Assert.That(dictionary, Is.Not.Null);
        Assert.That(dictionary, Is.InstanceOf<Dictionary<string, int>>());
    }

    [Test]
    public void Return_ClearsAndReturnsDictionaryToPool()
    {
        var dictionaryPool = new DictionaryPool<string, int>();
        var dictionary = dictionaryPool.Get();
        dictionary["key"] = 42;

        dictionaryPool.Return(dictionary);

        Assert.That(dictionary.Count, Is.EqualTo(0));
    }

    [Test]
    public void Return_ThrowsArgumentNullException_WhenDictionaryIsNull()
    {
        var dictionaryPool = new DictionaryPool<string, int>();
        Assert.Throws<ArgumentNullException>(() => dictionaryPool.Return(null));
    }
}

[TestFixture]
public class ListPoolTests
{
    [Test]
    public void ListPool_Get_ReturnsListWithInitialCapacity()
    {
        // Arrange
        int initialCapacity = 16;
        var listPool = new ListPool<int>(initialCapacity);

        // Act
        var list = listPool.Get();

        // Assert
        Assert.That(list, Is.Not.Null);
        Assert.That(list.Capacity, Is.EqualTo(initialCapacity));
    }

    [Test]
    public void ListPool_Return_ClearsListBeforeReturningToPool()
    {
        // Arrange
        var listPool = new ListPool<int>();
        var list = listPool.Get();
        list.Add(1);
        list.Add(2);

        // Act
        listPool.Return(list);
        var newList = listPool.Get();

        // Assert
        Assert.That(newList, Is.Not.Null);
        Assert.That(newList, Is.Empty);
    }

    [Test]
    public void ListPool_Return_ThrowsArgumentNullException_WhenListIsNull()
    {
        // Arrange
        var listPool = new ListPool<int>();

        // Act & Assert
        Assert.That(() => listPool.Return(null), Throws.TypeOf<ArgumentNullException>());
    }
}