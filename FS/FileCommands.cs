using Constants;
using FS.data;

namespace FS
{
    public class FileCommands
    {
        public static void callCommand(string[] command, FileSystemMetadata metadata, string containerPath)
        {
            Console.WriteLine(command[0]);
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
                    ls();
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
                    cpout();
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

        public static void ls()
        {
            Console.WriteLine("ls");
        }

        public static void rm()
        {
            Console.WriteLine("rm");
        }

        public static void cpout()
        {
            Console.WriteLine("cpout");
        }
    }
}
