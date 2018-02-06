using System;

namespace GolombCodeFilterSet
{
	public class Filter
	{
		public byte P { get; set; }
		public int N { get; set; }
		public ulong ModulusP { get; set; }
		public ulong ModulusNP { get; set; }
		public byte[] Data { get; set; }

		public bool Match(byte[] data, byte[] key)
		{
			var bitStream = new BitStream(Data);
			var nphi = ModulusNP >> 32;
			var nplo = ((ulong)((uint)ModulusNP));


			var hash = SipHasher.Hash(key, data);
			var searchValue = Utils.FastReduction(hash, nphi, nplo);
			var lastValue = 0UL;
			try
			{
				while (lastValue < searchValue)
				{
					var currentValue = ReadUInt64(bitStream);
					currentValue += lastValue;
					if (currentValue == searchValue)
						return true;

					lastValue = currentValue;
				}
			}
			catch(InvalidOperationException)
			// This means we reached the end of the bits stream ao, the value was not found
			{
				return false;
			}
			return false;
		}

		private ulong ReadUInt64(BitStream bitStream)
		{
			var count = 0UL;
			var bit = bitStream.ReadBit();
			while (bit)
			{
				count++;
				bit = bitStream.ReadBit();
			}

			var remainder = bitStream.ReadBits(P);
			var value = (count * ModulusP) + remainder;
			return value;
		}
	}
}