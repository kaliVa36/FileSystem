// See https://aka.ms/new-console-template for more information
using FS.Extensions;
using FS.data;
using FS;

namespace FileSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Reading the container's location
            Console.WriteLine("Enter location for storing the container:");
            string? path = Console.ReadLine();
            FileSystemMetadata metadata = new FileSystemMetadata(0, 0, 0, 0, 0);
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path.AddFileName()))
            {
                metadata = FS.data.FileSystem.InitializeFileSystem(path.AddFileName());
            }
            else
            {
                metadata = FS.data.FileSystem.ReadData(path.AddFileName(), 0);
            }
            // mocked data
            //FSFile.FileMetadata metadataFile = new FSFile.FileMetadata("text.txt", "Hello, my name is Lenchezar. Are you 06, cause I like you?");
            //long? address = FSFile.WriteData(metadataFile, path.AddFileName(), metadata.FirstAvailableAddress, metadata.MaxFileTitleSize);
            //FSFile.ReadData(path.AddFileName(), metadata.FirstAvailableAddress);

            //if (address != null)
            //{
            //    FileSystemData.FileSystem.ChnageFirstAvailableAddress(filePath: path.AddFileName(), newAddress: address ?? 0);
            //}
            FSFile.ReadData(path.AddFileName(), metadata.FirstAddress, metadata.MaxFileTitleSize);
            while (true)
            {
                Console.WriteLine("Enter command: ");
                string? command = Console.ReadLine();
                if (command != null)
                {
                    FileCommands.callCommand(command.SplitByChar(' '), metadata, path.AddFileName());
                }
            }
        }
    }
}
