using Miningcore.Blockchain.Progpow;
using NLog;

namespace Miningcore.Crypto.Hashing.Progpow.Evrprogpow;

[Identifier("evrprogpow")]
public class EvrProgpowLight : IProgpowLight
{
    private int numCaches;
    private readonly object cacheLock = new();
    private readonly Dictionary<int, Cache> caches = new();
    private Cache future;

    public string AlgoName { get; } = "EvrProgpow";

    public void Setup(int totalCache, ulong hardForkBlock = 0)
    {
        numCaches = totalCache;
    }

    public void Dispose()
    {
        foreach (var value in caches.Values)
            value.Dispose();

        future?.Dispose();
    }


    public async Task<IProgpowCache> GetCacheAsync(ILogger logger, int block, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await GetCacheAsync(logger, block);
    }


    public async Task<IProgpowCache> GetCacheAsync(ILogger logger, int block)
    {
        var epoch = block / EvrmoreConstants.EpochLength;
        Cache result;

        lock (cacheLock)
        {
            if (numCaches == 0)
                numCaches = 3;

            if (!caches.TryGetValue(epoch, out result))
            {
                while (caches.Count >= numCaches)
                {
                    var toEvict = caches.Values.OrderBy(x => x.LastUsed).First();
                    var key = caches.First(pair => pair.Value == toEvict).Key;

                    logger.Info(() => $"Evicting cache for epoch {toEvict.Epoch} in favour of epoch {epoch}");

                    toEvict.Dispose();
                    caches.Remove(key);
                }
                if (future != null && future.Epoch == epoch)
                {
                    logger.Debug(() => $"Using pre-generated cache for epoch {epoch}");
                    result = future;
                    future = null;
                }
                else
                {
                    logger.Info(() => $"No pre-generated cache available, creating new cache for epoch {epoch}");
                    result = new Cache(epoch);
                }

                caches[epoch] = result;
            }
            if (future == null || future.Epoch <= epoch)
            {
                logger.Info(() => $"Pre-generating cache for epoch {epoch + 1}");
                future = new Cache(epoch + 1);

#pragma warning disable 4014
                future.GenerateAsync(logger);
#pragma warning restore 4014
            }

            result.LastUsed = DateTime.Now;
        }

        // Ensure current cache is generated
        await result.GenerateAsync(logger);

        return result;
    }
}
