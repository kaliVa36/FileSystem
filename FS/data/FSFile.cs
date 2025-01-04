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
            public int BlockAddressesSize { get; set; }
            public List<long> BlockAddresses { get; set; }

            public FileMetadata(string fileName, int size, int blockAddressesSize, List<long> blockAddresses)
            {
                FileName = fileName;
                // ContentType = contentType; // Will be added soon
                Size = size;
                BlockAddresses = blockAddresses;
                BlockAddressesSize = blockAddressesSize;
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
                    writer.Write(BitConverter.GetBytes(metadata.BlockAddressesSize));
                    for (int i = 0; i < metadata.BlockAddressesSize; i++)
                    {
                        writer.Write(BitConverter.GetBytes(metadata.BlockAddresses[i]));
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

                    List<long> blockAddresses = new List<long>();
                    for (int i = 0; i < blockAddressesSize; i++)
                    {
                        long address = reader.ReadInt64(); 
                        blockAddresses.Add(address);
                        Console.WriteLine(address);
                    }
                }
            }
        }


        public static void ReadFile(string containerPath, string filePath, string newFileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap)
        {
            List<long> address = new List<long>();
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

                        address.Add(block);

                        fileSize += bytesRead;
                        fileBitmap.MarkAsUsed(block);
                    }
                }
            }

            fileBitmap.StoreBitmap(containerPath, fsMetadata.FirstBitmapFileAddress, FileConstants.ReadFileBuffer);
            FileMetadata metadata = new FileMetadata(newFileName, fileSize, address.Count, address);
            WriteMetadata(containerPath, fsMetadata.FirstFileMetadataAddress, fsMetadata.FirstBitmapMetadataAddress, metadata, fsMetadata.MaxFileTitleSize, metadataBitmap, MetadataConstants.DefaultMetadataBlock);
        }

        //    int bufferSize = FileConstants.ReadFileBuffer; // Example buffer size
        //    long lastStreamPosition = 0;
        //    long updatedSizePosition = 0;
        //    int sizeOfFile = 0;
        //    using (FileStream file = new FileStream(filePath, FileMode.Open))
        //    {
        //        byte[] buffer = new byte[bufferSize];
        //        int bytesRead;

        //        using (FileStream stream = new FileStream(containerPath, FileMode.Append, FileAccess.Write))
        //        {
        //            using (var writer = new BinaryWriter(stream))
        //            {
        //                stream.Seek(firstAvailbleAddress, SeekOrigin.Begin);
        //                byte[] title = Encoding.UTF8.GetBytes(newFileName.PadZeroes(maxFileTitleSize));
        //                stream.Write(title);
        //                updatedSizePosition = stream.Position;
        //                stream.Write(BitConverter.GetBytes(0L)); // will change later
        //                while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
        //                {
        //                    writer.Write(buffer, 0, bytesRead);
        //                    sizeOfFile += bytesRead;
        //                }
        //                lastStreamPosition = stream.Position;
        //            }
        //        }
        //    }

        //    using (FileStream stream = new FileStream(containerPath, FileMode.Open))
        //    {
        //        if (updatedSizePosition != 0 && sizeOfFile != 0)
        //        {
        //            stream.Seek(updatedSizePosition, SeekOrigin.Begin);
        //            using (var writer = new BinaryWriter(stream))
        //            {
        //                writer.Write(BitConverter.GetBytes(sizeOfFile));
        //            }
        //        }
        //    }

        //    if (lastStreamPosition != 0)
        //    {
        //        FSFileSystem.ChangeFirstAvailableAddress(lastStreamPosition, containerPath);
        //    }
        //}

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

        public static void WriteFileFromContainer(string conteinerPath, string filePath, long address, long size)
        {
            int bufferSize = FileConstants.ReadFileBuffer;

            using (FileStream stream = new FileStream(conteinerPath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[bufferSize];
                long totalBytesWritten = 0;

                using (FileStream file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(file))
                    {
                        stream.Seek(address, SeekOrigin.Begin);
                        while (totalBytesWritten < size)
                        {
                            int remainingSize = (int)(size - totalBytesWritten);
                            int bytesToRead = (remainingSize < bufferSize) ? remainingSize : bufferSize;

                            int bytesRead = stream.Read(buffer, 0, bytesToRead);
                            if (bytesRead == 0) break;

                            string decodedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            writer.Write(decodedData);
                            writer.Flush();

                            totalBytesWritten += bytesRead;
                        }
                    }
                }
            }
        }

        public static FileMetadata? ReadData(string filePath, long startAddress, int maxFileTitleSize)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
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

                    List<long> blockAddresses = new List<long>();
                    for (int i = 0; i < blockAddressesSize; i++)
                    {
                        long address = reader.ReadInt64();
                        blockAddresses.Add(address);
                        //Console.WriteLine(address);
                    }

                    return new FileMetadata(title, size, blockAddressesSize, blockAddresses);
                }
            }
        }
    }
}
