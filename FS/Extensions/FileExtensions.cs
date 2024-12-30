using Constants;
using FS.data;

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
                    data.FSFileSystem.InitializeFileSystem(path);
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

            // Reverse the char[]
            int start = 0;
            while (start < count)
            {
                char temp = type[start];
                type[start] = type[count];
                type[count] = temp;

                start++;
                count--;
            }

            // Check if its supported
            FileTypeEnum[] types = (FileTypeEnum[])Enum.GetValues(typeof(FileTypeEnum));
            bool flag = false;
            for (int i = 0; i < types.Length - 1; i++)
            {
                if (new string(type) == types[i].ToString())
                {
                    flag = true;
                }
            }

            if (!flag) 
            { 
                throw new Exception(ErrorConstants.FileNotSupported);
            }
            return new string(type);
        }
    }
}
