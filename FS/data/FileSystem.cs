using Constants;
using FS;

namespace FileSystemData
{
    public class FileSystemMetadata
    {
        public int BlockSize { get; set; }
        public int MaxFileSize { get; set; }
        public long FirstAddress { get; set; }
        public long FirstAvailableAddress { get; set; }
        public int MaxFileTitleSize { get; set; }

        public FileSystemMetadata(int blockSize, int maxFileSize, long firstAddress, long firstAvailableAddress, int maxFileTitleSize)
        {
            BlockSize = blockSize;
            MaxFileSize = maxFileSize;
            FirstAddress = firstAddress;
            FirstAvailableAddress = firstAvailableAddress;
            MaxFileTitleSize = maxFileTitleSize;
        }
    }

    internal class FileSystem: IFileOperations<FileSystemMetadata>
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
            // Setting FileSystem metadata
            string fileMaxSizeInput = inputComponent(
                message: $"Do you want to set custom file max size? (default is {MetadataConstants.DefaultFileSystemFileMaxSize}) y/n",
                valueMessage: "Type the file max size:",
                errorMesassge: "Answer should be positive integer."
            ) ?? MetadataConstants.DefaultFileSystemFileMaxSize.ToString();

            string fileNameCharactersMaxSizeInput = inputComponent(
                message: $"Do you want to set custom file name characters max size? (default is {MetadataConstants.DefaultFileSystemFileNameCharacters}) y/n",
                valueMessage: "Type the file max size:",
                errorMesassge: "Answer should be positive integer."
            ) ?? MetadataConstants.DefaultFileSystemFileNameCharacters.ToString();

            // Writing metadata into the container
            FileSystemMetadata metadata = new FileSystemMetadata(
                    blockSize: int.Parse(chunkSize),
                    maxFileSize: int.Parse(fileMaxSizeInput),
                    maxFileTitleSize: int.Parse(fileNameCharactersMaxSizeInput),
                    firstAddress: 0,
                    firstAvailableAddress: 0
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
                while ((userInput == null) || (!int.TryParse(userInput, out int parsedSize) && parsedSize <= 0))
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
                    writer.Write(BitConverter.GetBytes(data.FirstAddress));
                    writer.Write(BitConverter.GetBytes(data.FirstAvailableAddress));
                    writer.Write(BitConverter.GetBytes(data.MaxFileSize));
                    writer.Write(BitConverter.GetBytes(data.MaxFileTitleSize));

                    // Getting the file system's metadata size
                    long sizeOfMetadata = stream.Position;
                    // Skipping block size to write the new size into first address
                    writer.Seek(sizeof(int), SeekOrigin.Begin);
                    writer.Write(BitConverter.GetBytes(sizeOfMetadata));
                    writer.Write(BitConverter.GetBytes(sizeOfMetadata));

                    return null;
                }
            }
        }

        public static FileSystemMetadata ReadData(string filePath, long startAddress)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    int blockSize = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
                    Console.WriteLine("blockSize: " + blockSize);
                    long firstAddress = BitConverter.ToInt64(reader.ReadBytes(sizeof(long)), 0);
                    Console.WriteLine("firstAddress: " + firstAddress);
                    long firstAvailableAddress = BitConverter.ToInt64(reader.ReadBytes(sizeof(long)), 0);
                    Console.WriteLine("firstAvailableAddress: " + firstAvailableAddress);
                    int maxFileSize = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
                    Console.WriteLine("MaxFileSize: " + maxFileSize);
                    int maxFileTitleSize = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
                    Console.WriteLine("maxFileTitle: " + maxFileTitleSize);

                    return new FileSystemMetadata(
                        blockSize: blockSize,
                        firstAddress: firstAddress,
                        firstAvailableAddress: firstAvailableAddress,
                        maxFileSize: maxFileSize,
                        maxFileTitleSize: maxFileTitleSize
                    );
                }
            }
        }
        public static void ChnageFirstAvailableAddress(long newAddress, string filePath)
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
