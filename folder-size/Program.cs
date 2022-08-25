using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace folder_size
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                string path = getPath(args);
                List<string> addSeparate = getSeparate(args);
                bool dumpToFile = getFlag(args, "--dump");
                bool fullName = getFlag(args, "--fullname") || getFlag(args, "-fullname");
                bool skipCheck = getFlag(args, "-y") || getFlag(args, "--skip-check");

                Console.WriteLine($"Find folder sizers with args: ");
                Console.WriteLine($"  dumpToFile: {dumpToFile}");
                Console.WriteLine($"  fullName: {fullName}");
                if (addSeparate.Count == 0)
                    Console.WriteLine($"  addSeparate: <empty>");
                else
                {
                    foreach (string s in addSeparate)
                    {
                        Console.WriteLine($"  addSeparate: {s}");
                    }
                }
                Console.WriteLine($"");
                Console.WriteLine($"Use '-y' to skip this check");
                Console.Read();
                Console.WriteLine($"Running...");
                SizeFinder finder = new SizeFinder(path);
                finder.Find(addSeparate);
                finder.DisplayOrdered(fullName);
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
            string path = args[0];

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


        private static List<string> getSeparate(string[] args)
        {
            List<string> separates = new List<string>();
            const string flag = "-separate=";
            foreach (string arg in args)
            {
                if (arg.ToLower().StartsWith(flag))
                {
                    string filter = arg.Substring(flag.Length);
                    separates.Add(filter);
                }
            }
            return separates;
        }

        private static bool getFlag(string[] args, string flag)
        {
            return args.Contains(flag);
        }
    }

    internal class SizeInfo
    {
        public FileSystemInfo info;
        public long size;

        public SizeInfo(FileSystemInfo info, long size)
        {
            this.info = info;
            this.size = size;
        }
    }

    internal class SizeFinder
    {
        private string targetPath;
        public List<SizeInfo> sizes;

        public SizeFinder(string targetPath)
        {
            this.targetPath = targetPath;
        }
        public void Find(List<string> showSeparate)
        {
            sizes = new List<SizeInfo>();

            DirectoryInfo targetDir = new DirectoryInfo(targetPath);
            DirectoryInfo[] dirs = targetDir.GetDirectories();
            FileInfo[] files = targetDir.GetFiles();
            foreach (DirectoryInfo dir in dirs)
            {
                long size = -1;
                try
                {
                    size = getDirSize(dir, showSeparate);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exeption {e.GetType()} - {e.Message}\n{e.StackTrace}\n\n");
                }
                sizes.Add(new SizeInfo(dir, size));
            }
            foreach (FileInfo file in files)
            {
                long size = -1;
                try
                {
                    size = file.Length;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exeption {e.GetType()} - {e.Message}\n{e.StackTrace}\n\n");
                }
                sizes.Add(new SizeInfo(file, size));
            }
        }

        public void Display(bool fullName)
        {
            foreach (SizeInfo item in sizes)
            {
                writeSize(item, fullName);
            }
        }
        public void DisplayOrdered(bool fullName)
        {
            IOrderedEnumerable<SizeInfo> ordered = sizes.OrderByDescending(i => i.size);
            int maxNameLength = sizes.Max(i => fullName ? i.info.FullName.Length : i.info.Name.Length);
            foreach (SizeInfo item in ordered)
            {
                writeSize(item, fullName, Math.Max(maxNameLength + 20, 50));
            }
        }

        private void writeSize(SizeInfo info, bool fullName, int padding = 50)
        {
            writeSize(info.info, info.size, fullName, padding);
        }
        private void writeSize(FileSystemInfo file, long length, bool fullName, int padding = 50)
        {
            string fileName = fullName ? file.FullName : file.Name;
            string name = fileName + (file.Attributes == FileAttributes.Directory ? "/" : "");
            string size = SizeSuffix(length, paddingSize: 10);
            Console.WriteLine("{0} | {1}", name.PadRight(padding), size.PadRight(20));
        }

        private long getDirSize(DirectoryInfo info, List<string> showSeparate)
        {
            try
            {
                long size = 0;

                DirectoryInfo[] dirs = info.GetDirectories();
                FileInfo[] files = info.GetFiles();
                foreach (DirectoryInfo dir in dirs)
                {
                    size += getDirSize(dir, showSeparate);
                }
                foreach (FileInfo file in files)
                {
                    size += file.Length;
                }


                if (showSeparate.Contains(info.Name))
                {
                    sizes.Add(new SizeInfo(info, size));
                }

                return size;
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine($"Could not Find {e.Message}");
                return 0;
            }
            catch (AccessViolationException e)
            {
                Console.WriteLine($"Could not Access {e.Message}");
                return 0;
            }
        }

        private static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        private static string SizeSuffix(long value, int decimalPlaces = 1, int paddingSize = 0)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            string size = string.Format("{0:n" + decimalPlaces + "}", dValue);
            string suffix = SizeSuffixes[i];
            return size.PadRight(paddingSize) + " " + suffix;
        }
    }
}

