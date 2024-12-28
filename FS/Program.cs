// See https://aka.ms/new-console-template for more information
using FS;
using FileSystemData;
using FS.Extensions;

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
                metadata = FileSystemData.FileSystem.InitializeFileSystem(path);
            }
            else
            {
                metadata = FileSystemData.FileSystem.ReadData(path.AddFileName(), 0);
            }
            FSFile.FileMetadata metadataFile = new FSFile.FileMetadata("text.txt", "Hello, my name is Lenchezar. Are you 06, cause I like you?");

            FSFile.WriteData(metadataFile, path.AddFileName(), metadata.FirstAvailableAddress, metadata.MaxFileTitleSize);
            FSFile.ReadData(path.AddFileName(), metadata.FirstAvailableAddress);
        }
    }
}
