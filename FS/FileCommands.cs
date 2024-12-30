using Constants;
using FS.data;
using FS.Extensions;
using System.Text;

namespace FS
{
    public class FileCommands
    {
        public static void callCommand(string[] command, FileSystemMetadata metadata, string containerPath)
        {
            switch (command[0])
            {
                case var cmd when cmd == FileCommandsEnum.cpin.ToString():
                    if (command.Length != (int)FileCommandsEnum.cp) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cpin(containerPath, command[1], command[2], metadata.MaxFileTitleSize, metadata.FirstAvailableAddress);
                    break;
                case var cmd when cmd == FileCommandsEnum.ls.ToString():
                    if (command.Length != (int)FileCommandsEnum.ls) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    ls(containerPath, metadata.MaxFileTitleSize, metadata.FirstAddress, metadata.FirstAvailableAddress);
                    break;
                case var cmd when cmd == FileCommandsEnum.rm.ToString():
                    if (command.Length != (int)FileCommandsEnum.rm) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    rm();
                    break;
                case var cmd when cmd == FileCommandsEnum.cpout.ToString():
                    if (command.Length != (int)FileCommandsEnum.cp) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cpout(containerPath, command[2], command[1], metadata);
                    break;
                default:
                    Console.WriteLine(ErrorConstants.InvalidCommand);
                    break;
            }
        }

        public static void cpin(string containerPath, string path, string fileName, int maxFileTitleSize, long firstAvailableAddress)
        {
            if (!File.Exists(path))
            { 
                Console.WriteLine(ErrorConstants.LocationNotExist);
                return;
            }
            if (fileName.Length > maxFileTitleSize)
            {
                Console.WriteLine(ErrorConstants.TitleTooLong + maxFileTitleSize);
                return;
            }
            FSFile.ReadFile(containerPath: containerPath, filePath: path, newFileName: fileName, firstAvailbleAddress: firstAvailableAddress, maxFileTitleSize: maxFileTitleSize);
        }

        public static void ls(string containerPath, int fileTitleMaxSize, long firstAddress, long firstAvailableAddress)
        {
            using (FileStream stream = new FileStream(containerPath, FileMode.Open))
            {
                stream.Seek(firstAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < firstAvailableAddress)
                    {
                        byte[] buffer = new byte[fileTitleMaxSize];
                        int bytesRead = stream.Read(buffer, 0, fileTitleMaxSize);
                        // Convert the bytes to a string
                        string title = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimZeroes();

                        long size = reader.ReadInt64();

                        Console.WriteLine($"{title} {size}");

                        stream.Seek(size, SeekOrigin.Current);

                        if (stream.Position >= firstAvailableAddress) break;
                    }
                }
            }
        }

        public static void rm()
        {
            Console.WriteLine("rm");
        }

        public static void cpout(string containerPath, string filePath, string fileName, FileSystemMetadata metadata)
        {
            if (!Directory.Exists(filePath.SplitLastChar('\\')))
            {
                Console.WriteLine(ErrorConstants.LocationNotExist);
                return;
            }

            using (FileStream stream = new FileStream(containerPath, FileMode.Open))
            {
                stream.Seek(metadata.FirstAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < metadata.FirstAvailableAddress)
                    {
                        byte[] buffer = new byte[metadata.MaxFileTitleSize];
                        int bytesRead = stream.Read(buffer, 0, metadata.MaxFileTitleSize);
                        // Convert the bytes to a string
                        string title = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimZeroes();

                        long size = reader.ReadInt64();

                        if (fileName == title)
                        {
                            Console.WriteLine("1");
                            long position = stream.Position;
                            stream.Close();
                            FSFile.WriteFileFromContainer(containerPath, filePath, position, size);
                            break;
                        }

                        stream.Seek(size, SeekOrigin.Current);

                        if (stream.Position >= metadata.FirstAvailableAddress) break;
                    }
                }
            }
        }
    }
}
