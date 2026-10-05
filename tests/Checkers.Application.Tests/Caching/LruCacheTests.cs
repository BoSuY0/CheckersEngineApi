using Checkers.Application.Caching;
using Microsoft.Extensions.Time.Testing;

namespace Checkers.Application.Tests.Caching;

public sealed class LruCacheTests
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public void TryGet_MissingKey_Misses()
    {
        var cache = CreateCache(capacity: 2);

        Assert.False(cache.TryGet("a", out _));
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsValue()
    {
        var cache = CreateCache(capacity: 2);
        cache.Set("a", 1);

        Assert.True(cache.TryGet("a", out var value));
        Assert.Equal(1, value);
    }

    [Fact]
    public void Set_WhenFull_EvictsLeastRecentlyUsed()
    {
        var cache = CreateCache(capacity: 2);
        cache.Set("a", 1);
        cache.Set("b", 2);

        cache.Set("c", 3);

        Assert.False(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
    }

    [Fact]
    public void TryGet_RefreshesRecency()
    {
        var cache = CreateCache(capacity: 2);
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.TryGet("a", out _);

        cache.Set("c", 3);

        Assert.True(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
    }

    [Fact]
    public void Set_ExistingKey_ReplacesValueWithoutEvicting()
    {
        var cache = CreateCache(capacity: 2);
        cache.Set("a", 1);
        cache.Set("b", 2);

        cache.Set("a", 10);

        Assert.True(cache.TryGet("a", out var value));
        Assert.Equal(10, value);
        Assert.True(cache.TryGet("b", out _));
    }

    [Fact]
    public void TryGet_ExpiresWhenTtlElapsed()
    {
        var cache = CreateCache(capacity: 2);
        cache.Set("a", 1);

        _time.Advance(Ttl - TimeSpan.FromTicks(1));
        Assert.True(cache.TryGet("a", out _));

        _time.Advance(TimeSpan.FromTicks(1));
        Assert.False(cache.TryGet("a", out _));
    }

    [Fact]
    public void TryGet_DoesNotExtendTtl()
    {
        var cache = CreateCache(capacity: 2);
        cache.Set("a", 1);

        _time.Advance(Ttl / 2);
        cache.TryGet("a", out _);
        _time.Advance(Ttl / 2);

        Assert.False(cache.TryGet("a", out _));
    }

    [Fact]
    public void Set_AfterExpiry_StoresFreshEntry()
    {
        var cache = CreateCache(capacity: 2);
        cache.Set("a", 1);
        _time.Advance(Ttl);

        cache.Set("a", 2);

        Assert.True(cache.TryGet("a", out var value));
        Assert.Equal(2, value);
    }

    private LruCache<string, int> CreateCache(int capacity) => new(capacity, Ttl, _time);
}
