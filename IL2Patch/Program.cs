using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using System.Text.RegularExpressions;

namespace IL2Patch
{
    internal class BoxDrawing
    {
        private const char TopLeft = '╔';
        private const char TopRight = '╗';
        private const char BottomLeft = '╚';
        private const char BottomRight = '╝';
        private const char Horizontal = '═';
        private const char Vertical = '║';
        private const char LeftT = '╠';
        private const char RightT = '╣';

        private readonly int width;
        private readonly ConsoleColor defaultColor;

        public BoxDrawing(int width = 80)
        {
            this.width = width;
            this.defaultColor = Console.ForegroundColor;
        }

        public void DrawBoxStart(string title = null)
        {
            string topBorder = $"{TopLeft}{new string(Horizontal, width - 2)}{TopRight}";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(topBorder);

            if (!string.IsNullOrEmpty(title))
            {
                DrawBoxLine($"[ {title} ]", ConsoleColor.Cyan);
                DrawSeparator();
            }

            Console.ResetColor();
        }

        public void DrawBoxEnd()
        {
            string bottomBorder = $"{BottomLeft}{new string(Horizontal, width - 2)}{BottomRight}";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(bottomBorder);
            Console.ResetColor();
        }

        public void DrawSeparator()
        {
            string separator = $"{LeftT}{new string(Horizontal, width - 2)}{RightT}";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(separator);
            Console.ResetColor();
        }

        public void DrawBoxLine(string text, ConsoleColor? color = null)
        {
            var originalColor = Console.ForegroundColor;
            if (color.HasValue) Console.ForegroundColor = color.Value;

            // Pad or truncate the text to fit within the box
            string contentSpace = new string(' ', width - 4);  // -4 for borders and spaces
            string paddedText = text.PadRight(width - 4);
            if (paddedText.Length > width - 4)
            {
                paddedText = paddedText.Substring(0, width - 7) + "...";
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{Vertical} ");
            Console.ForegroundColor = color ?? defaultColor;
            Console.Write(paddedText);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($" {Vertical}");

            Console.ForegroundColor = originalColor;
        }

        public void DrawSuccessLine(string text)
        {
            DrawBoxLine($"[+] {text}", ConsoleColor.Green);
        }

        public void DrawErrorLine(string text)
        {
            DrawBoxLine($"[x] {text}", ConsoleColor.Red);
        }

        public void DrawHexDump(byte[] data, int index, int length, int contextBytes = 8)
        {
            int start = Math.Max(0, index - contextBytes);
            int end = Math.Min(data.Length, index + length + contextBytes);

            for (int i = start; i < end; i += 16)
            {
                // Address offset
                var hexLine = new System.Text.StringBuilder();
                hexLine.Append($"{i:X8}  ");

                // Hex values
                var asciiLine = new System.Text.StringBuilder();
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length)
                    {
                        hexLine.Append($"{data[i + j]:X2} ");
                        byte b = data[i + j];
                        asciiLine.Append(b >= 32 && b < 127 ? (char)b : '.');
                    }
                    else
                    {
                        hexLine.Append("   ");
                        asciiLine.Append(" ");
                    }
                }

                DrawBoxLine($"{hexLine} | {asciiLine}",
                           (i >= index && i < index + length) ? ConsoleColor.Yellow : ConsoleColor.DarkGray);
            }
        }
    }

    internal class BuildToolsConfig
    {
        public string Version { get; set; }
        public string ZipalignPath { get; set; }
        public string ApksignerPath { get; set; }
        public bool SignApk { get; set; } = true;
        public bool AlignApk { get; set; } = true;
        public bool EnableDebugOutput { get; set; } = false;

        public bool IsValid => File.Exists(ZipalignPath) && (!SignApk || File.Exists(ApksignerPath));

        public static BuildToolsConfig LoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                var doc = XDocument.Load(path);
                var config = new BuildToolsConfig
                {
                    Version = doc.Root.Element("BuildToolsVersion")?.Value,
                    ZipalignPath = doc.Root.Element("ZipalignPath")?.Value,
                    ApksignerPath = doc.Root.Element("ApkSignerPath")?.Value,
                    SignApk = bool.Parse(doc.Root.Element("SignApk")?.Value ?? "true"),
                    EnableDebugOutput = bool.Parse(doc.Root.Element("EnableDebugOutput")?.Value ?? "false")
                };

                return config.IsValid ? config : null;
            }
            catch
            {
                return null;
            }
        }

        public void SaveToFile(string path)
        {
            var config = new XDocument(
                new XElement("Config",
                    new XElement("BuildToolsVersion", Version),
                    new XElement("ZipalignPath", ZipalignPath),
                    new XElement("ApkSignerPath", ApksignerPath),
                    new XElement("SignApk", SignApk),
                    new XElement("EnableDebugOutput", EnableDebugOutput)
                )
            );
            config.Save(path);
        }
    }

    internal static class Logger
    {
        private static string logDirectory;
        private static string logFile;
        private static bool enableDebugOutput;

        public static void Initialize(bool enableDebug)
        {
            enableDebugOutput = enableDebug;
            if (enableDebugOutput)
            {
                logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                Directory.CreateDirectory(logDirectory);
                logFile = Path.Combine(logDirectory, $"il2patch_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            }
        }

        public static void Log(string message, bool isDebug = false)
        {
            Console.WriteLine(message);

            if (enableDebugOutput)
            {
                try
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
                catch { /* Ignore logging errors */ }
            }
        }

        public static void LogProcessOutput(string processOutput, string processName)
        {
            if (enableDebugOutput)
            {
                try
                {
                    string processLogFile = Path.Combine(logDirectory, $"{processName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    File.WriteAllText(processLogFile, processOutput);
                }
                catch { /* Ignore logging errors */ }
            }
        }
    }

    internal class Program
    {
        static void Clean()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            // Delete XML documentation files
            foreach (string file in Directory.GetFiles(currentDirectory, "*.xml"))
            {
                try
                {
                    XDocument xml = XDocument.Load(file);
                    if (xml.Root?.Name == "doc" && xml.Root.Element("assembly") != null)
                    {
                        Console.WriteLine($"Deleting XML documentation file: {file}");
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignore invalid XML files
                }
            }

            // Delete .config and .pdb files
            string[] extensionsToDelete = { "*.config", "*.pdb" };
            foreach (string ext in extensionsToDelete)
            {
                foreach (string file in Directory.GetFiles(currentDirectory, ext))
                {
                    Console.WriteLine($"Deleting {Path.GetExtension(file)} file: {file}");
                    File.Delete(file);
                }
            }
        }

        static void Main(string[] args)
        {
            System.Threading.Thread.Sleep(1500);
            Clean();
            foreach (string file in new[] { "patched.apk", "aligned.apk", "signed.apk", "signed.apk.idsig" })
                if (File.Exists(file)) File.Delete(file);

            if (Directory.Exists("temp_apk"))
            {
                Directory.Delete("temp_apk", true);
            }
            
            Console.WriteLine("IL2Patch: APK Modding Tool");

            string originalTitle = Console.Title;
            var buildTools = BuildToolsConfig.LoadFromFile("config.xml") ?? SetupBuildTools();
            if (buildTools == null)
            {
                Console.WriteLine("Failed to configure build tools.");
                Environment.Exit(1);
            }

            // Detect APK in current directory
            string currentDirectory = Directory.GetCurrentDirectory();
            string apkPath = Directory.GetFiles(currentDirectory, "*.apk").FirstOrDefault();
            string xmlPath = GetPatchXmlFile(currentDirectory);

            if (apkPath == null)
            {
                Console.WriteLine("Error: No APK file found in the current directory.");
                Environment.Exit(1);
            }
            if (xmlPath == null)
            {
                Console.WriteLine("Error: No signature patch XML file found in the current directory.");
                Environment.Exit(1);
            }

            Console.WriteLine($"Found APK: {apkPath}");
            Console.WriteLine($"Found Patch File: {xmlPath}");

            // Unzip the APK
            string extractPath = Path.Combine(currentDirectory, "temp_apk");
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);
            Directory.CreateDirectory(extractPath);

            Console.WriteLine();
            Console.WriteLine("Extracting APK...");

            int totalFiles = 0;
            using (var archive = System.IO.Compression.ZipFile.OpenRead(apkPath))
            {
                totalFiles = archive.Entries.Count;
            }

            // Create a progress-tracking extraction method
            int currentFile = 0;
            using (var archive = System.IO.Compression.ZipFile.OpenRead(apkPath))
            {
                foreach (var entry in archive.Entries)
                {
                    currentFile++;
                    Console.Title = $"Extracting APK: {currentFile}/{totalFiles} ({(currentFile * 100 / totalFiles)}%) - {entry.Name}";

                    string destinationPath = Path.Combine(extractPath, entry.FullName);
                    string destinationDir = Path.GetDirectoryName(destinationPath);

                    if (!string.IsNullOrEmpty(destinationDir))
                        Directory.CreateDirectory(destinationDir);

                    // Don't try to create directory for empty entries (directories in zip)
                    if (!string.IsNullOrEmpty(entry.Name))
                        entry.ExtractToFile(destinationPath, true);
                }
            }
            currentFile = 0;

            Console.Title = originalTitle;

            // Detect architectures
            string libPath = Path.Combine(extractPath, "lib");
            if (!Directory.Exists(libPath))
            {
                Console.WriteLine("Error: No lib folder found in APK. Possibly not an IL2CPP app.");
                Environment.Exit(1);
            }

            var architectures = Directory.GetDirectories(libPath).Select(Path.GetFileName).ToArray();
            Console.WriteLine("Detected Architectures:");
            foreach (var arch in architectures)
            {
                Console.WriteLine($"- {arch}");
            }

            XDocument patchConfig = XDocument.Load(xmlPath);
            var patches = patchConfig.Descendants("Patch")
                                      .GroupBy(p => (string)p.Attribute("arch"))
                                      .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var arch in architectures)
            {
                var box = new BoxDrawing(80);

                box.DrawBoxStart($"Architecture: {arch}");
                string libFilePath = Path.Combine(libPath, arch, "libil2cpp.so");

                if (File.Exists(libFilePath))
                {
                    box.DrawBoxLine($"Processing {arch} library...");
                    box.DrawBoxEnd();

                    byte[] fileBytes = File.ReadAllBytes(libFilePath);

                    if (patches.ContainsKey(arch))
                    {
                        foreach (var patch in patches[arch])
                        {
                            string findBytes = patch.Element("Find")?.Value;
                            string replaceBytes = patch.Element("Replace")?.Value;
                            string description = patch.Element("Description")?.Value ?? "No description";

                            if (!string.IsNullOrEmpty(findBytes) && !string.IsNullOrEmpty(replaceBytes))
                            {
                                byte[] findPattern = StringToByteArray(findBytes);
                                byte[] replacePattern = StringToByteArray(replaceBytes);

                                int index = FindPattern(fileBytes, findPattern);

                                // Start patch box
                                box.DrawBoxStart($"Checking for patch: {description}");

                                if (index != -1)
                                {
                                    box.DrawSuccessLine($"Found signature at offset {index:X}, applying patch...");
                                    box.DrawSeparator();

                                    box.DrawBoxLine("Before:", ConsoleColor.DarkGray);
                                    box.DrawHexDump(fileBytes, index, replacePattern.Length);
                                    box.DrawSeparator();

                                    // Apply patch
                                    Array.Copy(replacePattern, 0, fileBytes, index, replacePattern.Length);

                                    box.DrawBoxLine("After:", ConsoleColor.DarkGray);
                                    box.DrawHexDump(fileBytes, index, replacePattern.Length);
                                }
                                else
                                {
                                    box.DrawErrorLine("Warning: Signature not found, skipping patch.");
                                }

                                box.DrawBoxEnd();
                            }
                        }

                        File.WriteAllBytes(libFilePath, fileBytes);
                        box.DrawBoxStart("Status");
                        box.DrawSuccessLine($"All patches applied for {arch}");
                        box.DrawBoxEnd();
                    }
                    else
                    {
                        box.DrawBoxStart("Warning");
                        box.DrawErrorLine($"No patches found for {arch}");
                        box.DrawBoxEnd();
                    }
                }
                else
                {
                    box.DrawBoxStart("Error");
                    box.DrawErrorLine($"libil2cpp.so not found for {arch}, skipping...");
                    box.DrawBoxEnd();
                }
            }

            string patchedApkPath = Path.Combine(currentDirectory, "patched.apk");
            Console.WriteLine("Repacking APK...");
            Dictionary<string, bool> compressionMap = new Dictionary<string, bool>();

            using (ICSharpCode.SharpZipLib.Zip.ZipFile originalZip = new ICSharpCode.SharpZipLib.Zip.ZipFile(File.OpenRead(apkPath)))
            {
                foreach (ZipEntry entry in originalZip)
                {
                    if (!entry.IsDirectory)
                    {
                        compressionMap[entry.Name] = entry.CompressionMethod == CompressionMethod.Stored;
                    }
                }
            }

            using (FileStream fsOut = File.Create(patchedApkPath))
            using (ZipOutputStream zipStream = new ZipOutputStream(fsOut))
            {
                zipStream.SetLevel(9);
                zipStream.UseZip64 = UseZip64.Off;

                // Get total file count for progress
                var allFiles = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);

                foreach (string file in allFiles)
                {
                    currentFile++;
                    string fileName = Path.GetFileName(file);

                    // Update console title with progress
                    Console.Title = $"Processing APK: {currentFile}/{totalFiles} ({(currentFile * 100 / totalFiles)}%) - {fileName}";

                    string relativePath = GetRelativePath(extractPath, file).Replace("\\", "/");
                    relativePath = relativePath.Substring(relativePath.IndexOf('/') + 1);

                    ZipEntry newEntry = new ZipEntry(relativePath);
                    newEntry.DateTime = DateTime.Now;

                    if (compressionMap.TryGetValue(relativePath, out bool isStored) && isStored)
                    {
                        newEntry.CompressionMethod = CompressionMethod.Stored;
                    }
                    else
                    {
                        newEntry.CompressionMethod = CompressionMethod.Deflated;
                    }

                    zipStream.PutNextEntry(newEntry);
                    using (FileStream fs = File.OpenRead(file))
                    {
                        fs.CopyTo(zipStream);
                    }
                    zipStream.CloseEntry();
                }

                Console.Title = "Finalizing APK...";
                currentFile = 0;
                zipStream.Finish();
                zipStream.Flush();
                fsOut.Flush();
            }

            Console.WriteLine($"Patched APK created: {patchedApkPath}");

            string alignedApkPath = Path.Combine(currentDirectory, "aligned.apk");
            string finalApkPath = buildTools.SignApk ?
                Path.Combine(currentDirectory, "signed.apk") :
                alignedApkPath;

            if (buildTools.AlignApk)
            {
                Logger.Log("Running zipalign...");
                var zipalignOutput = new StringBuilder();
                var zipalignProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = buildTools.ZipalignPath,
                        Arguments = $"-v 4 \"{patchedApkPath}\" \"{alignedApkPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                zipalignProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        zipalignOutput.AppendLine(e.Data);
                        if (buildTools.EnableDebugOutput)
                            Logger.Log($"zipalign: {e.Data}", true);
                    }
                };

                zipalignProcess.Start();
                zipalignProcess.BeginOutputReadLine();
                zipalignProcess.WaitForExit();

                Logger.LogProcessOutput(zipalignOutput.ToString(), "zipalign");

                if (zipalignProcess.ExitCode != 0)
                {
                    Logger.Log("Error during zipalign process.");
                    Environment.Exit(1);
                }
            }

            if (buildTools.SignApk)
            {
                string folderPath = Path.Combine(currentDirectory, "keystore");

                if (Directory.Exists(folderPath))
                {
                    string keystoreFilePath = Directory.GetFiles(folderPath, "*.keystore").FirstOrDefault();
                    string passwordFilePath = Directory.GetFiles(folderPath, "*.txt").FirstOrDefault();

                    if (keystoreFilePath == null || passwordFilePath == null)
                    {
                        Logger.Log("Error: Keystore (.keystore) or password (.txt) file not found in the folder.");
                        Environment.Exit(1);
                    }

                    // Sign APK
                    Logger.Log("Signing APK...");
                    var signOutput = new StringBuilder();
                    var signProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "java",
                            Arguments = $"-jar \"{buildTools.ApksignerPath}\" sign --ks \"{keystoreFilePath}\" " +
                                       $"--ks-key-alias android --ks-pass file:\"{passwordFilePath}\" " +
                                       $"--out \"{finalApkPath}\" \"{alignedApkPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    signProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            signOutput.AppendLine(e.Data);
                            if (buildTools.EnableDebugOutput)
                                Logger.Log($"apksigner: {e.Data}", true);
                        }
                    };

                    signProcess.Start();
                    signProcess.BeginOutputReadLine();
                    signProcess.WaitForExit();

                    Logger.LogProcessOutput(signOutput.ToString(), "apksigner");

                    if (signProcess.ExitCode != 0)
                    {
                        Logger.Log("Error during signing process.");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    Logger.Log("Error: The specified folder does not exist.");
                    Environment.Exit(1);
                }
            }

            // Cleanup temporary files
            if (File.Exists(patchedApkPath)) File.Delete(patchedApkPath);
            if (buildTools.SignApk && File.Exists(alignedApkPath)) File.Delete(alignedApkPath); Directory.Delete("temp_apk", true);

            Logger.Log($"Successfully created {(buildTools.SignApk ? "signed" : "aligned")} APK: {finalApkPath}");

            Console.WriteLine($"Successfully created {(buildTools.SignApk ? "signed" : "aligned")} APK: {finalApkPath}");
        }

        static BuildToolsConfig SetupBuildTools()
        {
            string androidSdkPath = Environment.GetEnvironmentVariable("ANDROID_HOME")
                                    ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

            if (string.IsNullOrEmpty(androidSdkPath))
            {
                string potentialPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Android", "Sdk");

                if (Directory.Exists(potentialPath))
                {
                    androidSdkPath = potentialPath;
                    Logger.Log("Found Android SDK at: " + androidSdkPath);
                }
                else
                {
                    Logger.Log("Error: Android SDK not found. Set ANDROID_HOME environment variable.");
                    return null;
                }
            }

            string buildToolsPath = Path.Combine(androidSdkPath, "build-tools");
            if (!Directory.Exists(buildToolsPath))
            {
                Logger.Log("Error: Build tools directory not found in SDK.");
                return null;
            }

            string[] availableVersions = Directory.GetDirectories(buildToolsPath)
                .Select(Path.GetFileName)
                .OrderByDescending(v => v)
                .ToArray();

            if (availableVersions.Length == 0)
            {
                Logger.Log("Error: No build tools versions found.");
                return null;
            }

            Logger.Log("Available Build Tools Versions:");
            for (int i = 0; i < availableVersions.Length; i++)
            {
                Logger.Log($"{i + 1}: {availableVersions[i]}");
            }

            Console.Write("Select version (enter number): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > availableVersions.Length)
            {
                Logger.Log("Invalid selection.");
                return null;
            }

            Console.Write("Align APK after patching? (Y/N): ");
            bool alignApk = Console.ReadLine()?.Trim().ToUpper().StartsWith("Y") ?? true;

            Console.Write("Sign APK after patching? (Y/N): ");
            bool signApk = Console.ReadLine()?.Trim().ToUpper().StartsWith("Y") ?? true;

            Console.Write("Enable debug output? (Y/N): ");
            bool enableDebugOutput = Console.ReadLine()?.Trim().ToUpper().StartsWith("Y") ?? false;

            string selectedVersion = availableVersions[selection - 1];
            string selectedPath = Path.Combine(buildToolsPath, selectedVersion);

            var config = new BuildToolsConfig
            {
                Version = selectedVersion,
                ZipalignPath = Directory.GetFiles(selectedPath, "zipalign.exe", SearchOption.AllDirectories).FirstOrDefault(),
                ApksignerPath = Directory.GetFiles(selectedPath, "apksigner.jar", SearchOption.AllDirectories).FirstOrDefault(),
                SignApk = signApk,
                AlignApk = alignApk,
                EnableDebugOutput = enableDebugOutput
            };

            if (!config.IsValid)
            {
                Logger.Log("Error: Required build tools not found in " + selectedPath);
                return null;
            }

            config.SaveToFile("config.xml");
            return config;
        }

        // https://stackoverflow.com/questions/51179331/is-it-possible-to-use-path-getrelativepath-net-core2-in-winforms-proj-targeti
        static string GetRelativePath(string relativeTo, string path)
        {
            var uri = new Uri(relativeTo);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{Path.DirectorySeparatorChar}{rel}";
            }
            return rel;
        }

        public static byte[] StringToByteArray(string hex)
        {
            // Remove any non-hexadecimal characters (e.g., spaces, commas, '0x', etc.)
            string cleanedHex = Regex.Replace(hex, @"[^0-9a-fA-F]", "");

            // Now we can proceed with the same logic to convert the cleaned hex string to bytes
            return Enumerable.Range(0, cleanedHex.Length / 2)
                             .Select(x => Convert.ToByte(cleanedHex.Substring(x * 2, 2), 16))
                             .ToArray();
        }

        static void PrintHexView(byte[] data, int index, int length, int contextBytes = 8)
        {
            int start = Math.Max(0, index - contextBytes);
            int end = Math.Min(data.Length, index + length + contextBytes);

            for (int i = start; i < end; i += 16)
            {
                Console.Write("║ ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{i:X8}  "); // Address offset
                Console.ResetColor();

                // Print hex bytes
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length)
                    {
                        if (i + j >= index && i + j < index + length)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow; // Highlight patched bytes
                        }
                        Console.Write($"{data[i + j]:X2} ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write("   "); // Padding for missing bytes
                    }
                }

                Console.Write(" | ");

                // Print ASCII representation
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length)
                    {
                        byte b = data[i + j];
                        Console.Write(b >= 32 && b < 127 ? (char)b : '.'); // Printable ASCII or dot
                    }
                }
                Console.WriteLine(" ║");
            }
        }

        static string GetPatchXmlFile(string currentDirectory)
        {
            var files = Directory.GetFiles(currentDirectory, "*.xml")
                                 .Where(file => !file.Contains("config"));

            foreach (var file in files)
            {
                try
                {
                    var xml = XDocument.Load(file);
                    var root = xml.Root;
                    if (root != null && root.Name.LocalName == "Patches" &&
                        root.Elements("Patch").Any())
                    {
                        return file; // Found a valid patch XML
                    }
                }
                catch
                {
                    // Ignore invalid XML files and continue checking others
                }
            }

            return null; // No valid patch XML found
        }
        static int FindPattern(byte[] data, byte[] pattern)
        {
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
        }
    }
}
