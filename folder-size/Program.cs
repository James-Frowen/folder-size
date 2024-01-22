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
                var path = getPath(args);
                var addSeparate = getSeparate(args);
                var depth = getDepth(args);
                var dumpToFile = getFlag(args, "--dump");
                var fullName = getFlag(args, "--fullname") || getFlag(args, "-full");
                var skipCheck = getFlag(args, "-y") || getFlag(args, "--skip-check");
                var showLarge = getFlag(args, "--show-large");

                Console.WriteLine($"Find folder sizers with args: ");
                Console.WriteLine($"  dumpToFile: {dumpToFile}");
                Console.WriteLine($"  fullName: {fullName}");
                Console.WriteLine($"  showLarge: {showLarge}");
                if (addSeparate.Count == 0)
                    Console.WriteLine($"  addSeparate: <empty>");
                else
                {
                    foreach (var s in addSeparate)
                    {
                        Console.WriteLine($"  addSeparate: {s}");
                    }
                }
                Console.WriteLine($"");
                if (!skipCheck)
                {
                    Console.WriteLine($"Use '-y' to skip this check");
                    Console.Read();
                }
                Console.WriteLine($"Running...");
                var finder = new SizeFinder(path);
                finder.Find(depth, addSeparate, showLarge);
                Console.WriteLine("----------");
                Console.WriteLine("RESULTS:");
                Console.WriteLine("----------");
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
                Console.WriteLine($"No path given, using current folder");
                return ".";
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

        private static List<string> getSeparate(string[] args)
        {
            var separates = new List<string>();
            const string flag = "-separate=";
            foreach (var arg in args)
            {
                if (arg.ToLower().StartsWith(flag))
                {
                    var filter = arg.Substring(flag.Length);
                    separates.Add(filter);
                }
            }
            return separates;
        }

        private static int getDepth(string[] args)
        {
            const string flag = "-depth=";
            foreach (var arg in args)
            {
                if (arg.ToLower().StartsWith(flag))
                {
                    var depth = arg.Substring(flag.Length);
                    return int.Parse(depth);
                }
            }
            return 1;
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
        private const long LARGE_SIZE = 500_000_000;// 500mb

        private string targetPath;
        public List<SizeInfo> sizes;
        public long total;

        public SizeFinder(string targetPath)
        {
            this.targetPath = targetPath;
        }

        public void Display(bool fullName)
        {
            foreach (var item in sizes)
            {
                writeSize(item, fullName);
            }
        }
        public void DisplayOrdered(bool fullName)
        {
            var maxNameLength = sizes.Max(i => fullName ? i.info.FullName.Length : i.info.Name.Length);
            var padding = Math.Max(maxNameLength + 20, 50);

            // write total, followed by empty line
            writeSize("total", total, padding);
            Console.WriteLine();

            var ordered = sizes.OrderByDescending(i => i.size);
            foreach (var item in ordered)
            {
                writeSize(item, fullName, padding);
            }
        }

        private void writeSize(SizeInfo info, bool fullName, int padding = 50)
        {
            writeSize(info.info, info.size, fullName, padding);
        }
        private void writeSize(FileSystemInfo file, long length, bool fullName, int padding = 50)
        {
            var fileName = fullName ? file.FullName : file.Name;
            var name = fileName + (file.Attributes == FileAttributes.Directory ? "/" : "");
            writeSize(name, length, padding);
        }
        private void writeSize(string name, long length, int padding = 50)
        {
            var size = SizeSuffix(length, paddingSize: 10);
            Console.WriteLine("{0} | {1}", name.PadRight(padding), size.PadRight(20));
        }

        public void Find(int depth, List<string> showSeparate, bool showLarge)
        {
            sizes = new List<SizeInfo>();

            var targetDir = new DirectoryInfo(targetPath);

            total = getDirSize(targetDir, depth, showSeparate, showLarge);
        }


        private long getDirSize(DirectoryInfo parent, int showDepth, List<string> showSeparate, bool showLarge)
        {
            try
            {
                long current = 0;

                var dirs = parent.GetDirectories();
                foreach (var dir in dirs)
                {
                    var child = getDirSize(dir, showDepth - 1, showSeparate, showLarge);
                    if (showDepth > 0) // use too show folders in sub dir 
                    {
                        sizes.Add(new SizeInfo(dir, child));
                    }
                    else if (showSeparate.Contains(dir.Name)) // use to show folder seperate (like .../library/)
                    {
                        sizes.Add(new SizeInfo(dir, child));
                    }
                    else if (showLarge && child > LARGE_SIZE) // use to show folders that are large
                    {
                        sizes.Add(new SizeInfo(dir, child));
                    }

                    current += child;
                }

                var files = parent.GetFiles();
                foreach (var file in files)
                {
                    var child = file.Length;
                    if (showDepth > 0)
                    {
                        sizes.Add(new SizeInfo(file, child));
                    }

                    current += child;
                }

                return current;
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
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"Could not Unauthorized {e.Message}");
                return 0;
            }
            catch (PathTooLongException e)
            {
                Console.WriteLine($"Path too long {e.Message}");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected Exeption {e.GetType()} - {e.Message}\n{e.StackTrace}\n\n");
                return -1;
            }
        }

        private static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        private static string SizeSuffix(long value, int decimalPlaces = 1, int paddingSize = 0)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            var i = 0;
            decimal dValue = value;
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

