using System.Collections.Concurrent;

namespace Flit.Identity.Auth;

public sealed class AuthRateLimiter
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _attempts = new();
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public bool TryAcquire(string ip, string email)
    {
        var key = $"{ip}|{email.ToLowerInvariant()}";
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - Window;

        var queue = _attempts.GetOrAdd(key, _ => new Queue<DateTimeOffset>());

        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() < cutoff)
            {
                queue.Dequeue();
            }

            if (queue.Count >= MaxAttempts)
            {
                return false;
            }

            queue.Enqueue(now);
            return true;
        }
    }
}
