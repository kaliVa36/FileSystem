using Constants;
using FS.data;
using FS.Extensions;
using static FS.data.FSFile;

namespace FS
{
    internal class Commands
    {
        public static void callCommand(string[] command, FileSystemMetadata metadata, string containerPath, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap, long currentDirectory)
        {
            switch (command[0])
            {
                case var cmd when cmd == CommandsEnum.cpin.ToString():
                    if (command.Length != (int)CommandsEnum.cp) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cpin(containerPath, command[1], command[2], metadata, fileBitmap, metadataBitmap, currentDirectory);
                    break;
                case var cmd when cmd == CommandsEnum.ls.ToString():
                    if (command.Length != (int)CommandsEnum.ls)
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    ls(containerPath, metadata.MaxFileTitleSize, metadata, metadataBitmap, currentDirectory);
                    break;
                case var cmd when cmd == CommandsEnum.rm.ToString():
                    if (command.Length != (int)CommandsEnum.rm) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    rm(containerPath, command[1], metadata, fileBitmap, metadataBitmap, currentDirectory);
                    break;
                case var cmd when cmd == CommandsEnum.cpout.ToString():
                    if (command.Length != (int)CommandsEnum.cp) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cpout(containerPath, command[2], command[1], metadata, fileBitmap, metadataBitmap, currentDirectory);
                    break;
                case var cmd when cmd == CommandsEnum.md.ToString():
                    if (command.Length != 2)
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    md(command[1], containerPath, metadata, fileBitmap, metadataBitmap, currentDirectory);
                    break;
                case var cmd when cmd == CommandsEnum.cd.ToString():
                    if (command.Length != 2)
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cd();
                    break;
                case var cmd when cmd == CommandsEnum.rd.ToString():
                    if (command.Length != 2)
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    rd();
                    break;
                default:
                    Console.WriteLine(ErrorConstants.InvalidCommand);
                    break;
            }
        }

        public static void cpin(string containerPath, string path, string fileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap, long currentDirectory)
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
            FSFile.ReadFile(containerPath, path, fileName, fsMetadata, fileBitmap, metadataBitmap, currentDirectory);
        }

        public static void ls(string containerPath, int fileTitleMaxSize, FileSystemMetadata fsMetadata, FSBitmapManager metadataBitmap, long currentDirectory)
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
                    if (metadata != null && !metadata.IsDirectory) { Console.WriteLine(metadata.FileName + " " + metadata.Size); }
                }
            }
        }

        public static void rm(string containerPath, string fileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap, long currentDirectory)
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

        public static void cpout(string containerPath, string filePath, string fileName, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap, long currentDirectory)
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

        public static void md(string dirName, string containerPath, FileSystemMetadata fsMetadata, FSBitmapManager fileBitmap, FSBitmapManager metadataBitmap, long currentDirectory)
        {
            int block = metadataBitmap.FindFreeBlock();
            if (block == -1) { Console.WriteLine(ErrorConstants.MetadataNoFreeSpace); }

            FileMetadata? currentDirectoryMetadata = ReadData(containerPath, currentDirectory, fsMetadata.MaxFileTitleSize);

            if (currentDirectoryMetadata == null)
            { 
                Console.WriteLine(ErrorConstants.FileNotFound);
                return;
            }

            if (!FSFile.CanAddChild(256, fsMetadata.MaxFileTitleSize, currentDirectoryMetadata.ChildrenSize))
            {
                Console.WriteLine("No space");
                return;
            }

            FileMetadata metadata = new FileMetadata(dirName, true, currentDirectory);

            WriteMetadata(containerPath, fsMetadata.FirstFileMetadataAddress, fsMetadata.FirstBitmapMetadataAddress, metadata, fsMetadata.MaxFileTitleSize, fileBitmap, 256, currentDirectory, true);
        }

        public static void cd()
        { 
        
        }

        public static void rd()
        { 
        
        }
    }
}
