namespace FS
{
    public interface IFileOperations<T>
    {
        public abstract static long? WriteData(T data, string filePath, long startAddress, int size = 0);
        public abstract static T? ReadData(string filePath, long startAddress, int maxFileTitleSize);
    }
}
