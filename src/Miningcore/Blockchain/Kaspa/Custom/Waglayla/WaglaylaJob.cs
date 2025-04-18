using System;
using System.Numerics;
using Miningcore.Crypto;
using Miningcore.Crypto.Hashing.Algorithms;
using Miningcore.Extensions;
using Miningcore.Stratum;
using Miningcore.Util;
using NBitcoin;

namespace Miningcore.Blockchain.Kaspa.Custom.Waglayla;

public class WaglaylaJob: KaspaJob {
  protected Blake3IHash blake3Hasher;
  protected Sha3_256 sha3_256Hasher;

  public WaglaylaJob(IHashAlgorithm customBlockHeaderHasher, IHashAlgorithm customCoinbaseHasher, IHashAlgorithm customShareHasher): base(customBlockHeaderHasher, customCoinbaseHasher, customShareHasher) {
    this.blake3Hasher = new Blake3IHash();
    this.sha3_256Hasher = new Sha3_256();
  }

  private Span<byte> MatrixMultiply(Span<byte> input)
{
    // Example placeholder matrix operation
    byte[,] matrix = {
        { 1, 2, 3, 4 },
        { 5, 6, 7, 8 },
        { 9, 10, 11, 12 },
        { 13, 14, 15, 16 }
    };

    byte[] result = new byte[32];
    for (int i = 0; i < 32; i++)
    {
        result[i] = 0;
        for (int j = 0; j < 32; j++)
        {
            result[i] ^= (byte)(input[j] * matrix[i % 4, j % 4]); // Example multiplication
        }
    }

    return result.AsSpan();
}

  protected override Share ProcessShareInternal(StratumConnection worker, string nonce) {
    var context = worker.ContextAs < KaspaWorkerContext > ();

    BlockTemplate.Header.Nonce = Convert.ToUInt64(nonce, 16);

    var prePowHashBytes = SerializeHeader(BlockTemplate.Header, true);
    Span < byte > blake3Bytes = stackalloc byte[32];
    blake3Hasher.Digest(prePowHashBytes, blake3Bytes);

    Span < byte > sha3Bytes = stackalloc byte[32];
    sha3_256Hasher.Digest(blake3Bytes, sha3Bytes);

    Span < byte > matrixResult = MatrixMultiply(sha3Bytes);

    Span < byte > waglaylaHash = stackalloc byte[32];
    blake3Hasher.Digest(matrixResult, waglaylaHash);

    var targetHashCoinbaseBytes = new Target(new BigInteger(waglaylaHash.ToNewReverseArray(), true, true));
    var hashCoinbaseBytesValue = targetHashCoinbaseBytes.ToUInt256();

    var shareDiff = (double) new BigRational(KaspaConstants.Diff1b, targetHashCoinbaseBytes.ToBigInteger()) * shareMultiplier;

    // diff check
    var stratumDifficulty = context.Difficulty;
    var ratio = shareDiff / stratumDifficulty;

    // check if the share meets the much harder block difficulty (block candidate)
    var isBlockCandidate = hashCoinbaseBytesValue <= blockTargetValue;
    //var isBlockCandidate = true;

    // test if share meets at least workers current difficulty
    if (!isBlockCandidate && ratio < 0.99) {
      // check if share matched the previous difficulty from before a vardiff retarget
      if (context.VarDiff?.LastUpdate != null && context.PreviousDifficulty.HasValue) {
        ratio = shareDiff / context.PreviousDifficulty.Value;

        if (ratio < 0.99)
          throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share ({shareDiff})");

        // use previous difficulty
        stratumDifficulty = context.PreviousDifficulty.Value;
      } else
        throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share ({shareDiff})");
    }

    var result = new Share {
      BlockHeight = (long) BlockTemplate.Header.DaaScore,
        NetworkDifficulty = Difficulty,
        Difficulty = context.Difficulty / shareMultiplier
    };

    if (isBlockCandidate) {
      var hashBytes = SerializeHeader(BlockTemplate.Header, false);

      result.IsBlockCandidate = true;
      result.BlockHash = hashBytes.ToHexString();
    }

    return result;
  }
}