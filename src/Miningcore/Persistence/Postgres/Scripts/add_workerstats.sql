CREATE TABLE workerstats
(
	id BIGSERIAL NOT NULL PRIMARY KEY,
	poolid TEXT NOT NULL,
	miner TEXT NOT NULL,
	worker TEXT NOT NULL,
  bestdifficulty DOUBLE PRECISION NOT NULL DEFAULT 0,
	created TIMESTAMPTZ NOT NULL
);

CREATE INDEX IDX_WORKERSTATS_POOL_CREATED on workerstats(poolid, created);
CREATE INDEX IDX_WORKERSTATS_POOL_MINER_CREATED on workerstats(poolid, miner, created);
CREATE INDEX IDX_WORKERSTATS_POOL_MINER_WORKER_CREATED_BESTDIFFICULTY on workerstats(poolid,miner,worker,created desc,bestdifficulty);
