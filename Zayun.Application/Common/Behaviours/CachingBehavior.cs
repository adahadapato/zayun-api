using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Zayun.Application.Common.Caching;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Results;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Zayun.Application.Common.Behaviours;

/// <summary>
/// Caches the result of any request marked [Cacheable].
/// - Only works for requests that return Result<T> (the normal shape of our queries).
/// - Only SUCCESSFUL results are cached; a failure is returned but never stored.
/// - The cache key = request type + user (if PerUser) + the request's values.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(HybridCache cache, ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Worked out ONCE per request type (static fields in a generic class are per TRequest/TResponse pair).
    private static readonly CacheableAttribute? Settings =
        typeof(TRequest).GetCustomAttribute<CacheableAttribute>();

    // If TResponse is Result<T>, this is T; otherwise null (and we don't cache).
    private static readonly Type? ValueType = GetResultValueType(typeof(TResponse));

    // Result<T>.Value, to read the value out for storing.
    private static readonly PropertyInfo? ValueProperty =
        ValueType is null ? null : typeof(TResponse).GetProperty("Value");

    // Result<T>.Success(T), to rebuild the Result from the stored value.
    private static readonly MethodInfo? SuccessMethod =
        ValueType is null
            ? null
            : typeof(TResponse).GetMethod(
                "Success",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                [ValueType]);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Not marked [Cacheable], or not a Result<T> → just run the handler.
        if (Settings is null || ValueType is null || ValueProperty is null || SuccessMethod is null)
            return await next();

        // A per-user query with no signed-in user: don't risk sharing data, just run it.
        if (Settings.PerUser && currentUser.UserId is null)
            return await next();

        var key = BuildKey(request);
        TResponse? freshResponse = default;   // set only if the handler actually ran

        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(Settings.Minutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(Settings.Minutes)
        };

        var json = await cache.GetOrCreateAsync<string?>(
            key,
            async _ =>
            {
                freshResponse = await next();

                // Failure → return null (we remove it straight after, so it isn't kept).
                if (freshResponse is Result { IsFailure: true })
                    return null;

                var value = ValueProperty.GetValue(freshResponse);
                return JsonSerializer.Serialize(value, ValueType, JsonOptions);
            },
            options,
            Settings.Tags,
            cancellationToken);

        // The handler ran in THIS call → return its real response (no need to deserialize).
        if (freshResponse is not null)
        {
            if (json is null)
                await cache.RemoveAsync(key, cancellationToken);   // never keep a failure

            return freshResponse;
        }

        // Served from cache, but somehow empty → clear it and run the handler.
        if (json is null)
        {
            await cache.RemoveAsync(key, cancellationToken);
            return await next();
        }

        // Served from cache → rebuild Result<T>.Success(value).
        var cachedValue = JsonSerializer.Deserialize(json, ValueType, JsonOptions);
        return (TResponse)SuccessMethod.Invoke(null, [cachedValue])!;
    }

    private string BuildKey(TRequest request)
    {
        var user = Settings!.PerUser ? currentUser.UserId!.Value.ToString() : "all";
        var body = JsonSerializer.Serialize(request, JsonOptions);
        var raw = $"{typeof(TRequest).FullName}|{user}|{body}";

        // Hash so keys stay short and safe whatever the request contains.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return $"zayun:{typeof(TRequest).Name}:{hash}";
    }

    private static Type? GetResultValueType(Type type)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Result<>))
                return t.GetGenericArguments()[0];
        }
        return null;
    }
}