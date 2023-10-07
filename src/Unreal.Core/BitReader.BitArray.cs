using OozSharp.MemoryPool;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

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

        private void CreateBitArray(byte* ptr, int byteCount, int totalBits)
        {
            _owner = PinnedMemoryPool<bool>.Shared.Rent(totalBits);
            _items = _owner.PinnedMemory.Memory;
            LastBit = totalBits;
            Bits = (bool*)_owner.PinnedMemory.Pointer;

            if (Bmi2.X64.IsSupported && UseIntrinsics)
            {
                var bb = (ulong*)_owner.PinnedMemory.Pointer;

                for (int i = 0; i < byteCount; i++)
                {
                    var a = *(bb + i);

                    *(bb + i) = Bmi2.X64.ParallelBitDeposit(*(ptr + i), 0x0101010101010101UL);

                    var ba = *(bb + i);
                }

                Bits = (bool*)bb;
            }
            else
            {
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
