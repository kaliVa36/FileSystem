// See https://aka.ms/new-console-template for more information
using System;
using ExtensionMethods;

namespace FileSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Reading the container's location
            Console.WriteLine("Enter location for storing the container:");
            string path = Console.ReadLine();
            path.ReadFilePath();

        }
}
}
