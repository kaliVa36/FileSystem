using Constants;
using FS.Extensions;
using System.Text;
using static FS.data.FSFile;

namespace FS.data
{
    internal class FSFile : IFileOperations<FileMetadata>
    {
        public class FileMetadata
        {
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public int Size { get; set; }
            public int BlockIndexesSize { get; set; }
            public List<int> BlockIndexes { get; set; }

            public long NextAddress { get; set; }

            public FileMetadata(string fileName, int size, int blockAddressesSize, List<int> blockAddresses, long nextMetadataBlockIndex = -1)
            {
                FileName = fileName;
                // ContentType = contentType; // Will be added soon
                Size = size;
                BlockIndexes = blockAddresses;
                BlockIndexesSize = blockAddressesSize;
                NextAddress = nextMetadataBlockIndex;
            }
        }

        public static long? WriteData(FileMetadata data, string filePath, long startAddress, int size)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    //byte[] text = Encoding.Unicode.GetBytes(data.Size);
                    writer.Write(data.FileName.PadZeroes(size));
                    //writer.Write(BitConverter.GetBytes(text.Length));
                    //writer.Write(text);
                    return (stream.Position - startAddress);
                }
            }
        }

        public static void WriteMetadataBlocks(string containerPath, long startMetadataAddress, long startBitmapMetadataAddress, FileMetadata metadata, int fileNameMaxSize, FSBitmapManager bitmap, int metadataBlockSize)
        {
            // following FileMetadata structure
            int maxBlocks = (metadataBlockSize - fileNameMaxSize - sizeof(int) - sizeof(int) - sizeof(long)) / sizeof(int);

            if (metadata.BlockIndexes.Count <= maxBlocks)
            {
                WriteMetadata(containerPath, startMetadataAddress, startBitmapMetadataAddress, metadata, fileNameMaxSize, bitmap, metadataBlockSize);
                return;
            }

            FileMetadata metadataCopy = new FileMetadata(
                metadata.FileName,
                metadata.Size,
                metadata.BlockIndexesSize,
                new List<int>(metadata.BlockIndexes),
                metadata.NextAddress
            ); // metadata copy

            long address = startMetadataAddress;

            while (metadataCopy.BlockIndexes.Count > 0)
            {
                int metadataBlock = bitmap.FindFreeBlock(); // for finding the next address
                if (metadataBlock == -1)
                {
                    Console.WriteLine("No space");
                    return;
                }

                long nextAddress = startMetadataAddress + (metadataBlock * metadataBlockSize);

                List<int> currentBlockIndexes = new List<int>();
                for (int i = 0; i < maxBlocks && i < metadataCopy.BlockIndexes.Count; i++)
                {
                    currentBlockIndexes.Add(metadataCopy.BlockIndexes[i]);
                }

                long next = (metadataCopy.BlockIndexes.Count > maxBlocks) ? nextAddress : -1;

                FileMetadata blockMetadata = new FileMetadata(
                    metadataCopy.FileName,
                    metadataCopy.Size,
                    currentBlockIndexes.Count,
                    currentBlockIndexes,
                    next
                );

                Console.WriteLine("next address " + next);
                //Console.WriteLine($"Writing Metadata Block at {address}:");
                //Console.WriteLine($"Indexes: {string.Join(", ", currentBlockIndexes)}");
                //Console.WriteLine($"Next Address: {blockMetadata.NextAddress}");

                WriteMetadata(containerPath, address, startBitmapMetadataAddress, blockMetadata, fileNameMaxSize, bitmap, metadataBlockSize);

                // updating the block indexes and the address fopr the next block
                List<int> remainingBlockIndexes = new List<int>();
                for (int i = maxBlocks; i < metadataCopy.BlockIndexes.Count; i++)
                {
                    remainingBlockIndexes.Add(metadataCopy.BlockIndexes[i]);
                }
                metadataCopy.BlockIndexes = remainingBlockIndexes;
                address = nextAddress;
            }
        }
        public static void WriteMetadata(string containerPath, long startMetadataAddress, long startBitmapMetadataAddress, FileMetadata metadata, int fileNameMaxSize, FSBitmapManager bitmap, int metadataBlockSize)
        {
            int bitmapBlock = bitmap.FindFreeBlock();
            if (bitmapBlock == -1) throw new Exception(ErrorConstants.MetadataNoFreeSpace);

            long startAddress = startMetadataAddress + (bitmapBlock * metadataBlockSize); // calculating the start address of the current metadata by skipping to the index of the found block

            using (var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var writer = new BinaryWriter(stream))
                {
                    string title = metadata.FileName.PadZeroes(fileNameMaxSize);
                    writer.Write(Encoding.UTF8.GetBytes(title));
                    writer.Write(BitConverter.GetBytes(metadata.Size));
                    writer.Write(BitConverter.GetBytes(metadata.BlockIndexesSize));
                    for (int i = 0; i < metadata.BlockIndexesSize; i++)
                    {
                        writer.Write(BitConverter.GetBytes(metadata.BlockIndexes[i]));
                    }
                    Console.WriteLine("NextAddress writing " + metadata.NextAddress);
                    writer.Write(BitConverter.GetBytes(metadata.NextAddress));
                }

                bitmap.MarkAsUsed(bitmapBlock);
                bitmap.StoreBitmap(containerPath, startBitmapMetadataAddress, FileConstants.ReadBitmapBuffer);
            }
        }

        public static void ReadMetadataBlocks(string containerPath, long startMetadataAddress, string fileName, int metadataBlockSize, int maxFileTitleSize)
        {
            // initializing metadata for all blocks
            FileMetadata? metadata = null;
            List<int> metadataBlockIndexes = new List<int>();

            long address = startMetadataAddress; // it will hold the value of the next address of the file metadata blocks

            //while (address != -1)
            //{
                FileMetadata? currentMetadata = ReadData(containerPath, address, maxFileTitleSize);

                if (currentMetadata == null)
                {
                    Console.WriteLine("Something went wrong");
                    return;
                }

                Console.WriteLine($"Reading Metadata Block at {address}:");
                Console.WriteLine($"Indexes: {string.Join(", ", currentMetadata.BlockIndexes)}");
                Console.WriteLine($"Next Address: {currentMetadata.NextAddress}");


                if (metadata == null)
                {
                    // getting the name and size of the file
                    metadata = new FileMetadata(currentMetadata.FileName, currentMetadata.Size, 0, new List<int>(), -1);
                }

                for (int i = 0; i < currentMetadata.BlockIndexes.Count; i++)
                {
                    metadataBlockIndexes.Add(currentMetadata.BlockIndexes[i]);
                }

                address = currentMetadata.NextAddress;

                if (metadata != null)
                {
                    metadata.BlockIndexes = metadataBlockIndexes;
                    metadata.BlockIndexesSize = metadataBlockIndexes.Count;

                    Console.WriteLine($"File Name: {metadata.FileName}");
                    Console.WriteLine($"File Size: {metadata.Size} bytes");
                    Console.WriteLine($"Block Count: {metadata.BlockIndexesSize}");
                    Console.WriteLine($"Blocks: {string.Join(", ", metadata.BlockIndexes)}");
                    Console.WriteLine(metadata.NextAddress);
                }
            //}
        }

        public static void ReadMetadata(string containerPath, long startMetadataAddress, string fileName, int metadataBlockSize, int maxFileTitleSize)
        {
            int blockIndex = FindMetadataBlockIndex(containerPath, startMetadataAddress, metadataBlockSize, fileName, maxFileTitleSize);

            long blockAddress = startMetadataAddress + (blockIndex * metadataBlockSize);

            using (var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(blockAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    Console.WriteLine("FileMetadata: "); // for testing

                    byte[] buffer = new byte[maxFileTitleSize];

                    int bytesRead = reader.Read(buffer, 0, maxFileTitleSize);
                    string title = Encoding.UTF8.GetString(buffer).TrimZeroes();
                    Console.WriteLine(title);
                    int size = reader.ReadInt32();
                    Console.WriteLine(size);
                    int blockAddressesSize = reader.ReadInt32();
                    Console.WriteLine(blockAddressesSize);

                    List<int> blockIndexes = new List<int>();
                    for (int i = 0; i < blockAddressesSize; i++)
                    {
                        int index = reader.ReadInt32();
                        blockIndexes.Add(index);
                        Console.WriteLine(index);
                    }
                }
            }
        }


        public static void ReadFile(string containerPath, string filePath, string newFileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap)
        {
            List<int> indexes = new List<int>();
            int fileSize = 0;

            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fsMetadata.BlockSize];
                int bytesRead;

                using (var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write))
                {
                    while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        int block = fileBitmap.FindFreeBlock();
                        if (block == -1) throw new Exception(ErrorConstants.MetadataNoFreeSpace);

                        long blockAddress = fsMetadata.FirstFileAddress + (block * fsMetadata.BlockSize);

                        stream.Seek(blockAddress, SeekOrigin.Begin);
                        stream.Write(buffer, 0, bytesRead);

                        indexes.Add(block);

                        fileSize += bytesRead;
                        fileBitmap.MarkAsUsed(block);
                    }
                }
            }

            fileBitmap.StoreBitmap(containerPath, fsMetadata.FirstBitmapFileAddress, FileConstants.ReadFileBuffer);
            FileMetadata metadata = new FileMetadata(newFileName, fileSize, indexes.Count, indexes);
            WriteMetadata(containerPath, fsMetadata.FirstFileMetadataAddress, fsMetadata.FirstBitmapMetadataAddress, metadata, fsMetadata.MaxFileTitleSize, metadataBitmap, MetadataConstants.DefaultMetadataBlock);
        }

        public static int FindMetadataBlockIndex(string containerPath, long metadataStartAddress, int metadataBlockSize, string fileName, int maxFileTitleSize)
        {
            using (var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    for (int i = 0; ; i++) // i - block index
                    {
                        long block = metadataStartAddress + (i * metadataBlockSize);

                        stream.Seek(metadataStartAddress, SeekOrigin.Begin);

                        byte[] buffer = new byte[maxFileTitleSize];

                        int bytesRead = reader.Read(buffer, 0, maxFileTitleSize);
                        string title = Encoding.UTF8.GetString(buffer).TrimZeroes();

                        if (title == fileName) return i;
                    }
                }
            }
        }

        public static FileMetadata? ReadData(string filePath, long startAddress, int maxFileTitleSize)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    //Console.WriteLine("FileMetadata: "); // for testing

                    byte[] buffer = new byte[maxFileTitleSize];

                    int bytesRead = reader.Read(buffer, 0, maxFileTitleSize);
                    string title = Encoding.UTF8.GetString(buffer).TrimZeroes();
                    //Console.WriteLine(title);
                    int size = reader.ReadInt32();
                    //Console.WriteLine(size);
                    int blockAddressesSize = reader.ReadInt32();
                    //Console.WriteLine(blockAddressesSize);

                    List<int> blockIndexes = new List<int>();
                    Console.WriteLine(blockAddressesSize);
                    for (int i = 0; i < blockAddressesSize; i++)
                    {
                        int index = reader.ReadInt32();
                        blockIndexes.Add(index);
                        Console.WriteLine(index);
                    }

                    long nextAddress = (long)reader.ReadUInt64();

                    return new FileMetadata(title, size, blockAddressesSize, blockIndexes, nextAddress);
                }
            }
        }

        public static void DeleteData(string containerPath, long startAddress, int size, int bufferSize)
        {
            using (var stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);

                byte[] buffer = new byte[bufferSize];
                int bytesLeft = size;

                while (bytesLeft > 0)
                {
                    int bytesToWrite = (bytesLeft < bufferSize) ? bytesLeft : bufferSize;
                    stream.Write(buffer, 0, bytesToWrite);

                    bytesLeft -= bytesToWrite;
                }
            }
        }
    }
}
