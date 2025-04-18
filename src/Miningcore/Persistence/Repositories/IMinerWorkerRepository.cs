using System.Data;
using Miningcore.Persistence.Model;

namespace Miningcore.Persistence.Repositories;

public interface IMinerWorkerRepository
{
    Task<MinerWorkerStats> GetWorkerStatsAsync(IDbConnection con, IDbTransaction tx, string poolId, string address, string worker);
    Task UpdateWorkerStatsAsync(IDbConnection con, IDbTransaction tx, MinerWorkerStats settings);
}
