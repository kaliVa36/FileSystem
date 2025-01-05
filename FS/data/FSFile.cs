using Constants;
using FS.Extensions;
using System.Drawing;
using System.Net;
using System.Text;
using static FS.data.FSFile;

namespace FS.data
{
    internal class FSFile : IFileOperations<FileMetadata>
    {
        public class FileMetadata
        {
            public string FileName { get; set; }
            public bool IsDirectory { get; set; }
            public List<long> ChildrenAddresses { get; set; } = new List<long>();
            public long ParentAddress { get; set; }
            // public string ContentType { get; set; }
            public int Size { get; set; }
            public int BlockIndexesSize { get; set; }
            public List<int> BlockIndexes { get; set; }

            public int ChildrenSize { get; set; }
            public List<long> ChildrenAddress { get; set; } = new List<long>();
            // Adding some default values so to be used by both directories and files
            public FileMetadata(string fileName, bool isDirectory, long parentAddress, int size = 0, int blockAddressesSize = 0, List<int>? blockAddresses = null, int childrenSize = 0, List<long>? childrenAddress = null)
            {
                FileName = fileName;
                // ContentType = contentType; // Will be added soon
                IsDirectory = isDirectory;
                ParentAddress = parentAddress;
                Size = size;
                BlockIndexes = blockAddresses ?? new List<int>();
                BlockIndexesSize = blockAddressesSize;
                ChildrenSize = childrenSize;
                ChildrenAddresses = childrenAddress ?? new List<long>();
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

        public static void WriteMetadata(string containerPath, long startMetadataAddress, long startBitmapMetadataAddress, FileMetadata metadata, int fileNameMaxSize, FSBitmapManager bitmap, int metadataBlockSize, long currentDirectory = 0, bool addChild = false)
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
                    Console.WriteLine(title);
                    writer.Write(BitConverter.GetBytes(metadata.IsDirectory));
                    Console.WriteLine("write: " + metadata.IsDirectory);
                    writer.Write(BitConverter.GetBytes(metadata.ParentAddress));
                    Console.WriteLine(metadata.ParentAddress);
                    if (metadata.IsDirectory == true)
                    {
                        writer.Write(BitConverter.GetBytes(metadata.ChildrenSize));
                        Console.WriteLine(metadata.ChildrenSize);
                        for (int i = 0; i < metadata.ChildrenSize; i++)
                        {
                            Console.WriteLine(metadata.ChildrenAddresses[i]);
                            writer.Write(BitConverter.GetBytes(metadata.ChildrenAddresses[i]));
                        }
                    } else {
                        writer.Write(BitConverter.GetBytes(metadata.Size));
                        writer.Write(BitConverter.GetBytes(metadata.BlockIndexesSize));
                        for (int i = 0; i < metadata.BlockIndexesSize; i++)
                        {
                            writer.Write(BitConverter.GetBytes(metadata.BlockIndexes[i]));
                        }
                    }
                }

                bitmap.MarkAsUsed(bitmapBlock);
                bitmap.StoreBitmap(containerPath, startBitmapMetadataAddress, FileConstants.ReadBitmapBuffer);
                //bitmap.PrintBitmap();

                //if (addChild)
                //{
                //    FSFile.AddChild(startAddress, 256, fileNameMaxSize, containerPath, currentDirectory);
                //}
            }
            ReadData(containerPath, startAddress, fileNameMaxSize);
        }

        public static void ReadFile(string containerPath, string filePath, string newFileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap, long currentDirectory)
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
            FileMetadata metadata = new FileMetadata(newFileName, false, currentDirectory, fileSize, indexes.Count, indexes);
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
                    Console.WriteLine("FileMetadata: "); // for testing

                    byte[] buffer = new byte[maxFileTitleSize];

                    int bytesRead = reader.Read(buffer, 0, maxFileTitleSize);
                    string title = Encoding.UTF8.GetString(buffer).TrimZeroes();
                    Console.WriteLine(title);
                    bool isDirectory = reader.ReadBoolean();
                    // Console.WriteLine("read " + isDirectory);
                    Console.WriteLine(isDirectory);
                    long parentAddress = reader.ReadInt64();
                    Console.WriteLine(parentAddress);
                    if (isDirectory)
                    {
                        int childrenSize = reader.ReadInt32();
                        Console.WriteLine(childrenSize);

                        List<long> childrenAddress = new List<long>();
                        for (int i = 0; i < childrenSize; i++)
                        {
                            long index = reader.ReadInt64();
                            childrenAddress.Add(index);
                            Console.WriteLine(index);
                        }

                        return new FileMetadata(title, isDirectory, parentAddress, childrenSize: childrenSize, childrenAddress: childrenAddress);
                    }
                    else
                    {
                        int size = reader.ReadInt32();
                        //Console.WriteLine(size);
                        int blockAddressesSize = reader.ReadInt32();
                        //Console.WriteLine(blockAddressesSize);

                        List<int> blockIndexes = new List<int>();
                        for (int i = 0; i < blockAddressesSize; i++)
                        {
                            int index = reader.ReadInt32();
                            blockIndexes.Add(index);
                            //Console.WriteLine(index);
                        }

                        return new FileMetadata(title, isDirectory, parentAddress, size, blockAddressesSize, blockIndexes);
                    }
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

        public static bool CanAddChild(int metadataBlockSize, int fileTitleMaxSize, int childrenSize)
        {
            // Following the directory metadata structure
            int usedSize = fileTitleMaxSize + sizeof(bool) + sizeof(int) + (childrenSize * sizeof(long));

            int remainingSize = metadataBlockSize - usedSize;

            return remainingSize >= sizeof(long);
        }

        public static void AddChild(long childAddress, int metadataBlockSize, int maxTitleSize, string containerPath, long currentDirectory)
        {
            Console.WriteLine(currentDirectory);
            FileMetadata? metadata = ReadData(containerPath, currentDirectory, maxTitleSize); //

            if (metadata == null)
            {
                Console.WriteLine("Something went wrong"); // Error constants
                return;
            }

            if (!metadata.IsDirectory)
            {
                Console.WriteLine($"{metadata.FileName} is not a directory");
                return;
            }

            if (!CanAddChild(metadataBlockSize, maxTitleSize, metadata.ChildrenSize))
            {
                Console.WriteLine("No space left in this directory");
                return;
            }

            metadata.ChildrenAddress.Add(childAddress);

            using (FileStream stream = new FileStream(containerPath, FileMode.Open, FileAccess.Write))
            {
                // adding the size of the title and isDirectory bool
                long address = currentDirectory + maxTitleSize + sizeof(bool);
                stream.Seek(address, SeekOrigin.Begin);
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(metadata.ChildrenAddresses.Count);
                    for (int i = 0; i < metadata.ChildrenAddresses.Count; i++)
                    {
                        Console.WriteLine(metadata.ChildrenAddress[i]);
                        writer.Write(metadata.ChildrenAddress[i]);
                    }
                }
            }
        }
    }
}
