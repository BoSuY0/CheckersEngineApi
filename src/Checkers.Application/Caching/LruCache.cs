using System.Diagnostics.CodeAnalysis;

namespace Checkers.Application.Caching;

/// <summary>
/// A thread-safe, size-bounded cache that evicts the least recently used entry when full.
/// Each entry expires a fixed time after it was set, however often it is read.
/// </summary>
internal sealed class LruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly TimeSpan _timeToLive;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _entries = [];

    // Most recently used first.
    private readonly LinkedList<Entry> _recency = new();
    private readonly Lock _lock = new();

    public LruCache(int capacity, TimeSpan timeToLive, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _timeToLive = timeToLive;
        _timeProvider = timeProvider;
    }

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(key, out var node))
            {
                if (_timeProvider.GetUtcNow() < node.Value.ExpiresAt)
                {
                    _recency.Remove(node);
                    _recency.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                Remove(node);
            }

            value = default;
            return false;
        }
    }

    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                Remove(existing);
            }
            else if (_entries.Count == _capacity)
            {
                Remove(_recency.Last!);
            }

            _entries[key] = _recency.AddFirst(new Entry(key, value, _timeProvider.GetUtcNow() + _timeToLive));
        }
    }

    private void Remove(LinkedListNode<Entry> node)
    {
        _recency.Remove(node);
        _entries.Remove(node.Value.Key);
    }

    private readonly record struct Entry(TKey Key, TValue Value, DateTimeOffset ExpiresAt);
}
