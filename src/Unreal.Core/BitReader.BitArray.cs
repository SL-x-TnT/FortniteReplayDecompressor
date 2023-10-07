using OozSharp.MemoryPool;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Unreal.Core
{
    public unsafe partial class BitReader
    {
        public static bool UseIntrinsics = true;

        protected bool* Bits;

        protected ReadOnlyMemory<bool> _items { get; set; }
        private IPinnedMemoryOwner<bool> _owner;

        public void SetBits(byte* ptr, int byteCount, int bitCount)
        {
            CreateBitArray(ptr, byteCount, bitCount);
            _position = 0;
        }

        public void DisposeBits()
        {
            _owner?.Dispose();
            _owner = null;
            _items = null;
            Bits = null;
        }

        private static Vector256<byte> _avx2Shuffle = Vector256.Create(0x0000000000000000, 0x0101010101010101, 0x0202020202020202, 0x0303030303030303).AsByte();
        private static Vector256<byte> _avx2BitMask = Vector256.Create(0x8040201008040201).AsByte();
        private static Vector256<byte> _avx2One = Vector256.Create((byte)1);

#if NET8_0_OR_GREATER
        private static Vector512<byte> _avx512Shuffle = Vector512.Create(0x0000000000000000, 0x0101010101010101, 0x0202020202020202, 0x0303030303030303, 0x0404040404040404, 0x0505050505050505, 0x0606060606060606, 0x0707070707070707).AsByte();
        private static Vector512<byte> _avx512BitMask = Vector512.Create(0x8040201008040201).AsByte();
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateBitArray(byte* ptr, int byteCount, int totalBits)
        {
            _owner = PinnedMemoryPool<bool>.Shared.Rent(byteCount * 8);
            _items = _owner.PinnedMemory.Memory;
            LastBit = totalBits;
            Bits = (bool*)_owner.PinnedMemory.Pointer;

            if (UseIntrinsics)
            {
#if NET8_0_OR_GREATER
                if (Avx512BW.IsSupported)
                {
                    //Console.WriteLine("a");


                    Span<byte> rb = new Span<byte>(_owner.PinnedMemory.Pointer, byteCount * 8);
                    Span<byte> db = new Span<byte>(ptr, byteCount);

                    Span<ulong> d = MemoryMarshal.Cast<byte, ulong>(db);
                    Span<ulong> r = MemoryMarshal.Cast<byte, ulong>(rb);

                    var initalRead = totalBits / 8 * 8;

                    for (int i = 0; i < d.Length; i++)
                    {
                        var vmask = Vector512.Create(d[i]).AsByte();
                        vmask = Avx512BW.Shuffle(vmask, _avx512Shuffle);
                        vmask = Avx512BW.And(vmask, _avx512BitMask);
                        vmask = Avx512BW.Min(vmask, Vector512<byte>.One);

                        vmask.CopyTo(rb.Slice(i * 64));
                    }

                    //Can be slow on AMD, but it's a max of 7 iterations
                    for (int i = d.Length * 8; i < db.Length; i++)
                    {
                        r[i] = Bmi2.X64.ParallelBitDeposit(db[i], 0x0101010101010101UL);
                    }
                }
                else
#endif
                if (Avx2.IsSupported)
                {
                    Span<byte> rb = new Span<byte>(_owner.PinnedMemory.Pointer, byteCount * 8);
                    Span<byte> db = new Span<byte>(ptr, byteCount);

                    Span<uint> d = MemoryMarshal.Cast<byte, uint>(db);
                    Span<ulong> r = MemoryMarshal.Cast<byte, ulong>(rb);

                    for (int i = 0; i < d.Length; i++)
                    {
                        var vmask = Vector256.Create(d[i]).AsByte();
                        vmask = Avx2.Shuffle(vmask, _avx2Shuffle);
                        vmask = Avx2.And(vmask, _avx2BitMask);
                        vmask = Avx2.Min(vmask, _avx2One);

#if NET7_0_OR_GREATER
                        vmask.CopyTo(rb.Slice(i * 32));

#else
                        Avx2.Store((byte*)(Bits + i * 32), vmask);
#endif
                    }

                    for (int i = d.Length * 4; i < db.Length; i++)
                    {
                        r[i] = Bmi2.X64.ParallelBitDeposit(db[i], 0x0101010101010101UL);
                    }
                }
                else
                {
                    var bb = (ulong*)_owner.PinnedMemory.Pointer;

                    for (int i = 0; i < byteCount; i++)
                    {
                        *(bb + i) = Bmi2.X64.ParallelBitDeposit(*(ptr + i), 0x0101010101010101UL);

                        var ba = *(bb + i);
                    }

                    Bits = (bool*)bb;
                }
            }
            else
            {
                //Should changed this to 
                /*
                    // https://stackoverflow.com/questions/8461126/how-to-create-a-byte-out-of-8-bool-values-and-vice-versa/51750902#51750902
                    void unpack8bools(uint8_t b, bool* a)
                    {
                    // on little-endian,  a[0] = (b>>7) & 1  like printing order
                    auto MAGIC = 0x8040201008040201ULL;  // for opposite order, byte-reverse this
                    auto MASK  = 0x8080808080808080ULL;
                    uint64_t t = ((MAGIC*b) & MASK) >> 7;
                    memcpy(a, &t, sizeof t);    // store 8 bytes without UB
                    }
                */

                for (int i = 0; i < byteCount; i++)
                {
                    int offset = i * 8;
                    byte deref = *(ptr + i);

                    *(Bits + offset) = (deref & 0x01) == 0x01;
                    *(Bits + offset + 1) = (deref & 0x02) == 0x02;
                    *(Bits + offset + 2) = (deref & 0x04) == 0x04;
                    *(Bits + offset + 3) = (deref & 0x08) == 0x08;
                    *(Bits + offset + 4) = (deref & 0x10) == 0x10;
                    *(Bits + offset + 5) = (deref & 0x20) == 0x20;
                    *(Bits + offset + 6) = (deref & 0x40) == 0x40;
                    *(Bits + offset + 7) = (deref & 0x80) == 0x80;
                }
            }
        }


private void AppendBits(ReadOnlyMemory<bool> after)
{
IPinnedMemoryOwner<bool> newOwner = PinnedMemoryPool<bool>.Shared.Rent(after.Length + LastBit);
Memory<bool> newMemory = newOwner.PinnedMemory.Memory;
int oldLength = LastBit;

//Copy old array
_items.CopyTo(newMemory);

DisposeBits(); //Get rid of old

_items = newMemory;

_owner = newOwner;
Bits = (bool*)_owner.PinnedMemory.Pointer;

MemoryHandle afterPin = after.Pin();

Buffer.MemoryCopy(afterPin.Pointer, Bits + oldLength, after.Length, after.Length);

afterPin.Dispose();

LastBit = after.Length + LastBit;
}

protected byte GetAsByte(int index)
{
return (*(byte*)(Bits + index));
}
}
}
