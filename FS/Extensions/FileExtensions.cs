using Constants;

namespace FS.Extensions
{
    public static class FileExtensions
    {
        public static void ReadFilePath(this string path)
        {
            if (Path.Exists(path))
            {
                if (!File.Exists(path + "/" + ConstantsValues.ContainerName))
                {
                    FileSystemData.FileSystem.InitializeFileSystem(path);
                    Console.WriteLine("File system created");
                }
                else
                {
                    Console.WriteLine("Opening FS.. You can start writing");
                }
            }
            else
            {
                Console.WriteLine("Path does not exist " + path);
                throw new Exception(ErrorConstants.LocationNotExist);
            }
        }
    }
}
