// See https://aka.ms/new-console-template for more information
using FS.Extensions;
using FS.data;
using FS;
using Constants;

namespace FileSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Reading the container's location
            Console.WriteLine("Enter location for storing the container:");
            string? path = Console.ReadLine();
            FileSystemMetadata fsMetadata;
            FSBitmapManager bitmapMetadata = new FSBitmapManager(MetadataConstants.DefaultBitmapMetadataSize);
            FSBitmapManager bitmapFile = new FSBitmapManager(MetadataConstants.DefaultBitmapFileSize);
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path.AddFileName()))
            {
                fsMetadata = FSFileSystem.InitializeFileSystem(path.AddFileName());

                bitmapMetadata.InitializeBitmap(path.AddFileName(), MetadataConstants.DefaultFileSystemMetadataSize, FileConstants.ReadBitmapBuffer);
                bitmapFile.InitializeBitmap(path.AddFileName(), fsMetadata.FirstBitmapFileAddress, FileConstants.ReadFileBuffer);
            }
            else
            {
                fsMetadata = FSFileSystem.ReadData(path.AddFileName(), 0);
                bitmapMetadata.LoadBitmap(path.AddFileName(), MetadataConstants.DefaultFileSystemMetadataSize, FileConstants.ReadBitmapBuffer);
                // bitmapMetadata.PrintBitmap(); // for testing
                bitmapFile.LoadBitmap(path.AddFileName(), fsMetadata.FirstBitmapFileAddress, FileConstants.ReadBitmapBuffer);
                Console.WriteLine("Bitmap of files");
                // bitmapFile.PrintBitmap();
            }

            while (true)
            {
                Console.WriteLine("Enter command: ");
                string? command = Console.ReadLine();
                if (command != null)
                {
                    FileCommands.callCommand(command.SplitByChar(' '), FSFileSystem.ReadData(path.AddFileName(), 0), path.AddFileName(), bitmapFile, bitmapMetadata);
                }
            }
        }
    }
}
