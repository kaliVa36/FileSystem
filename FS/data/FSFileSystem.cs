using Constants;

namespace FS.data
{
    public class FileSystemMetadata
    {
        // File metadata
        public long FirstFileMetadataAddress { get; set; }
        public long FirstBitmapMetadataAddress { get; set; }
        public int BitmapMetadataSize { get; set; }
        // File
        public long FirstFileAddress { get; set; }
        public long FirstBitmapFileAddress { get; set; }
        public int BitmapFileSize { get; set; }
        public int BlockSize { get; set; }
        public int MaxFileTitleSize { get; set; }

        public FileSystemMetadata(long firstFileMetadataAddress, long firstBitmapMetadataAddress,int bitmapMetadataSize, long firstFileAddress, long firstBitmapFileAddress, int bitmapFileSize, int blockSize, int maxFileTitleSize)
        {
            FirstFileMetadataAddress = firstFileMetadataAddress;
            FirstBitmapMetadataAddress = firstBitmapMetadataAddress;
            BitmapMetadataSize = bitmapMetadataSize;
            FirstFileAddress = firstFileAddress;
            FirstBitmapFileAddress = firstBitmapFileAddress;
            BitmapFileSize = bitmapFileSize;
            BlockSize = blockSize;
            MaxFileTitleSize = maxFileTitleSize;
        }
    }

    /*
    In the MVP of the file system (FS), 
    the data sequence will be the following:
    1. Metadata of the File System
    2. Bitmap of the file metadata
    3. File metadata
    4. Bitmap of the files
    5. Files
    Each type of data will have a fixed size, 
    with plans for future implementation to develop dynamic size allocation.
    */

    internal class FSFileSystem : IFileOperations<FileSystemMetadata>
    {
        public static FileSystemMetadata InitializeFileSystem(string path)
        {
            // Creating the .bin file
            // Adding chunk size
            Console.WriteLine("Enter chunk size for working with files of this file system:");
            string? chunkSize = Console.ReadLine();
            while (chunkSize == null || !int.TryParse(chunkSize, out int parsedSize) || parsedSize <= 0)
            {
                Console.WriteLine("The chunk size should be a positive integer:");
                chunkSize = Console.ReadLine();
            }

            string fileNameCharactersMaxSizeInput = inputComponent(
                message: $"Do you want to set custom file name characters max size? (default is {MetadataConstants.DefaultFileSystemFileNameCharacters}) y/n",
                valueMessage: "Type the file max size:",
                errorMesassge: "Answer should be positive integer."
            ) ?? MetadataConstants.DefaultFileSystemFileNameCharacters.ToString();

            // Writing metadata into the container
            // For constants deeper understanding read the description above the function
            FileSystemMetadata metadata = new FileSystemMetadata(
                    firstFileMetadataAddress: MetadataConstants.DefaultFileSystemMetadataSize + MetadataConstants.DefaultBitmapMetadataSize,
                    firstBitmapMetadataAddress: MetadataConstants.DefaultFileSystemMetadataSize,
                    bitmapMetadataSize: MetadataConstants.DefaultBitmapMetadataSize,
                    firstFileAddress: MetadataConstants.DefaultFileSystemMetadataSize + MetadataConstants.DefaultBitmapMetadataSize + MetadataConstants.DefaultMetadataStorage + MetadataConstants.DefaultBitmapFileSize,
                    firstBitmapFileAddress: MetadataConstants.DefaultFileSystemMetadataSize + MetadataConstants.DefaultBitmapMetadataSize + MetadataConstants.DefaultMetadataStorage,
                    bitmapFileSize: MetadataConstants.DefaultBitmapFileSize,
                    blockSize: int.Parse(chunkSize),
                    maxFileTitleSize: int.Parse(fileNameCharactersMaxSizeInput)
            );
            WriteData(metadata, path, 0);
            return ReadData(path, 0);
        }

        public static string? inputComponent(string message, string valueMessage, string errorMesassge)
        {
            Console.WriteLine(message);
            string? answer = Console.ReadLine();
            while (answer != "y" && answer != "n")
            {
                Console.WriteLine("Type y or n to continue:");
                answer = Console.ReadLine();
            }
            if (answer == "y")
            {
                Console.WriteLine(valueMessage);
                string? userInput = Console.ReadLine();
                while (userInput == null || !int.TryParse(userInput, out int parsedSize) && parsedSize <= 0)
                {
                    Console.WriteLine(errorMesassge);
                    userInput = Console.ReadLine();
                }
                return userInput;
            }
            else return null;
        }

        public static long? WriteData(FileSystemMetadata data, string filePath, long startAddress, int size = 0)
        {
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(BitConverter.GetBytes(data.FirstFileMetadataAddress));
                    writer.Write(BitConverter.GetBytes(data.FirstBitmapMetadataAddress));
                    writer.Write(BitConverter.GetBytes(data.BitmapMetadataSize));
                    writer.Write(BitConverter.GetBytes(data.FirstFileAddress));
                    writer.Write(BitConverter.GetBytes(data.FirstBitmapFileAddress));
                    writer.Write(BitConverter.GetBytes(data.BitmapFileSize));
                    writer.Write(BitConverter.GetBytes(data.BlockSize));
                    writer.Write(BitConverter.GetBytes(data.MaxFileTitleSize));

                    return null;
                }
            }
        }

        public static FileSystemMetadata ReadData(string filePath, long startAddress, int maxFileTitleSize = 0)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    // Print for testing
                    //Console.WriteLine("File System metadata:");
                    long firstFileMetadataAddress = reader.ReadInt64();
                    //Console.WriteLine(firstFileMetadataAddress);
                    long firstBitmapMetadataAddress = reader.ReadInt64();
                    //Console.WriteLine(firstBitmapMetadataAddress);
                    int bitmapMetadataSize = reader.ReadInt32();
                    //Console.WriteLine(bitmapMetadataSize);
                    long firstFileAddress = reader.ReadInt64();
                    //Console.WriteLine(firstFileAddress);
                    long firstBitmapFileAddress = reader.ReadInt64();
                    //Console.WriteLine(firstBitmapFileAddress);
                    int bitmapFileSize = reader.ReadInt32();
                    //Console.WriteLine(bitmapFileSize);
                    int blockSize = reader.ReadInt32();
                    //Console.WriteLine(blockSize);
                    int maxFileTitleSizeRead = reader.ReadInt32();
                    //Console.WriteLine(maxFileTitleSizeRead);

                    return new FileSystemMetadata(
                        firstFileMetadataAddress: firstFileMetadataAddress,
                        firstBitmapMetadataAddress: firstBitmapMetadataAddress,
                        bitmapMetadataSize: bitmapMetadataSize,
                        firstFileAddress: firstFileAddress,
                        firstBitmapFileAddress: firstBitmapFileAddress,
                        bitmapFileSize: bitmapFileSize,
                        blockSize: blockSize,
                        maxFileTitleSize: maxFileTitleSizeRead
                    );
                }
            }
        }
    }
}
