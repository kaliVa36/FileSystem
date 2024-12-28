using FS.Extensions;
using System.Text;
using static FS.FSFile;

namespace FS
{
    internal class FSFile : IFileOperations<FileMetadata>
    {
        public class FileMetadata
        {
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public string Content { get; set; }

            public FileMetadata(string fileName, string content)
            {
                FileName = fileName;
                // ContentType = contentType; // Will be added soon
                Content = content;
            }
        }

        public static long? WriteData(FileMetadata data, string filePath, long startAddress, int size)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                stream.Seek(startAddress, SeekOrigin.Begin);
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    byte[] text = Encoding.Unicode.GetBytes(data.Content);
                    writer.Write(data.FileName.PadZeroes(size));
                    writer.Write(BitConverter.GetBytes(text.Length));
                    writer.Write(text);
                    return stream.Position;
                }
            }
        }

        public static FileMetadata? ReadData(string filePath, long startAddress)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(startAddress, SeekOrigin.Current);
                using (var reader = new BinaryReader(stream))
                {

                    string title = reader.ReadString();
                    Console.WriteLine(title.TrimZeroes());
                    int size = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
                    Console.WriteLine(size);
                    byte[] content = reader.ReadBytes(size);
                    Console.WriteLine(Encoding.UTF8.GetString(content));
                }
                return null;
            }
        }
    }
}
