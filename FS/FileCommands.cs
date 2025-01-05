using Constants;
using FS.data;
using FS.Extensions;
using System.Net.Http.Headers;
using System.Text;
using static FS.data.FSFile;

namespace FS
{
    internal class FileCommands
    {
        public static void callCommand(string[] command, FileSystemMetadata metadata, string containerPath, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap)
        {
            switch (command[0])
            {
                case var cmd when cmd == FileCommandsEnum.cpin.ToString():
                    if (command.Length != (int)FileCommandsEnum.cp) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cpin(containerPath, command[1], command[2], metadata, fileBitmap, metadataBitmap);
                    break;
                case var cmd when cmd == FileCommandsEnum.ls.ToString():
                    if (command.Length != (int)FileCommandsEnum.ls)
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    ls(containerPath, metadata.MaxFileTitleSize, metadata, metadataBitmap);
                    break;
                case var cmd when cmd == FileCommandsEnum.rm.ToString():
                    if (command.Length != (int)FileCommandsEnum.rm) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    rm(containerPath, command[1], metadata, fileBitmap, metadataBitmap);
                    break;
                case var cmd when cmd == FileCommandsEnum.cpout.ToString():
                    if (command.Length != (int)FileCommandsEnum.cp) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cpout(containerPath, command[2], command[1], metadata, fileBitmap, metadataBitmap);
                    break;
                default:
                    Console.WriteLine(ErrorConstants.InvalidCommand);
                    break;
            }
        }

        public static void cpin(string containerPath, string path, string fileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap)
        {
            if (!File.Exists(path))
            { 
                Console.WriteLine(ErrorConstants.LocationNotExist);
                return;
            }
            if (fileName.Length > fsMetadata.MaxFileTitleSize)
            {
                Console.WriteLine(ErrorConstants.TitleTooLong + fsMetadata.MaxFileTitleSize);
                return;
            }
            FSFile.ReadFile(containerPath, path, fileName, fsMetadata, fileBitmap, metadataBitmap);
        }

        public static void ls(string containerPath, int fileTitleMaxSize, FileSystemMetadata fsMetadata, FSBitmapManager metadataBitmap)
        {
            for (int i = 0; i < metadataBitmap.TotalBlocks; i++)
            {
                if (metadataBitmap.IsBlockUsed(i))
                {
                    FileMetadata? metadata = ReadData(
                        containerPath,
                        fsMetadata.FirstFileMetadataAddress + (i * MetadataConstants.DefaultMetadataBlock),
                        fsMetadata.MaxFileTitleSize
                    );

                    if (metadata != null) { Console.WriteLine(metadata.FileName + " " + metadata.Size); }
                }
            }
        }

        public static void rm(string containerPath, string fileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap)
        {
            bool flag = false; // flag - does file exist

            for (int i = 0; i < metadataBitmap.TotalBlocks; i++)
            {
                if (metadataBitmap.IsBlockUsed(i))
                {
                    long metadataAddress = fsMetadata.FirstFileMetadataAddress + (i * MetadataConstants.DefaultMetadataBlock);
                    FileMetadata? metadata = ReadData(containerPath, metadataAddress, fsMetadata.MaxFileTitleSize);

                    if (metadata != null)
                    {
                        Console.WriteLine(metadata.FileName + " " + fileName);
                        if (metadata.FileName == fileName)
                        {
                            flag = true;
                            for (int j = 0; j < metadata.BlockIndexesSize; j++)
                            {
                                fileBitmap.MarkAsUnused(metadata.BlockIndexes[j]);

                                long blockAddress = fsMetadata.FirstFileAddress + ((int)metadata.BlockIndexes[j] * fsMetadata.BlockSize);
                                FSFile.DeleteData(containerPath, blockAddress, fsMetadata.BlockSize, FileConstants.ReadFileBuffer); // make dynaic fileBuffer
                            }
                            fileBitmap.StoreBitmap(containerPath, fsMetadata.FirstBitmapFileAddress, FileConstants.ReadFileBuffer);

                            metadataBitmap.MarkAsUnused(i);
                            metadataBitmap.StoreBitmap(containerPath, fsMetadata.FirstBitmapMetadataAddress, FileConstants.ReadBitmapBuffer);
                            
                            FSFile.DeleteData(containerPath, metadataAddress, MetadataConstants.DefaultMetadataBlock, FileConstants.ReadFileBuffer);
                        }
                    }
                }
            }

            if (!flag) Console.WriteLine(ErrorConstants.FileNotFound);
        }

        public static void cpout(string containerPath, string filePath, string fileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap)
        {
            if (!Directory.Exists(filePath.SplitLastChar('\\')))
            {
                Console.WriteLine(ErrorConstants.LocationNotExist);
                return;
            }

            bool flag = false; // flag - does file exists

            for (int i = 0; i < metadataBitmap.TotalBlocks; i++)
            {
                if (metadataBitmap.IsBlockUsed(i))
                {
                    long metadataAddress = fsMetadata.FirstFileMetadataAddress + (i * MetadataConstants.DefaultMetadataBlock);
                    FileMetadata? metadata = ReadData(containerPath, metadataAddress, fsMetadata.MaxFileTitleSize);

                    if (metadata != null)
                    {
                        if (metadata.FileName == fileName)
                        { 
                            flag = true;

                            using (FileStream stream = new FileStream(containerPath, FileMode.Open, FileAccess.Read))
                            {
                                using (FileStream file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                                {
                                    byte[] buffer = new byte[FileConstants.ReadFileBuffer];
                                    for (int j = 0; j < metadata.BlockIndexesSize; j ++)
                                    {
                                        long blockAddress = fsMetadata.FirstFileAddress + (metadata.BlockIndexes[j] * fsMetadata.BlockSize);
                                        stream.Seek(blockAddress, SeekOrigin.Begin);

                                        int bytesToRead = fsMetadata.BlockSize;
                                        while (bytesToRead > 0)
                                        {
                                            int bytesRead = stream.Read(buffer, 0, Math.Min(bytesToRead, buffer.Length));
                                            if (bytesRead == 0) break;

                                            file.Write(buffer, 0, bytesRead);
                                            bytesToRead -= bytesRead;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!flag) Console.WriteLine(ErrorConstants.FileNotFound);
        }
    }
}
