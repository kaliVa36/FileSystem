namespace Constants
{
    public static class ConstantsValues
    {
        public const string ContainerName = "container.bin";
    }

    public static class ErrorConstants
    {
        public const string LocationNotExist = "Location does not exist";
        public const string FileNotSupported = "This file type is not supported";
        public const string InvalidCommand = "Invalid command";
        public const string TitleTooLong = "Title is too long. Max size for a title is ";
    }

    public static class MetadataConstants
    {
        public const int DefaultFileSystemFileMaxSize = 1073741824;
        public const int DefaultFileSystemFileNameCharacters = 20;
        public const int DefaultMetadataStorage = 10240; // 10 KB
    }

    public static class FileConstants
    {
        public const int FileTypeMaxCharacters = 4;
        public const int ReadFileBuffer = 16;
    }
}
