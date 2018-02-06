using System;
using System.Collections;

namespace GolombCodeFilterSet
{
	class BitStream
	{
		private BitArray _buffer;
		private int _readPos;
		private int _writePos;

		public BitStream()
			:this(new BitArray(0))
		{
		}

		public BitStream(byte[] data)
			: this( new BitArray(data))
		{
		}

		public BitStream(BitArray bitArray)
		{
			_buffer = bitArray;
			_readPos = 0;
			_writePos = 0;
		}

		public bool ReadBit()
		{
			if (_buffer.Length == _readPos - 1)
				throw new InvalidOperationException("End of stream reached");

			return _buffer[_readPos++];
		}

		public ulong ReadBits(int count)
		{
			if (count > 64 || count < 1)
				throw new ArgumentOutOfRangeException(nameof(count), "the value has to be in the range 1, 64");
			var val = 0UL;
			for (var i = 0; i < count; i++)
			{
				val <<= 1;
				val |= ReadBit() ? 1UL : 0UL;
			}
			return val;
		}

		public void WriteBit(bool bit)
		{
			if (_buffer.Length == _writePos)
				_buffer.Length++;

			_buffer[_writePos++] = bit;
		}

		public void WriteBits(ulong data, byte count)
		{
			data <<= (64 - count);
			while (count > 0)
			{
				var bit = data >> (64 - 1);
				WriteBit(bit == 1);
				data <<= 1;
				count--;
			}
		}

		public void WriteByte(byte b)
		{
			for (var i = 7; i >= 0; i--)
			{
				WriteBit((b & (1 << i)) == 1);
			}
		}

		public byte[] ToByteArray()
		{
			var byteArray = new byte[(int)Math.Ceiling((double)_buffer.Length / 8)];
			_buffer.CopyTo(byteArray, 0);
			return byteArray;
		}
	}

	class GRCodedStreamWriter
	{
		private BitStream _stream;
		private byte _P;
		private ulong _modP;
		private ulong _lastValue;

		public GRCodedStreamWriter(BitStream stream, byte P)
		{
			_stream = stream;
			_P = P;
			_modP = (1UL << P);
			_lastValue = 0UL;
		}

		public void Write(ulong value)
		{
			var diff = value - _lastValue;

			var remainder = diff & (_modP - 1);
			var quotient = (diff - remainder) >> _P;

			while (quotient > 0)
			{
				_stream.WriteBit(true);
				quotient--;
			}
			_stream.WriteBit(false);
			_stream.WriteBits(remainder, _P);
			_lastValue = value;
		}
	}

	class GRCodedStreamReader
	{
		private BitStream _stream;
		private byte _P;
		private ulong _modP;
		private ulong _lastValue;

		public GRCodedStreamReader(BitStream stream, byte P)
		{
			_stream = stream;
			_P = P;
			_modP = (1UL << P);
			_lastValue = 0UL;
		}

		public ulong Read()
		{
			var currentValue = ReadUInt64();
			currentValue += _lastValue;
			_lastValue = currentValue;
			return currentValue;
		}

		private ulong ReadUInt64()
		{
			var count = 0UL;
			var bit = _stream.ReadBit();
			while (bit)
			{
				count++;
				bit = _stream.ReadBit();
			}

			var remainder = _stream.ReadBits(_P);
			var value = (count * _modP) + remainder;
			return value;
		}
	}
}
