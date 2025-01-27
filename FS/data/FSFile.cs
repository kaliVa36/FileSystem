﻿using Constants;
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

            public FileMetadata(string fileName, int size, int blockAddressesSize, List<int> blockAddresses)
            {
                FileName = fileName;
                // ContentType = contentType; // Will be added soon
                Size = size;
                BlockIndexes = blockAddresses;
                BlockIndexesSize = blockAddressesSize;
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
                }

                bitmap.MarkAsUsed(bitmapBlock);
                bitmap.StoreBitmap(containerPath, startBitmapMetadataAddress, FileConstants.ReadBitmapBuffer);
            }
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
                    for (int i = 0; i < blockAddressesSize; i++)
                    {
                        int index = reader.ReadInt32();
                        blockIndexes.Add(index);
                        //Console.WriteLine(address);
                    }

                    return new FileMetadata(title, size, blockAddressesSize, blockIndexes);
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
