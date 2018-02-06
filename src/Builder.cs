using System;
using System.Collections.Generic;
using System.Linq;

namespace GolombCodeFilterSet
{
	// Implements a Golomb-coded set to be use in the creation of client-side filter
	// for a new kind Bitcoin light clients. This code is based on the BIP:
	// https://github.com/Roasbeef/bips/blob/master/gcs_light_client.mediawiki
	public class FilterBuilder
    {
		private byte _P;
		private ulong _modP;

		public FilterBuilder(byte P)
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

			// Using N and P we compute F = N * P. F constricts the range of the hashed values accordingly in order to 
			// achieve our desired false positive rate.
			var F = P * N;

			// The list of data item hashes
			var values = new List<ulong>(N);

			var modNP = ((ulong)N) * _modP;
			var nphi = modNP >> 32;
			var nplo = (ulong)((uint)modNP);
			// Process the data items and calculate the 64 bits hash for each of them
			foreach (var item in data)
			{
				var hash = SipHasher.Hash(key, item); // * (ulong)F) >> 64;
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

			var lastValue = 0UL;
			foreach(var value in values)
			{
				var diff = value - lastValue;
				var remainder =  diff & (modP - 1);
				var quotient = (diff - remainder) >> P;

				UnaryEncode(bitStream, quotient);

				// Write the remainder as a big-endian integer with enough bits
				// to represent the appropriate collision probability.
				WriteBitsBigEndian(bitStream, remainder, P);

				lastValue = value;
			}
			return bitStream.ToByteArray();
		}

		// Unary coding is an entropy encoding that represents a natural number, n, with n ones followed by a zero
		// For example:
		//   n = 0      enc = 0
		//   n = 1      enc = 10
		//   n = 2      enc = 110
		//   n = 3      enc = 1110
		// https://en.wikipedia.org/wiki/Unary_coding
		private void UnaryEncode(BitStream bitStream, ulong n)
		{
			while (n > 0)
			{
				bitStream.WriteBit(true);
				n--;
			}
			bitStream.WriteBit(false);
		}

		private void WriteBitsBigEndian(BitStream bitStream, ulong data, int count)
		{
			data <<= (64 - count);
			while(count >= 8)
			{
				var b = (byte)(data >> (64 - 8));
				bitStream.WriteByte(b);
				data <<= 8;
				count -= 8;
			}

			while(count > 0)
			{
				var bit = data >> (64 - 1);
				bitStream.WriteBit(bit == 1);
				data <<= 1;
				count--;
			}
		}
	}
}
