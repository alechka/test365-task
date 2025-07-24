using StackExchange.Redis;
using Test365.Common;

namespace Test365.ScoreService;

public class RedisDuplicateCheckingService(IDatabase redis) : IDuplicateCheckingService
{
    /// <inheritdoc/>
    public async Task<bool> LockAsync(string key)
    {
        var result = await redis.StringSetAsync(key, "1", expiry: TimeFrame.Span, when: When.NotExists );
        return result;
    }
}