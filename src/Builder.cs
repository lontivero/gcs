using System.Collections.Generic;
using GolombCodeFilterSet;
using NBitcoin;

namespace GolombCodeFilterSet
{
	public class Builder
	{
		private const int P = 20;

		public GcsFilter Build(Block block)
		{
			var key = block.GetHash().ToBytes();

			var buffer = new List<byte[]>();
			buffer.Add(key);

			foreach (var tx in block.Transactions)
			{
				foreach (var txOutput in tx.Outputs)
				{
					var witDestination = PayToWitTemplate.Instance.ExtractScriptPubKeyParameters(txOutput.ScriptPubKey);
					var isValidPayToWitness = witDestination != null;

					if (isValidPayToWitness)
					{
						var scriptPubKeyBytes = txOutput.ScriptPubKey.ToBytes();
						buffer.Add(scriptPubKeyBytes);
					}
				}
			}

			return GcsFilter.Build(key, P, buffer);
		}
	}
}
