using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

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

                if (getFlag(args, "--find-dups"))
                {
                    Console.WriteLine($"Running with --find-dups");
                    FindDups.Scan(path);
                }
                else if (getFlag(args, "--fix-drive"))
                {
                    var real = getFlag(args, "--run");
                    var fixer = new GoogleDriveFixer(whatIf: !real);
                    fixer.Fix(path);
                }
                else
                {
                    Console.WriteLine($"Find folder sizers with args: ");
                    Console.WriteLine($"  dumpToFile: {dumpToFile}");
                    Console.WriteLine($"  fullName: {fullName}");
                    Console.WriteLine($"  showLarge: {showLarge}");
                    if (addSeparate.Count == 0)
                    {
                        Console.WriteLine($"  addSeparate: <empty>");
                    }
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
                }

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

    internal class FindDups
    {
        public static void Scan(string rootFolder)
        {
            var dirHashes = new Dictionary<long, List<string>>();
            Walk(rootFolder, dirHashes);

            Console.WriteLine($"Walk complete");
            Console.WriteLine($"");
            Console.WriteLine($"");

            StringBuilder dupOut = new();
            foreach (var directories in dirHashes.Values.Where(x => x.Count > 1))
            {
                Write($"Files:");
                foreach (var file in Directory.GetDirectories(directories.First()))
                    Write($"    {file}");
                foreach (var file in Directory.GetFiles(directories.First()))
                    Write($"    {file}");

                Write($"Dir:");
                foreach (var dir in directories)
                    Write($"    {dir}");

                Write($"");
                Write($"");
            }

            File.WriteAllText("FindDups.log", dupOut.ToString());

            void Write(string msg)
            {
                Console.WriteLine(msg);
                dupOut.AppendLine(msg);
            }
        }

        private static long Walk(string dir, Dictionary<long, List<string>> dirHashes)
        {
            // skip these folders
            foreach (var exclude in new string[] {
                ".git",
                "Library",
                ".vs",
                "Temp",
                "node_modules",
            })
            {
                if (dir.EndsWith($"\\{exclude}"))
                    return 0;
            }

            Console.WriteLine($"Walk: {dir}");
            long hash = 0;
            try
            {
                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    var subHash = Walk(subDir, dirHashes);
                    hash = (hash * 7) + subHash;
                }

                foreach (var file in Directory.GetFiles(dir))
                {
                    var nameOnly = Path.GetFileName(file);
                    var nameHash = nameOnly.GetHashCode();
                    hash = (hash * 7) + nameHash;
                }

                if (!dirHashes.TryGetValue(hash, out var existing))
                {
                    existing = new List<string>();
                    dirHashes[hash] = existing;
                }
                existing.Add(dir);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Walk threw {e}");
            }

            return hash;
        }
    }

    internal class GoogleDriveFixer
    {
        private Dictionary<string, List<(string name, bool real)>> files = new Dictionary<string, List<(string name, bool real)>>();
        private readonly bool whatIf;

        public GoogleDriveFixer(bool whatIf)
        {
            this.whatIf = whatIf;
        }

        private void Add(string key, (string name, bool real) value)
        {
            if (!files.TryGetValue(key, out var list))
            {
                list = new List<(string name, bool real)>();
                files[key] = list;
            }

            list.Add(value);
        }

        private static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        public void Fix(string path)
        {
            Walk(new DirectoryInfo(path), CheckFile);

            foreach (var kvp in files.Where(x => x.Value.Count == 2))
            {
                Console.WriteLine($"{kvp.Key}");
                FixFile(kvp);
                Console.WriteLine($"");
            }


            var firstAbove = true;
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var kvp in files.Where(x => x.Value.Count > 2))
            {
                if (firstAbove)
                {
                    firstAbove = false;
                    Console.WriteLine($"ABOVE 2 SAME");
                }
                Console.WriteLine($"{kvp.Key}");
            }
            Console.ResetColor();
        }

        private void FixFile(KeyValuePair<string, List<(string name, bool real)>> kvp)
        {
            FileInfo real, copy;
            if (kvp.Value[0].real)
            {
                if (kvp.Value[1].real)
                {
                    Error($"Both files real {kvp.Key}");
                    return;
                }
                real = new FileInfo(kvp.Value[0].name);
                copy = new FileInfo(kvp.Value[1].name);
            }
            else
            {
                real = new FileInfo(kvp.Value[1].name);
                copy = new FileInfo(kvp.Value[0].name);
            }

            var realSize = real.Length;
            var copySize = copy.Length;
            if (realSize > 0 && copySize > 0)
            {
                Error($"Both have size {kvp.Key}");
                return;
            }
            if (realSize == 0 && copySize == 0)
            {
                Error($"Both no size {kvp.Key}");
                return;
            }

            if (realSize > 0)
            {
                Console.WriteLine($"Delete {copy.Name} - {copy.Length}");
                if (!whatIf)
                {
                    Console.WriteLine($"{copy.Attributes}");
                    copy.Delete();
                }
            }
            else
            {
                Console.WriteLine($"Rename {copy.Name} - {copy.Length}");
                Console.WriteLine($"Delete {real.Name} - {real.Length}");
                if (!whatIf)
                {
                    var realPath = real.FullName;
                    // for some reason we have to first move the file before we can delete it
                    // so just append a known string to the end, then we can delete it after
                    real.MoveTo(realPath + ".badfile");
                    copy.MoveTo(realPath);

                    File.Delete(realPath + ".badfile");
                }
            }
        }

        private void CheckFile(FileInfo info)
        {
            var name = info.FullName;
            if (!name.Contains("."))
                return;
            var lastIndex = name.LastIndexOf('.');
            var extension = name[lastIndex..];
            var withoutExtension = name[..lastIndex];

            if (withoutExtension.EndsWith(" (1)"))
            {
                var otherPath = withoutExtension[..^" (1)".Length] + extension;
                Add(otherPath, (name, false));
            }

            Add(name, (name, true));
        }

        public static void Walk(DirectoryInfo dir, Action<FileInfo> onFile)
        {
            foreach (var sub in dir.GetDirectories())
                Walk(sub, onFile);

            foreach (var file in dir.GetFiles())
                onFile.Invoke(file);
        }
    }
}

