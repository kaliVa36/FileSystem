using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Constants;

namespace ExtensionMethods
{
    public static class FileExtensions 
    {
        public static void ReadFilePath(this string? path) 
        { 
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (Path.Exists(path))
            {
                BinaryWriter bwStream = 
                    new BinaryWriter(new FileStream(path + ConstantsValues.ContainerName, FileMode.Create));
            }
            else 
            {
                throw new Exception("Location does not exist"); // mocked for testing 
            }
        }
    }
}
