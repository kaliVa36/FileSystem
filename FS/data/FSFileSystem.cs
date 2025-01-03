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
        public long FirstAvaialbleAddress { get; set; }
        public int BlockSize { get; set; }
        public int MaxFileTitleSize { get; set; }

        public FileSystemMetadata(long firstFileMetadataAddress, long firstBitmapMetadataAddress,int bitmapMetadataSize, long firstFileAddress, long firstBitmapFileAddress, int bitmapFileSize, int blockSize, long firstAvailableAddress, int maxFileTitleSize)
        {
            FirstFileMetadataAddress = firstFileMetadataAddress;
            FirstBitmapMetadataAddress = firstBitmapMetadataAddress;
            BitmapMetadataSize = bitmapMetadataSize;
            FirstFileAddress = firstFileAddress;
            FirstBitmapFileAddress = firstBitmapFileAddress;
            BitmapFileSize = bitmapFileSize;
            FirstAvaialbleAddress = firstAvailableAddress;
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
                    bitmapMetadataSize: MetadataConstants.DefaultFileSystemMetadataSize,
                    firstFileAddress: MetadataConstants.DefaultFileSystemMetadataSize + MetadataConstants.DefaultBitmapMetadataSize + MetadataConstants.DefaultMetadataStorage + MetadataConstants.DefaultBitmapFileSize,
                    firstBitmapFileAddress: MetadataConstants.DefaultFileSystemMetadataSize + MetadataConstants.DefaultBitmapMetadataSize + MetadataConstants.DefaultMetadataStorage,
                    bitmapFileSize: MetadataConstants.DefaultFileSystemMetadataSize + MetadataConstants.DefaultBitmapMetadataSize + MetadataConstants.DefaultMetadataStorage,
                    blockSize: int.Parse(chunkSize),
                    firstAvailableAddress: MetadataConstants.DefaultMetadataStorage, // this wont be working
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
                    writer.Write(BitConverter.GetBytes(data.BlockSize));
                    writer.Write(BitConverter.GetBytes(data.FirstMetadataAddress));
                    writer.Write(BitConverter.GetBytes(data.FirstFileAddress));
                    writer.Write(BitConverter.GetBytes(data.MaxFileSize));
                    writer.Write(BitConverter.GetBytes(data.MaxFileTitleSize));

                    // Getting the file system's metadata size
                    long sizeOfMetadata = stream.Position;
                    // Skipping block size to write the new size into first address
                    writer.Seek(sizeof(int), SeekOrigin.Begin);
                    writer.Write(BitConverter.GetBytes(sizeOfMetadata));

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
                    int blockSize = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
                    // Console.WriteLine("blockSize: " + blockSize);
                    long firstAddress = BitConverter.ToInt64(reader.ReadBytes(sizeof(long)), 0);
                    // Console.WriteLine("firstAddress: " + firstAddress);
                    long firstAvailableAddress = BitConverter.ToInt64(reader.ReadBytes(sizeof(long)), 0);
                    // Console.WriteLine("firstAvailableAddress: " + firstAvailableAddress);
                    long maxFileSize = reader.ReadInt64();
                    // Console.WriteLine("MaxFileSize: " + maxFileSize);
                    int maxFileTitle = reader.ReadInt32();
                    // Console.WriteLine($"maxFileTitle: {maxFileTitle}");

                    return new FileSystemMetadata(
                        blockSize: blockSize,
                        firstAddress: firstAddress,
                        firstAvailableAddress: firstAvailableAddress,
                        maxFileSize: maxFileSize,
                        maxFileTitleSize: maxFileTitle
                    );
                }
            }
        }
        public static void ChangeFirstAvailableAddress(long newAddress, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Seek(sizeof(int) + sizeof(long), SeekOrigin.Begin);
                    writer.Write(BitConverter.GetBytes(newAddress));
                }
            }
        }
    }
}
