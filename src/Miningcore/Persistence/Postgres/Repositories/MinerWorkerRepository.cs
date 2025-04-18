using System.Data;
using AutoMapper;
using Dapper;
using Miningcore.Persistence.Model;
using Miningcore.Persistence.Repositories;

namespace Miningcore.Persistence.Postgres.Repositories;

public class MinerWorkerRepository : IMinerWorkerRepository
{
    public MinerWorkerRepository(IMapper mapper)
    {
        this.mapper = mapper;
    }

    private readonly IMapper mapper;

    public async Task<MinerWorkerStats> GetWorkerStatsAsync(IDbConnection con, IDbTransaction tx, string poolId, string address, string worker)
    {
        const string query = @"SELECT * FROM workerstats WHERE poolid = @poolId AND address = @address AND worker = @worker";

        var entity = await con.QuerySingleOrDefaultAsync<Entities.MinerWorkerStats>(query, new {poolId, address, worker}, tx);

        return mapper.Map<MinerWorkerStats>(entity);
    }

    public Task UpdateWorkerStatsAsync(IDbConnection con, IDbTransaction tx, MinerWorkerStats settings)
    {
        const string query = @"INSERT INTO workerstats(poolid, address, worker, bestdifficulty, created, updated)
            VALUES(@poolid, @address, @worker, @bestdifficulty, now(), now())
            ON CONFLICT ON CONSTRAINT workerstats_pkey DO UPDATE
            SET bestdifficulty = @bestdifficulty, updated = now()
            WHERE workerstats.poolid = @poolid AND workerstats.address = @address AND workerstats.worker = @worker";

        return con.ExecuteAsync(query, settings, tx);
    }
}
