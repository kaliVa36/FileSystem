using Constants;
using FS.Extensions;
using System.Drawing;
using System.Text;
using static FS.data.FSFile;

namespace FS.data
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
                    return (stream.Position-startAddress);
                }
            }
        }

        public static FileMetadata? ReadData(string filePath, long startAddress, int maxFileTitleSize)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(startAddress, SeekOrigin.Current);
                using (var reader = new BinaryReader(stream))
                {
                    byte[] buffer = new byte[maxFileTitleSize];
                    int bytesRead = stream.Read(buffer, 0, maxFileTitleSize);
                    // Convert the bytes to a string
                    string title = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(title.TrimZeroes());

                    long size = BitConverter.ToInt64(reader.ReadBytes(sizeof(long)), 0);
                    Console.WriteLine(size);

                    byte[] content = reader.ReadBytes((int)size);
                    Console.WriteLine(Encoding.UTF8.GetString(content));
                }
                return null;
            }
        }

        public static void ReadFile(string containerPath, string filePath, long firstAvailbleAddress, int maxFileTitleSize, string newFileName)
        {
            int bufferSize = FileConstants.ReadFileBuffer; // Example buffer size
            long lastStreamPosition = 0;
            long updatedSizePosition = 0;
            int sizeOfFile = 0;
            using (FileStream file = new FileStream(filePath, FileMode.Open))
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead;

                using (FileStream stream = new FileStream(containerPath, FileMode.Append, FileAccess.Write))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        stream.Seek(firstAvailbleAddress, SeekOrigin.Begin);
                        byte[] title = Encoding.UTF8.GetBytes(newFileName.PadZeroes(maxFileTitleSize));
                        stream.Write(title);
                        updatedSizePosition = stream.Position;
                        stream.Write(BitConverter.GetBytes(0L)); // will change later
                        while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            writer.Write(buffer, 0, bytesRead);
                            sizeOfFile += bytesRead;
                        }
                        lastStreamPosition = stream.Position;
                    }
                }
            }

            using (FileStream stream = new FileStream(containerPath, FileMode.Open))
            {
                if (updatedSizePosition != 0 && sizeOfFile != 0)
                {
                    stream.Seek(updatedSizePosition, SeekOrigin.Begin);
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(BitConverter.GetBytes(sizeOfFile));
                    }
                }
            }

            if (lastStreamPosition != 0)
            {
                FileSystem.ChangeFirstAvailableAddress(lastStreamPosition, containerPath);
            }
        }
    }
}
