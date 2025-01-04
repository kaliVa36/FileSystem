using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

/*
in the MVP version, the bitmap will work with constant values 
for blockSize and totalBlocks. In the future it will use
only blockSize.
*/

namespace FS
{
    internal class FSBitmapManager
    {
        private byte[] Bitmap;
        private readonly int TotalBlocks;

        public FSBitmapManager(int totalBlocks)
        { 
            TotalBlocks = totalBlocks;

            // (a + b - 1) / b; 1 byte = 8 bits
            Bitmap = new byte[((TotalBlocks + 7) / 8)];
        }

        public void LoadBitmap(string containerPath, long startAddress, int blockSize)
        {
            using (var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    int bytesLeft = Bitmap.Length; // Total bytes to read
                    int offset = 0;

                    while (bytesLeft > 0)
                    {
                        // If less bytes than the block size are left - reading the exact number of left bytes
                        int bytesToRead = (bytesLeft < blockSize) ? bytesLeft : blockSize;

                        // Read the block into the bitmap array
                        reader.Read(Bitmap, offset, bytesToRead);

                        // Update remaining bytes and offset
                        bytesLeft -= bytesToRead;
                        offset += bytesToRead;
                    }
                }
            }
        }



        // For testing
        public void PrintBitmap()
        {
            Console.WriteLine("Bitmap State:");
            for (int i = 0; i < Bitmap.Length; i++)
            {
                string binaryRepresentation = Convert.ToString(Bitmap[i], 2).PadLeft(8, '0');
                Console.WriteLine($"Byte {i}: {binaryRepresentation}");
            }
        }

        public void StoreBitmap(string containerPath, long startAddress, int blockSize)
        {
            using (var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var writer = new BinaryWriter(stream))
                {
                    int bytesLeft = Bitmap.Length; // Total bytes to write
                    int offset = 0;

                    while (bytesLeft > 0)
                    {
                        // If less bytes than the chunk size are left - writing the exact number of left bytes
                        int bytesToWrite = (bytesLeft < blockSize) ? bytesLeft : blockSize;

                        // Write the chunk into the container
                        writer.Write(Bitmap, offset, bytesToWrite);

                        // Update remaining bytes and offset
                        bytesLeft -= bytesToWrite;
                        offset += bytesToWrite;
                    }
                }
            }

            PrintBitmap();
        }

        public void InitializeBitmap(string containerPath, long startAddress, int blockSize)
        {
            for (int i = 0; i < Bitmap.Length; i++)
            {
                Bitmap[i] = 0;
            }

            StoreBitmap(containerPath, startAddress, blockSize);
        }

        public void MarkAsUsed(int blockIndex)
        { 
            if (blockIndex < 0 || blockIndex > TotalBlocks) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            int byteIndex = blockIndex / 8; // 1 byte has 8 bits
            int bitIndex = blockIndex % 8; // finds the bit which responds for this index
            Bitmap[byteIndex] |= (byte)(1 << bitIndex); // makes a binary on a specific position and adds it to the bitmap without changing the rest of the bits
        }

        public void MarkAsUnused(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex > TotalBlocks) throw new ArgumentOutOfRangeException(nameof(blockIndex));

            int byteIndex = blockIndex / 8; // 1 byte has 8 bits
            int bitIndex = blockIndex % 8; // finds the bit which responds for this index
            Bitmap[byteIndex] &= (byte)~(1 << bitIndex); // makes a binary at a specific index, reverses it and adds it using AND. This way all 1s stay the same, only the bit which is 0 changes
        }

        public int FindFreeBlock()
        {
            for (int i = 0; i < TotalBlocks; i++)
            {
                int byteIndex = i / 8; // 1 byte has 8 bits
                int bitIndex = i % 8; // finds the bit which responds for this index
                if ((Bitmap[byteIndex] & (1 << bitIndex)) == 0)  return i; // Return the index of the first free block
            }
            return -1; // No free blocks available
        }
    }
}
