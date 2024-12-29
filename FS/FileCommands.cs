using Constants;
using FS.data;

namespace FS
{
    public class FileCommands
    {
        public static void callCommand(string[] command)
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
                    cpin();
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
                        Console.WriteLine(1);
                        break;
                    }
                    cpout();
                    break;
                default:
                    Console.WriteLine(ErrorConstants.InvalidCommand);
                    Console.WriteLine(2);
                    break;
            }
        }

        public static void cpin()
        {
            Console.WriteLine("cpin");
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
