using System;
using System.Collections.Generic;
using System.Linq;

namespace GolombCodeFilterSet
{
	// Implements a Golomb-coded set to be use in the creation of client-side filter
	// for a new kind Bitcoin light clients. This code is based on the BIP:
	// https://github.com/Roasbeef/bips/blob/master/gcs_light_client.mediawiki
	public class FilterBuilder22
    {
		private byte _P;
		private ulong _modP;

		public FilterBuilder22(byte P)
		{
			// NOTE: P must be a power of two as we target the specialized case of Golomb coding: Golomb - Rice coding
			if (P == 0x00 || (P & (P - 1)) != 0)
				throw new ArgumentException("P has to be a power of two value", nameof(P));

			_P = P;
			_modP = 1UL << P;
		}

		// P a value which is computed as 1/fp where fp is the desired false positive rate.
		// k is a 128-bit key
		public Filter Build(byte[] k, IEnumerable<byte[]> data)
		{
			if (data == null || !data.Any())
				throw new ArgumentException("data can not be null or empty array", nameof(data));

			var hs = ConstructHashedSet(_P, k, data);
			var filterData = Compress(hs, _P);
			var N = data.Count();

			return new Filter
			{
				P = _P,
				N = data.Count(),
				ModulusP = _modP,
				ModulusNP = ((ulong)N) * _modP,
				Data = filterData
			};
		}

		private List<ulong> ConstructHashedSet(byte P, byte[] key, IEnumerable<byte[]> data)
		{
			// N the number of items to be inserted into the set
			var N = data.Count();

			// The list of data item hashes
			var values = new List<ulong>(N);

			var modNP = ((ulong)N) * _modP;
			var nphi = modNP >> 32;
			var nplo = (ulong)((uint)modNP);
			// Process the data items and calculate the 64 bits hash for each of them
			foreach (var item in data)
			{
				var hash = SipHasher.Hash(key, item);
				var value = Utils.FastReduction(hash, nphi, nplo);
				values.Add(value);
			}

			return values;
		}

		private byte[] Compress(List<ulong> values, byte P)
		{
			// Sort the list in place
			values.Sort();

			var modP = 1UL << P;
			var bitStream = new BitStream();
			var sw = new GRCodedStreamWriter(bitStream, P);

			foreach(var value in values)
			{
				sw.Write(value);
			}
			return bitStream.ToByteArray();
		}
	}
}
