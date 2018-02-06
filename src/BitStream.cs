using System;
using System.Collections;

namespace GolombCodeFilterSet
{
	class BitStream
	{
		private BitArray _buffer;
		private int _readPos;
		private int _writePos;
		private int _count;

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
			_count = _buffer.Length;
		}

		public bool ReadBit()
		{
			return _buffer[_readPos++];
		}

		public byte ReadByte()
		{
			var ret = (byte) 0;
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 7);
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 6);
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 5);
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 4);
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 3);
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 2);
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 1);
			ret |= (byte)((_buffer[_readPos++] ? 1 : 0) << 0);

			return ret;
		}

		public ulong ReadBits(int count)
		{
			var val = 0UL;
			while (count >= 8)
			{
				val <<= 8;
				var b = ReadByte();
				val |= (ulong) b;
				count -= 8;
			}

			while (count > 0)
			{
				val <<= 1;
				val |= _buffer[_readPos++] ? 1UL : 0UL; // ReadBit()
				count--;
			}
			return val;
		}

		public void WriteBit(bool bit)
		{
			if (_count == _writePos)
			{
				_count = (_count * 2) + 1;
				_buffer.Length = _count;
			}

			_buffer[_writePos++] = bit;
		}

		public void WriteBits(ulong data, byte count)
		{
			data <<= (64 - count);
			while (count >= 8)
			{
				var byt = (byte) (data >> (64 - 8));
				WriteByte(byt);

				data <<= 8;
				count -= 8;
			}

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
			if (_count  <= _writePos + 8)
			{
				_count += 8;
				_buffer.Length = _count;
			}

			WriteBit((b & (1 << 7)) != 0);
			WriteBit((b & (1 << 6)) != 0);
			WriteBit((b & (1 << 5)) != 0);
			WriteBit((b & (1 << 4)) != 0);
			WriteBit((b & (1 << 3)) != 0);
			WriteBit((b & (1 << 2)) != 0);
			WriteBit((b & (1 << 1)) != 0);
			WriteBit((b & (1 << 0)) != 0);
		}

		public byte[] ToByteArray()
		{
			var byteArray = new byte[(_writePos  + (_writePos-1)) / 8];
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
