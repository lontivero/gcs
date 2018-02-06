using System;
using System.Collections.Generic;
using System.Linq;

namespace GolombCodeFilterSet
{
	// Implements a Golomb-coded set to be use in the creation of client-side filter
	// for a new kind Bitcoin light clients. This code is based on the BIP:
	// https://github.com/Roasbeef/bips/blob/master/gcs_light_client.mediawiki
	public class Filter
	{
		public byte P { get; internal set; }
		public int N { get; internal set; }
		public ulong ModulusP { get; internal set; }
		public ulong ModulusNP { get; internal set; }
		public byte[] Data { get; internal set; }

		public static Filter Build(byte[] k, byte P, IEnumerable<byte[]> data)
		{
			// NOTE: P must be a power of two as we target the specialized case of Golomb coding: Golomb - Rice coding
			if (P == 0x00 || (P & (P - 1)) != 0)
				throw new ArgumentException("P has to be a power of two value", nameof(P));

			if (data == null || !data.Any())
				throw new ArgumentException("data can not be null or empty array", nameof(data));

			var modP = 1UL << P;

			var hs = ConstructHashedSet(P, k, data);
			var filterData = Compress(hs, P);
			var N = data.Count();

			return new Filter
			{
				P = P,
				N = data.Count(),
				ModulusP = modP,
				ModulusNP = ((ulong)N) * modP,
				Data = filterData
			};
		}

		private static List<ulong> ConstructHashedSet(byte P, byte[] key, IEnumerable<byte[]> data)
		{
			// N the number of items to be inserted into the set
			var N = data.Count();

			// The list of data item hashes
			var values = new List<ulong>(N);

			var modP = 1UL << P;
			var modNP = ((ulong)N) * modP;
			var nphi = modNP >> 32;
			var nplo = (ulong)((uint)modNP);

			// Process the data items and calculate the 64 bits hash for each of them
			foreach (var item in data)
			{
				var hash = SipHasher.Hash(key, item);
				var value = Utils.FastReduction(hash, nphi, nplo);
				values.Add(value);
			}

			values.Sort();
			return values;
		}

		private static byte[] Compress(List<ulong> values, byte P)
		{
			var bitStream = new BitStream();
			var sw = new GRCodedStreamWriter(bitStream, P);

			foreach (var value in values)
			{
				sw.Write(value);
			}
			return bitStream.ToByteArray();
		}


		public bool Match(byte[] data, byte[] key)
		{
			var bitStream = new BitStream(Data);
			var sr = new GRCodedStreamReader(bitStream, P);

			var nphi = ModulusNP >> 32;
			var nplo = ((ulong)((uint)ModulusNP));

			var hash = SipHasher.Hash(key, data);
			var searchValue = Utils.FastReduction(hash, nphi, nplo);
			try
			{
				var currentValue = sr.Read();
				while (currentValue < searchValue)
				{
					currentValue = sr.Read();
				}
				if (currentValue == searchValue)
					return true;
			}
			catch (InvalidOperationException)
			// This means we reached the end of the bits stream ao, the value was not found
			{
				return false;
			}
			return false;
		}

		public bool MatchAny(IEnumerable<byte[]> data, byte[] key)
		{
			if (data == null || !data.Any())
				throw new ArgumentException("data can not be null or empty array", nameof(data));

			var hs = ConstructHashedSet(P, key, data);

			var lastValue1 = 0UL;
			var lastValue2 = hs[0];
			var i = 1;

			var bitStream = new BitStream(Data);
			var sr = new GRCodedStreamReader(bitStream, P);

			while (lastValue1 != lastValue2)
			{
				if (lastValue1 > lastValue2)
				{
					if (i < hs.Count)
					{
						lastValue2 = hs[i];
						i++;
					}
					else
					{
						return false;
					}
				}
				else if (lastValue2 > lastValue1)
				{
					var val = sr.Read();
					lastValue1 = val;
				}
			}
			return true;
		}
	}
}