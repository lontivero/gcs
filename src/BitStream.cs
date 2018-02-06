using System;

namespace GolombCodeFilterSet
{
	class BitStream
	{
		private byte[] _buffer;
		private int _remainCount;

		public BitStream()
			: this(new byte[0])
		{
		}

		public BitStream(byte[] buffer)
		{
			var newBuffer = new byte[buffer.Length];
			Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
			_buffer = newBuffer;
			_remainCount = buffer.Length == 0 ? 0 : 8;
		}

		private void AddZeroByte()
		{
			var newBuffer = new byte[_buffer.Length + 1];
			Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);
			_buffer = newBuffer;
		}

		private void EnsureCapacity()
		{
			if (_remainCount == 0)
			{
				AddZeroByte();
				_remainCount = 8;
			}
		}

		public void WriteBit(bool bit)
		{
			EnsureCapacity();
			if (bit)
			{
				var lastIndex = _buffer.Length - 1;
				_buffer[lastIndex] |= (byte)(1 << (_remainCount - 1));
			}
			_remainCount--;
		}

		public void WriteByte(byte b)
		{
			EnsureCapacity();

			var lastIndex = _buffer.Length - 1;
			_buffer[lastIndex] |= (byte)(b >> (8 - _remainCount));

			AddZeroByte();
			_buffer[lastIndex + 1] = (byte)(b << _remainCount);
		}

		public bool ReadBit()
		{
			if (_buffer.Length == 0)
				throw new InvalidOperationException("The stream is empty");

			if(_remainCount == 0)
			{
				if (_buffer.Length == 1) {
					throw new InvalidOperationException("End of stream reached");
				}
				var newBuffer = new byte[_buffer.Length - 1];
				Buffer.BlockCopy(_buffer, 1, newBuffer, 0, _buffer.Length - 1);
				_buffer = newBuffer;
				_remainCount = 8;
			}

			var bit = _buffer[0] & 0x80;
			_buffer[0] <<= 1;
			_remainCount--;

			return bit != 0;
		}

		public ulong ReadBits(int count)
		{
			var val = 0UL;
			while(count >= 8)
			{
				val <<= 8;
				val |= (ulong)ReadByte();
				count -= 8;
			}

			while(count > 0)
			{
				val <<= 1;
				val |= ReadBit() ? 1UL : 0UL;
				count--;
			}
			return val;
		}

		public byte ReadByte()
		{
			if (_buffer.Length == 0)
				throw new InvalidOperationException("The stream is empty");

			if (_remainCount == 0)
			{
				if (_buffer.Length == 1)
				{
					throw new InvalidOperationException("End of stream reached");
				}
				var newBuffer = new byte[_buffer.Length - 1];
				Buffer.BlockCopy(_buffer, 1, newBuffer, 0, _buffer.Length - 1);
				_buffer = newBuffer;
				_remainCount = 8;
			}

			var b = _buffer[0];
			var newBuffer1 = new byte[_buffer.Length - 1];
			Buffer.BlockCopy(_buffer, 1, newBuffer1, 0, _buffer.Length - 1);
			_buffer = newBuffer1;
			if (_remainCount == 8)
			{
				return b;
			}

			if (_buffer.Length == 0)
			{
				throw new InvalidOperationException("End of stream reached");
			}

			b |= (byte)(_buffer[0] >> _remainCount);
			_buffer[0] <<= (8 - _remainCount);
			return b;
		}

		public byte[] ToByteArray()
		{
			return _buffer;
		}
	}
}
