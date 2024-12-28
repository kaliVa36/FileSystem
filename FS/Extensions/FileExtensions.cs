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
                    data.FileSystem.InitializeFileSystem(path);
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

        public static string GetType(this string fileName)
        {
            char[] type = new char[4];
            int count = 0;
            for (int i = fileName.Length - 1; i >= 0; i--)
            {
                if (fileName[i] != '.')
                {
                    type[i] = fileName[i];
                    count++;
                }
                else
                {
                    break;
                }
            }

            int start = 0;
            while (start < count)
            {
                char temp = type[start];
                type[start] = type[count];
                type[count] = temp;

                start++;
                count--;
            }
            return new string(type);
        }
    }
}
