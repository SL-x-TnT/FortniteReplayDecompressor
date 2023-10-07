using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Unreal.Core.Extensions;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;

namespace Unreal.Core
{
    /// <summary>
    /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Public/Serialization/BitArchive.h
    /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Private/Serialization/BitArchive.cpp
    /// </summary>
    public unsafe partial class BitReader : FBitArchive
    {
        /// <summary>
        /// Position in current BitArray. Set with <see cref="Seek(int, SeekOrigin)"/>
        /// </summary>
        public override int Position { get => _position; protected set => _position = value; }

        protected int _position;

        /// <summary>
        /// Last used bit Position in current BitArray. Used to avoid reading trailing zeros to fill last byte.
        /// </summary>
        public int LastBit { get; private set; }

        private int[] _tempLastBit = GetPool();
        private int[] _tempPosition = GetPool();

        private static ConcurrentQueue<int[]> _positionQueues = new ConcurrentQueue<int[]>();

        /// <summary>
        /// For pushing and popping FBitReaderMark positions.
        /// </summary>
        public int MarkPosition { get; private set; }

        public BitReader()
        {

        }

        public BitReader(byte* ptr, int byteCount, int bitCount)
        {
            CreateBitArray(ptr, byteCount, bitCount);
        }

        public BitReader(bool* boolPtr, int bitCount)
        {
            Bits = boolPtr;
            LastBit = bitCount;
        }

        private static int[] GetPool()
        {
            if (_positionQueues.TryDequeue(out int[] result))
            {
                return result;
            }

            result = new int[8];

            return result;
        }

        /// <summary>
        /// Returns whether <see cref="Position"/> in current <see cref="Bits"/> is greater than the lenght of the current <see cref="Bits"/>.
        /// </summary>
        /// <returns>true, if <see cref="Position"/> is greater than lenght, false otherwise</returns>
        public override bool AtEnd()
        {
            return _position >= LastBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool CanRead(int count)
        {
            return _position + count <= LastBit;
        }

        /// <summary>
        /// Returns the bit at <see cref="Position"/> and does not advance the <see cref="Position"/> by one bit.
        /// </summary>
        /// <returns>The value of the bit at position index.</returns>
        /// <seealso cref="ReadBit"/>
        public override bool PeekBit()
        {
            return Bits[_position];
        }

        /// <summary>
        /// Returns the bit at <see cref="Position"/> and advances the <see cref="Position"/> by one bit.
        /// </summary>
        /// <returns>The value of the bit at position index.</returns>
        /// <seealso cref="PeekBit"/>
        public override bool ReadBit()
        {
            if (_position >= LastBit)
            {
                IsError = true;
                return false;
            }

            return Bits[_position++];
        }

        /// <summary>
        /// Retuns int and advances the <see cref="Position"/> by <paramref name="bits"/> bits.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        /// <returns>int</returns>
        public int ReadBitsToInt(int bitCount)
        {
            if (!CanRead(bitCount))
            {
                IsError = true;

                return 0;
            }

            var result = 0;

            for (var i = 0; i < bitCount; i++)
            {
                result |= (byte)(GetAsByte(_position + i) << i);
            }

            _position += bitCount;

            return result;
        }

        public ulong ReadBitsToLong(int bitCount)
        {
            if (!CanRead(bitCount))
            {
                IsError = true;

                return 0;
            }

            ulong result = 0;

            for (var i = 0; i < bitCount; i++)
            {
                result |= (byte)(GetAsByte(_position + i) << i);
            }

            _position += bitCount;

            return result;
        }

        /// <summary>
        /// Retuns bool[] and advances the <see cref="Position"/> by <paramref name="bits"/> bits.
        /// </summary>
        /// <param name="bits">The number of bits to read.</param>
        /// <returns>bool[]</returns>

        public override ReadOnlyMemory<bool> ReadBits(int bitCount)
        {
            if (!CanRead(bitCount) || bitCount < 0)
            {
                IsError = true;
                return ReadOnlyMemory<bool>.Empty;
            }


            var result = _items.Slice(_position, bitCount);

            _position += bitCount;

            return result;
        }

        public override ReadOnlyMemory<bool> ReadBits(uint bitCount)
        {
            return ReadBits((int)bitCount);
        }

        /// <summary>
        /// Returns the bit at <see cref="Position"/> and advances the <see cref="Position"/> by one bit.
        /// </summary>
        /// <returns>The value of the bit at position index.</returns>
        /// <seealso cref="ReadBit"/>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public override bool ReadBoolean()
        {
            return ReadBit();
        }

        /// <summary>
        /// Returns the byte at <see cref="Position"/>
        /// </summary>
        /// <returns>The value of the byte at <see cref="Position"/> index.</returns>
        public override byte PeekByte()
        {
            var result = ReadByte();
            _position -= 8;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe byte ReadByteNoCheck()
        {
            var result = new byte();

            var pos = _position;

            result |= (GetAsByte(pos + 0));
            result |= (byte)(GetAsByte(pos + 1) << 1);
            result |= (byte)(GetAsByte(pos + 2) << 2);
            result |= (byte)(GetAsByte(pos + 3) << 3);
            result |= (byte)(GetAsByte(pos + 4) << 4);
            result |= (byte)(GetAsByte(pos + 5) << 5);
            result |= (byte)(GetAsByte(pos + 6) << 6);
            result |= (byte)(GetAsByte(pos + 7) << 7);

            _position += 8;

            return result;
        }

        /// <summary>
        /// Returns the byte at <see cref="Position"/> and advances the <see cref="Position"/> by 8 bits.
        /// </summary>
        /// <returns>The value of the byte at <see cref="Position"/> index.</returns>
        public override byte ReadByte()
        {
            if (!CanRead(8))
            {
                IsError = true;

                return 0;
            }

            return ReadByteNoCheck();
        }

        public override T ReadByteAsEnum<T>()
        {
            return (T)Enum.ToObject(typeof(T), ReadByte());
        }

        public void ReadBytes(Span<byte> data)
        {
            if (!CanRead(data.Length * 8))
            {
                IsError = true;
                return;
            }

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = ReadByteNoCheck();
            }
        }

        public override byte[] ReadBytes(int byteCount)
        {
            if (byteCount < 0)
            {
                IsError = true;
                return new byte[0];
            }

            if (!CanRead(byteCount))
            {
                IsError = true;
                return Array.Empty<byte>();
            }

            var result = new byte[byteCount];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ReadByteNoCheck();
            }

            return result;
        }

        public override byte[] ReadBytes(uint byteCount)
        {
            return ReadBytes((int)byteCount);
        }

        public override T[] ReadArray<T>(Func<T> func1)
        {
            throw new NotImplementedException();
        }

        public override string ReadBytesToString(int count)
        {
            //Never hits this for newer replay

            // https://github.com/dotnet/corefx/issues/10013
            return BitConverter.ToString(ReadBytes(count)).Replace("-", "");
        }

        /// <summary>
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Private/Containers/String.cpp#L1390
        /// </summary>
        /// <returns>string</returns>
        public override string ReadFString()
        {
            var length = ReadInt32();

            if (length == 0)
            {
                return String.Empty;
            }

            var isUnicode = length < 0;
            length = isUnicode ? -2 * length : length;

            if (length > 256 || length < 0)
            {
                IsError = true;

                return String.Empty;
            }

            Span<byte> bytes = stackalloc byte[length];
            ReadBytes(bytes);

            return isUnicode ? Encoding.Unicode.GetString(bytes.Slice(0, bytes.Length - 2)) : Encoding.Default.GetString(bytes.Slice(0, bytes.Length - 1));
        }

        public override string ReadGUID()
        {
            return ReadBytesToString(16);
        }

        public override string ReadGUID(int size)
        {
            return ReadBytesToString(size);
        }

        /// <summary>
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Public/Serialization/BitReader.h#L69
        /// </summary>
        /// <param name="maxValue"></param>
        /// <returns>uint</returns>
        /// <exception cref="OverflowException"></exception>
        public unsafe override uint ReadSerializedInt(int maxValue)
        {
            int value = 0;
            int count = 0;


            var mValue = (uint)maxValue;

            for (uint mask = 1; (value + mask) < mValue; mask *= 2)
            {
                if (_position >= LastBit)
                {
                    IsError = true;
                    return 0;
                }

                value |= GetAsByte(_position++) << count++;
            }

            return (uint)value;
        }

        public override short ReadInt16()
        {
            Span<byte> value = stackalloc byte[2];

            ReadBytes(value);

            return BinaryPrimitives.ReadInt16LittleEndian(value);
        }

        public override int ReadInt32()
        {
            Span<byte> value = stackalloc byte[4];

            ReadBytes(value);

            return BinaryPrimitives.ReadInt32LittleEndian(value);
        }

        public override bool ReadInt32AsBoolean()
        {
            return ReadInt32() == 1;
        }

        public override long ReadInt64()
        {
            Span<byte> value = stackalloc byte[8];

            ReadBytes(value);

            return BinaryPrimitives.ReadInt64LittleEndian(value);
        }

        /// <summary>
        /// Retuns uint
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Private/Serialization/BitReader.cpp#L254
        /// </summary>
        /// <returns>uint</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override uint ReadIntPacked()
        {
            int value = 0;
            byte count = 0;
            var remaining = true;

            while (remaining)
            {
                if (_position + 8 > LastBit)
                {
                    IsError = true;
                    return 0;
                }

                remaining = Bits[_position];

                value |= GetAsByte(_position + 1) << count;
                value |= GetAsByte(_position + 2) << (count + 1);
                value |= GetAsByte(_position + 3) << (count + 2);
                value |= GetAsByte(_position + 4) << (count + 3);
                value |= GetAsByte(_position + 5) << (count + 4);
                value |= GetAsByte(_position + 6) << (count + 5);
                value |= GetAsByte(_position + 7) << (count + 6);

                _position += 8;
                count += 7;
            }

            return (uint)value;
        }

        /// <summary>
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Engine/Classes/Engine/NetSerialization.h#L1210
        /// </summary>
        /// <returns>Vector</returns>
        ///         
        public override FVector ReadPackedVector(int scaleFactor, int maxBits)
        {
            if (EngineNetworkVersion >= EngineNetworkVersionHistory.HISTORY_PACKED_VECTOR_LWC_SUPPORT && EngineNetworkVersion != EngineNetworkVersionHistory.HISTORY_21_AND_VIEWPITCH_ONLY_DO_NOT_USE)
            {
                return ReadQuantizedVector(scaleFactor);
            }

            return ReadPackedVectorLegacy(scaleFactor, maxBits);
        }

        /// <summary>
        /// see https://github.com/EpicGames/UnrealEngine/commit/db095ed4a590f2ae0f42a2fbf9e22678fbb1fb9f#diff-2641717376a3189c7cc27e9e28c26b72ce57d5d3efcf4a26b6beb0dd92eaaa0f
        /// </summary>
        private FVector ReadQuantizedVector(int scaleFactor)
        {
            var componentBitCountAndExtraInfo = ReadSerializedInt(1 << 7);
            var componentBitCount = (int)(componentBitCountAndExtraInfo & 63U);
            var extraInfo = componentBitCountAndExtraInfo >> 6;

            if (componentBitCount > 0U)
            {
                var X = ReadBitsToLong((int)componentBitCount);
                var Y = ReadBitsToLong((int)componentBitCount);
                var Z = ReadBitsToLong((int)componentBitCount);

                ulong signBit = 1UL << (int)(componentBitCount - 1);

                double fX = (long)(X ^ signBit) - (long)signBit;
                double fY = (long)(Y ^ signBit) - (long)signBit;
                double fZ = (long)(Z ^ signBit) - (long)signBit;

                if (extraInfo > 0)
                {
                    fX /= scaleFactor;
                    fY /= scaleFactor;
                    fZ /= scaleFactor;
                }

                return new FVector(fX, fY, fZ)
                {
                    Bits = componentBitCount,
                    ScaleFactor = scaleFactor,
                };
            }
            else if (extraInfo == 0)
            {
                double X = ReadSingle();
                double Y = ReadSingle();
                double Z = ReadSingle();

                return new FVector(X, Y, Z)
                {
                    Bits = 32,
                    ScaleFactor = scaleFactor,
                };
            }
            else
            {
                double X = ReadDouble();
                double Y = ReadDouble();
                double Z = ReadDouble();

                return new FVector(X, Y, Z)
                {
                    Bits = 64,
                    ScaleFactor = scaleFactor,
                };
            }
        }




        public FVector ReadPackedVectorLegacy(int scaleFactor, int maxBits)
        {
            var bits = ReadSerializedInt(maxBits);

            var bias = 1 << ((int)bits + 1);
            var max = 1 << ((int)bits + 2);

            var dx = ReadSerializedInt(max);
            var dy = ReadSerializedInt(max);
            var dz = ReadSerializedInt(max);

            if (IsError)
            {
                return new FVector(0, 0, 0);
            }

            var x = (float)(dx - bias) / scaleFactor;
            var y = (float)(dy - bias) / scaleFactor;
            var z = (float)(dz - bias) / scaleFactor;

            FVector vector = new FVector(x, y, z);

            return vector;
        }

        /// <summary>
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Private/Math/UnrealMath.cpp#L79
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Public/Math/Rotator.h#L654
        /// </summary>
        /// <returns></returns>
        public override FRotator ReadRotation()
        {
            float pitch = 0;
            float yaw = 0;
            float roll = 0;

            if (Bits[_position++]) // Pitch
            {
                pitch = ReadByte() * 360f / 256f;
            }

            if (Bits[_position++])
            {
                yaw = ReadByte() * 360f / 256f;
            }

            if (Bits[_position++])
            {
                roll = ReadByte() * 360f / 256f;
            }

            if (IsError)
            {
                return FRotator.Empty;
            }

            return new FRotator(pitch, yaw, roll);
        }

        /// <summary>
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Private/Math/UnrealMath.cpp#L79
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Public/Math/Rotator.h#L654
        /// </summary>
        /// <returns></returns>
        public override FRotator ReadRotationShort()
        {
            float pitch = 0;
            float yaw = 0;
            float roll = 0;

            if (Bits[_position++]) // Pitch
            {
                pitch = ReadUInt16() * 360 / 65536f;
            }

            if (Bits[_position++])
            {
                yaw = ReadUInt16() * 360 / 65536f;
            }

            if (Bits[_position++])
            {
                roll = ReadUInt16() * 360 / 65536f;
            }

            if (IsError)
            {
                return new FRotator(0, 0, 0);
            }

            return new FRotator(pitch, yaw, roll);
        }

        public override sbyte ReadSByte()
        {
            throw new NotImplementedException();
        }

        public override float ReadSingle()
        {
            Span<byte> value = stackalloc byte[4];

            ReadBytes(value);

            return BinaryPrimitives.ReadSingleLittleEndian(value);
        }

        public override double ReadDouble()
        {
            Span<byte> value = stackalloc byte[8];

            ReadBytes(value);

            return BinaryPrimitives.ReadDoubleLittleEndian(value);
        }

        public override (T, U)[] ReadTupleArray<T, U>(Func<T> func1, Func<U> func2)
        {
            throw new NotImplementedException();
        }

        public override ushort ReadUInt16()
        {
            Span<byte> value = stackalloc byte[2];

            ReadBytes(value);

            return BinaryPrimitives.ReadUInt16LittleEndian(value);
        }

        public override uint ReadUInt32()
        {
            Span<byte> value = stackalloc byte[4];

            ReadBytes(value);

            return BinaryPrimitives.ReadUInt32LittleEndian(value);
        }

        public override bool ReadUInt32AsBoolean()
        {
            throw new NotImplementedException();
        }

        public override T ReadUInt32AsEnum<T>()
        {
            throw new NotImplementedException();
        }

        public override ulong ReadUInt64()
        {
            Span<byte> value = stackalloc byte[8];

            ReadBytes(value);

            return BinaryPrimitives.ReadUInt64LittleEndian(value);
        }

        /// <summary>
        /// Sets <see cref="Position"/> within current BitArray.
        /// </summary>
        /// <param name="offset">The offset relative to the <paramref name="seekOrigin"/>.</param>
        /// <param name="seekOrigin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override void Seek(int offset, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            if (offset < 0 || offset > LastBit || (seekOrigin == SeekOrigin.Current && offset + _position > LastBit))
            {
                throw new ArgumentOutOfRangeException("Specified offset doesnt fit within the BitArray buffer");
            }

            _ = (seekOrigin switch
            {
                SeekOrigin.Begin => _position = offset,
                SeekOrigin.End => _position = LastBit - offset,
                SeekOrigin.Current => _position += offset,
                _ => _position = offset,
            });
        }

        public override void SkipBytes(uint byteCount)
        {
            SkipBytes((int)byteCount);
        }

        public override void SkipBytes(int byteCount)
        {
            Seek(byteCount * 8, SeekOrigin.Current);
        }

        public override void SkipBits(int numbits)
        {
            _position += numbits;

            if (numbits < 0 || _position > LastBit)
            {
                IsError = true;

                _position = LastBit;
            }
        }

        /// <summary>
        /// Save Position to <see cref="MarkPosition"/> so we can reset back to this point.
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Public/Serialization/BitReader.h#L228
        /// </summary>
        public override void Mark()
        {
            MarkPosition = _position;
        }

        /// <summary>
        /// Set Position back to <see cref="MarkPosition"/>
        /// see https://github.com/EpicGames/UnrealEngine/blob/70bc980c6361d9a7d23f6d23ffe322a2d6ef16fb/Engine/Source/Runtime/Core/Public/Serialization/BitReader.h#L228
        /// </summary>
        public override void Pop()
        {
            // TODO: pop makes it sound like a list...
            _position = MarkPosition;
        }

        /// <summary>
        /// Get number of bits left, including any bits after <see cref="LastBit"/>.
        /// </summary>
        /// <returns></returns>
        public override int GetBitsLeft()
        {
            return LastBit - _position;
        }

        /// <summary>
        /// Append bool array to this archive.
        /// </summary>
        /// <param name="data"></param>
        public override void AppendDataFromChecked(ReadOnlyMemory<bool> data)
        {
            AppendBits(data);
        }

        public override void Dispose()
        {
            DisposeBits();

            _positionQueues.Enqueue(_tempLastBit);
            _positionQueues.Enqueue(_tempPosition);
        }

        public void SetTempEnd(int totalBits, int index = 0)
        {
            uint setPosition = (uint)(_position + totalBits);

            if(setPosition > LastBit)
            {
                IsError = true;

                return;
            }

            _tempLastBit[index] = LastBit;
            _tempPosition[index] = (int)setPosition;
            LastBit = _position + totalBits;
        }

        public void RestoreTemp(int index = 0)
        {
            LastBit = _tempLastBit[index];
            _position = _tempPosition[index];

            /*
            _tempLastBit = 0;
            _tempPosition = 0;
            */

                IsError = false;
        }
    }
}
