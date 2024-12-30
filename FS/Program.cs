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
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path.AddFileName()))
            {
                FSFileSystem.InitializeFileSystem(path.AddFileName());
            }
            else
            {
                FSFileSystem.ReadData(path.AddFileName(), 0);
            }
            
            // FSFile.ReadData(path.AddFileName(), metadata.FirstAddress, metadata.MaxFileTitleSize);
            while (true)
            {
                Console.WriteLine("Enter command: ");
                string? command = Console.ReadLine();
                if (command != null)
                {
                    FileCommands.callCommand(command.SplitByChar(' '), FSFileSystem.ReadData(path.AddFileName(), 0), path.AddFileName());
                }
            }
        }
    }
}
