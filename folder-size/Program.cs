using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace folder_size
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var path = getPath(args);
                var dumpToFile = getDumpToFile(args);
                var finder = new SizeFinder(path);
                finder.Find();
                finder.DisplayOrdered();
                Console.Read();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }

        private static string getPath(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("No path given");
            }
            var path = args[0];

            if (Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
            {
                return path;
            }
            else if (Directory.Exists(path))
            {
                return path;
            }
            else
            {
                throw new Exception("Path is not well formatted");
            }
        }

        private static bool getDumpToFile(string[] args)
        {
            return args.Contains("--dump");
        }
    }

    class SizeInfo
    {
        public FileSystemInfo info;
        public long size;

        public SizeInfo(FileSystemInfo info, long size)
        {
            this.info = info;
            this.size = size;
        }
    }
    class SizeFinder
    {
        string targetPath;
        Uri targetUri;
        public List<SizeInfo> sizes;

        public SizeFinder(string targetPath)
        {
            this.targetPath = targetPath;
            this.targetUri = new Uri(Path.GetFullPath(targetPath));
        }
        public void Find()
        {
            this.sizes = new List<SizeInfo>();

            var targetDir = new DirectoryInfo(this.targetPath);
            var dirs = targetDir.GetDirectories();
            var files = targetDir.GetFiles();
            foreach (var dir in dirs)
            {
                long size = -1;
                try
                {
                    size = getDirSize(dir);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exeption {e.GetType()} - {e.Message}");
                }
                this.sizes.Add(new SizeInfo(dir, size));
            }
            foreach (var file in files)
            {
                long size = -1;
                try
                {
                    size = file.Length;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exeption {e.GetType()} - {e.Message}");
                }
                this.sizes.Add(new SizeInfo(file, size));
            }
        }

        public void Display()
        {
            foreach (var item in this.sizes)
            {
                this.writeSize(item);
            }
        }
        public void DisplayOrdered()
        {
            var ordered = this.sizes.OrderByDescending(i => i.size);
            var maxNameLength = this.sizes.Max(i => i.info.Name.Length);
            foreach (var item in ordered)
            {
                this.writeSize(item, Math.Max(maxNameLength + 20, 50));
            }
        }

        private void writeSize(FileSystemInfo file, long length, int padding = 50)
        {
            var name = file.Name + (file.Attributes == FileAttributes.Directory ? "/" : "");
            var size = SizeSuffix(length, paddingSize: 10);
            Console.WriteLine("{0} | {1}", name.PadRight(padding), size.PadRight(20));
        }
        private void writeSize(SizeInfo info, int padding = 50)
        {
            this.writeSize(info.info, info.size, padding);
        }

        private static long getDirSize(DirectoryInfo info)
        {
            long size = 0;
            var dirs = info.GetDirectories();
            var files = info.GetFiles();
            foreach (var dir in dirs)
            {
                size += getDirSize(dir);
            }
            foreach (var file in files)
            {
                size += file.Length;
            }
            return size;
        }


        static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        static string SizeSuffix(long value, int decimalPlaces = 1, int paddingSize = 0)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            var i = 0;
            var dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            var size = string.Format("{0:n" + decimalPlaces + "}", dValue);
            var suffix = SizeSuffixes[i];
            return size.PadRight(paddingSize) + " " + suffix;
        }
    }
}

