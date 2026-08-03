using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;
using System.Threading.RateLimiting;

namespace LogicFit.API.RateLimiting;

/// <summary>
/// A non-queued fixed-window limiter whose counter is atomically maintained by Redis.
/// It keeps the existing ASP.NET Core rate-limiter contract while making counters shared by
/// all API instances that use the same Redis namespace.
/// </summary>
public sealed class RedisFixedWindowRateLimiter : RateLimiter
{
    private const string AcquireScript = """
        local amount = tonumber(ARGV[1])
        local limit = tonumber(ARGV[2])
        local windowMilliseconds = tonumber(ARGV[3])
        local current = tonumber(redis.call('GET', KEYS[1]) or '0')
        local ttl = redis.call('PTTL', KEYS[1])

        if amount == 0 then
            if current >= limit then
                return {-1, ttl}
            end
            return {current, ttl}
        end

        if current + amount > limit then
            if ttl < 0 then
                ttl = windowMilliseconds
            end
            return {-1, ttl}
        end

        local updated = redis.call('INCRBY', KEYS[1], amount)
        if ttl < 0 then
            redis.call('PEXPIRE', KEYS[1], windowMilliseconds)
            ttl = windowMilliseconds
        end
        return {updated, ttl}
        """;

    private readonly IDatabase _database;
    private readonly RedisKey _key;
    private readonly int _permitLimit;
    private readonly long _windowMilliseconds;
    private readonly TimeSpan _window;
    private long _successfulLeases;
    private long _failedLeases;

    public RedisFixedWindowRateLimiter(
        IConnectionMultiplexer connection,
        string namespacePrefix,
        string policyName,
        string partitionKey,
        int permitLimit,
        TimeSpan window)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(namespacePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        if (permitLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(permitLimit));
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window));

        _database = connection.GetDatabase();
        _key = BuildKey(namespacePrefix, policyName, partitionKey);
        _permitLimit = permitLimit;
        _window = window;
        _windowMilliseconds = checked((long)Math.Ceiling(window.TotalMilliseconds));
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics()
    {
        return new RateLimiterStatistics
        {
            CurrentAvailablePermits = 0,
            CurrentQueuedCount = 0,
            TotalSuccessfulLeases = Interlocked.Read(ref _successfulLeases),
            TotalFailedLeases = Interlocked.Read(ref _failedLeases)
        };
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        try
        {
            var result = _database.ScriptEvaluate(
                AcquireScript,
                new[] { _key },
                CreateArguments(permitCount));

            return ParseResult(result);
        }
        catch (RedisException)
        {
            // A limiter outage must not silently turn into an unlimited endpoint.
            return RecordFailure(RedisRateLimitLease.Rejected(_window));
        }
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = await _database.ScriptEvaluateAsync(
                    AcquireScript,
                    new[] { _key },
                    CreateArguments(permitCount))
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return ParseResult(result);
        }
        catch (RedisException)
        {
            return RecordFailure(RedisRateLimitLease.Rejected(_window));
        }
    }

    protected override void Dispose(bool disposing)
    {
        // The shared multiplexer is owned by the host container, not by a partition limiter.
    }

    private RedisValue[] CreateArguments(int permitCount)
    {
        return new RedisValue[]
        {
            permitCount,
            _permitLimit,
            _windowMilliseconds
        };
    }

    private RateLimitLease ParseResult(RedisResult result)
    {
        var values = (RedisResult[]?)result;
        if (values is null || values.Length < 2)
            return RecordFailure(RedisRateLimitLease.Rejected(_window));

        var marker = long.Parse(values[0].ToString(), CultureInfo.InvariantCulture);
        var ttlMilliseconds = long.Parse(values[1].ToString(), CultureInfo.InvariantCulture);

        if (marker < 0)
        {
            var retryAfter = ttlMilliseconds > 0
                ? TimeSpan.FromMilliseconds(ttlMilliseconds)
                : _window;
            return RecordFailure(RedisRateLimitLease.Rejected(retryAfter));
        }

        Interlocked.Increment(ref _successfulLeases);
        return RedisRateLimitLease.Acquired();
    }

    private RateLimitLease RecordFailure(RateLimitLease lease)
    {
        Interlocked.Increment(ref _failedLeases);
        return lease;
    }

    private static RedisKey BuildKey(
        string namespacePrefix,
        string policyName,
        string partitionKey)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(partitionKey));
        var safePolicyName = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(policyName)))
            .ToLowerInvariant();

        return $"{namespacePrefix.TrimEnd(':')}:rate-limit:{safePolicyName}:{Convert.ToHexString(digest)}";
    }

    private sealed class RedisRateLimitLease : RateLimitLease
    {
        private readonly TimeSpan? _retryAfter;

        private RedisRateLimitLease(TimeSpan? retryAfter)
        {
            _retryAfter = retryAfter;
        }

        public override bool IsAcquired => _retryAfter is null;

        public override IEnumerable<string> MetadataNames => _retryAfter is null
            ? Array.Empty<string>()
            : new[] { MetadataName.RetryAfter.Name };

        public static RedisRateLimitLease Acquired() => new(null);

        public static RedisRateLimitLease Rejected(TimeSpan retryAfter) => new(retryAfter);

        public override IEnumerable<KeyValuePair<string, object?>> GetAllMetadata()
        {
            if (_retryAfter is not null)
            {
                yield return new KeyValuePair<string, object?>(
                    MetadataName.RetryAfter.Name,
                    _retryAfter.Value);
            }
        }

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (_retryAfter is not null && metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = _retryAfter.Value;
                return true;
            }

            metadata = null;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
