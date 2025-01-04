﻿using Constants;
using FS.data;
using FS.Extensions;
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
                    rm();
                    break;
                case var cmd when cmd == FileCommandsEnum.cpout.ToString():
                    if (command.Length != (int)FileCommandsEnum.cp) 
                    {
                        Console.WriteLine(ErrorConstants.InvalidCommand);
                        break;
                    }
                    cpout(containerPath, command[2], command[1], metadata);
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
            using (FileStream stream = new FileStream(containerPath, FileMode.Open, FileAccess.Read))
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
        }

        public static void rm()
        {
            Console.WriteLine("rm");
        }

        public static void cpout(string containerPath, string filePath, string fileName, FileSystemMetadata metadata)
        {
            if (!Directory.Exists(filePath.SplitLastChar('\\')))
            {
                Console.WriteLine(ErrorConstants.LocationNotExist);
                return;
            }

            using (FileStream stream = new FileStream(containerPath, FileMode.Open))
            {
               // stream.Seek(metadata.FirstMetadataAddress, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < metadata.FirstFileAddress)
                    {
                        byte[] buffer = new byte[metadata.MaxFileTitleSize];
                        int bytesRead = stream.Read(buffer, 0, metadata.MaxFileTitleSize);
                        // Convert the bytes to a string
                        string title = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimZeroes();

                        long size = reader.ReadInt64();

                        if (fileName == title)
                        {
                            long position = stream.Position;
                            stream.Close();
                            FSFile.WriteFileFromContainer(containerPath, filePath, position, size);
                            break;
                        }

                        stream.Seek(size, SeekOrigin.Current);

                        if (stream.Position >= metadata.FirstFileAddress) break;
                    }
                }
            }
        }
    }
}
