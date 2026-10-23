using System.Collections.Concurrent;

namespace YelG.BLL.Common
{
    /// <summary>Bound from the "LoginThrottle" configuration section.</summary>
    public sealed class LoginThrottleOptions
    {
        /// <summary>Failed attempts on one user name, existing or not, before it is blocked.</summary>
        public int MaxFailuresPerUser { get; init; } = 5;

        /// <summary>Failed attempts from one IP address, over all user names, before it is blocked.</summary>
        public int MaxFailuresPerAddress { get; init; } = 20;

        /// <summary>Failures older than this are forgotten (sliding window).</summary>
        public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// Counts failed sign-ins per user name and per IP address in a sliding window and blocks further
    /// attempts once either limit is reached. Only failures count, so colleagues behind one office IP
    /// are not blocked by normal sign-ins. Identity's own lockout still applies to existing accounts;
    /// this also covers user names that do not exist. In memory: one app instance, reset on restart.
    /// </summary>
    public sealed class LoginThrottle
    {
        private const int PruneThreshold = 5000;

        private readonly LoginThrottleOptions _options;
        private readonly TimeProvider _clock;
        private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _failures = new();

        public LoginThrottle(LoginThrottleOptions options, TimeProvider clock)
        {
            _options = options;
            _clock = clock;
        }

        /// <summary>How long until another attempt is allowed, or null when it is allowed now.</summary>
        public TimeSpan? RetryAfter(string? address, string userName)
        {
            var now = _clock.GetUtcNow();
            var wait = Wait(UserKey(userName), _options.MaxFailuresPerUser, now);
            if (address is not null)
            {
                var byAddress = Wait(AddressKey(address), _options.MaxFailuresPerAddress, now);
                if (byAddress > wait) wait = byAddress;
            }
            return wait > TimeSpan.Zero ? wait : null;
        }

        /// <summary>Counts a failed attempt. True when this failure is the one that starts a block (log it once).</summary>
        public bool RecordFailure(string? address, string userName)
        {
            var now = _clock.GetUtcNow();
            if (_failures.Count > PruneThreshold) Prune(now);

            var blocked = Add(UserKey(userName), _options.MaxFailuresPerUser, now);
            if (address is not null) blocked |= Add(AddressKey(address), _options.MaxFailuresPerAddress, now);
            return blocked;
        }

        /// <summary>A successful sign-in clears the user name's failures (the address keeps its count).</summary>
        public void Reset(string userName) => _failures.TryRemove(UserKey(userName), out _);

        private static string UserKey(string userName) => "u:" + userName.Trim().ToUpperInvariant();

        private static string AddressKey(string address) => "a:" + address;

        private TimeSpan Wait(string key, int max, DateTimeOffset now)
        {
            if (!_failures.TryGetValue(key, out var times)) return TimeSpan.Zero;
            lock (times)
            {
                Expire(times, now);
                if (times.Count < max) return TimeSpan.Zero;
                // Allowed again once enough of the oldest failures leave the window.
                return times.ElementAt(times.Count - max) + _options.Window - now;
            }
        }

        private bool Add(string key, int max, DateTimeOffset now)
        {
            var times = _failures.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
            lock (times)
            {
                Expire(times, now);
                times.Enqueue(now);
                return times.Count == max;
            }
        }

        private void Expire(Queue<DateTimeOffset> times, DateTimeOffset now)
        {
            while (times.Count > 0 && times.Peek() <= now - _options.Window) times.Dequeue();
        }

        private void Prune(DateTimeOffset now)
        {
            foreach (var (key, times) in _failures)
            {
                bool empty;
                lock (times)
                {
                    Expire(times, now);
                    empty = times.Count == 0;
                }
                if (empty) _failures.TryRemove(key, out _);
            }
        }
    }
}
