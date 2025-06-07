using SharpCompress.Archives;
using SharpCompress.Common;
using Serilog;
using System.IO.Compression;
using SharpCompress.Archives.SevenZip;

namespace PackageAnalyzer.Core.FileSystem
{
    public class ArchiveProvider : IFileProvider
    {
        static List<string> tempFiles = new List<string>();

        public List<FileInfo> FindFile(string directoryPath, string targetFileName, bool returnFirst = false)
        {
            try
            {
                List<FileInfo> filesCollection = new List<FileInfo>();

                // Search for the file in the current directory
                SearchArchive(directoryPath, targetFileName, filesCollection, tempFiles, returnFirst);

                if (filesCollection.Count > 0)
                {
                    return filesCollection;
                }
                else
                {
                    Log.Information($"File '{targetFileName}' not found in the current directory or its subdirectories for '{directoryPath}'");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error searching for file in the directory: {ex.Message}:\r\n{ex.StackTrace}");
                throw new Exception($"Error searching for file in the directory: {ex.Message}");
            }
        }

        public string FindFolder(string directoryPath, string targetFolderName)
        {
            try
            {
                List<string> foldersCollection = new List<string>();

                // Search for the folder in the current directory
                SearchFolderInArchive(directoryPath, targetFolderName, foldersCollection);

                if (foldersCollection.Count > 0)
                {
                    return foldersCollection[0]; // Return the first found folder path
                }
                else
                {
                    Log.Information($"Folder '{targetFolderName}' not found in the current directory or its subdirectories for '{directoryPath}'");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error searching for folder in the directory: {ex.Message}:\r\n{ex.StackTrace}");
                throw new Exception($"Error searching for folder in the directory: {ex.Message}");
            }
        }

        private void SearchArchive(string archivePath, string targetFileName, List<FileInfo> filesCollection, List<string> tempFiles = null, bool returnFirst = false)
        {
            targetFileName = targetFileName.Replace("*", "");
            try
            {
                using (var archive = ArchiveFactory.Open(archivePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            string fileName = Path.GetFileName(entry.Key).ToLowerInvariant();
                            if (returnFirst && fileName.Contains(targetFileName.ToLowerInvariant()))
                            {
                                ExtractEntry(entry, filesCollection, tempFiles);
                                if (returnFirst)
                                    return;
                            }
                            else if (fileName == targetFileName.ToLowerInvariant())
                            {
                                ExtractEntry(entry, filesCollection, tempFiles);
                            }
                        }
                        else if (IsArchiveFile(entry.Key))
                        {
                            string nestedArchivePath = Path.Combine(Path.GetDirectoryName(archivePath), entry.Key);
                            SearchArchive(nestedArchivePath, targetFileName, filesCollection, tempFiles);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error searching archive '{archivePath}': {ex.Message}");
            }
        }
        private void SearchFolderInArchive(string archivePath, string targetFolderName, List<string> foldersCollection)
        {
            try
            {
                using (var archive = ArchiveFactory.Open(archivePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Key.Contains(targetFolderName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Find the target folder index
                            int targetFolderIndex = entry.Key.IndexOf(targetFolderName, StringComparison.InvariantCultureIgnoreCase);
                            if (targetFolderIndex == -1)
                            {
                                Log.Error($"The target folder name '{targetFolderName}' was not found in '{entry.Key}'.");
                                continue;
                            }
                            // Find the index of the first slash after the target folder name
                            int slashIndex = entry.Key.IndexOf('/', targetFolderIndex);

                            if (slashIndex == -1)
                            {
                                Log.Error($"No slash ('/') was found in '{entry.Key}' after the target folder name '{targetFolderName}'.");
                                continue;
                            }
                            // Extract the folder path
                            string folderPath = entry.Key.Substring(0, slashIndex);
                            ExtractFolderFromEntry(folderPath, archive, foldersCollection);
                            break; // No need to continue once we start extracting the target folder
                        }
                        else if (IsArchiveFile(entry.Key))
                        {
                            string nestedArchivePath = Path.Combine(Path.GetDirectoryName(archivePath), entry.Key);
                            SearchFolderInArchive(nestedArchivePath, targetFolderName, foldersCollection);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error searching archive '{archivePath}': {ex.Message}");
            }
        }

        private void ExtractFolderFromEntry(string folderPath, IArchive archive, List<string> foldersCollection)
        {
            string tempRootFolder = Path.Combine(Path.GetTempPath(), "SearchArchiveTemp", folderPath);
            if (!Directory.Exists(tempRootFolder))
                Directory.CreateDirectory(tempRootFolder);

            foreach (var entry in archive.Entries)
            {
                if (entry.Key.StartsWith(folderPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    string relativePath = entry.Key.Substring(folderPath.Length).TrimStart('/');
                    string tempFolder = Path.Combine(tempRootFolder, relativePath);

                    if (entry.IsDirectory)
                    {
                        if (!Directory.Exists(tempFolder))
                            Directory.CreateDirectory(tempFolder);
                    }
                    else
                    {
                        string tempFilePath = Path.Combine(tempRootFolder, relativePath);
                        string tempFileDir = Path.GetDirectoryName(tempFilePath);

                        if (!Directory.Exists(tempFileDir))
                            Directory.CreateDirectory(tempFileDir);

                        entry.WriteToFile(tempFilePath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                        tempFiles?.Add(tempFilePath);
                    }
                }
            }

            foldersCollection.Add(tempRootFolder);
        }

        private void ExtractEntry(IArchiveEntry entry, List<FileInfo> filesCollection, List<string> tempFiles)
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "SearchArchiveTemp");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            string extractedFilePath = Path.Combine(tempFolder, entry.Key);

            if (!File.Exists(extractedFilePath))
            {
                entry.WriteToDirectory(tempFolder, new ExtractionOptions()
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }

            filesCollection.Add(new FileInfo(extractedFilePath));
            tempFiles?.Add(extractedFilePath);
        }

        private bool IsArchiveFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".zip" || ext == ".7z" || ext == ".rar";
        }

        public static void CleanupTempFiles()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "SearchArchiveTemp");

            try
            {
                // Delete all files in the directory
                foreach (var file in Directory.GetFiles(tempFolder))
                {
                    File.Delete(file);
                }

                // Delete all subdirectories in the directory
                foreach (var dir in Directory.GetDirectories(tempFolder))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error deleting temp files: {ex.Message}");
            }
        }

        public static string UnarchiveToTempFolder(string archivePath, string[] foldersToExclude)
        {
            string tempRootFolder = Path.Combine(Path.GetTempPath(), "SearchArchiveTemp", Path.GetFileNameWithoutExtension(archivePath));

            if (File.Exists(archivePath))
            {
                // Determine the archive type
                string extension = Path.GetExtension(archivePath).ToLower();

                switch (extension)
                {
                    case ".zip":
                        UnzipArchive(archivePath, tempRootFolder, foldersToExclude);
                        break;

                    case ".7z":
                        UnarchiveWithSevenZipSharpCompress(archivePath, tempRootFolder, foldersToExclude);
                        break;
                    case ".rar":
                        UnarchiveWithSharpCompress(archivePath, tempRootFolder, foldersToExclude);
                        break;

                    default:
                        throw new NotSupportedException($"The archive type '{extension}' is not supported.");
                }
                return tempRootFolder;
            }
            return string.Empty;
        }

        private static void UnzipArchive(string archivePath, string destinationPath, string[] foldersToExclude)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Skip folders in the exclude list
                    if (ShouldExclude(entry.FullName, foldersToExclude))
                        continue;

                    string destinationFilePath = Path.Combine(destinationPath, entry.FullName);
                    string destinationDirectory = Path.GetDirectoryName(destinationFilePath);

                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    if (!entry.FullName.EndsWith("/")) // Not a directory
                    {
                        entry.ExtractToFile(destinationFilePath, overwrite: true);
                    }
                }
            }
        }

        private static void UnarchiveWithSharpCompress(string archivePath, string destinationPath, string[] foldersToExclude)
        {
            using (Stream stream = File.OpenRead(archivePath))
            {
                IArchive archive = ArchiveFactory.Open(stream);

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        // Skip folders in the exclude list
                        if (ShouldExclude(entry.Key, foldersToExclude))
                            continue;

                        string destinationFilePath = Path.Combine(destinationPath, entry.Key);
                        string destinationDirectory = Path.GetDirectoryName(destinationFilePath);

                        if (!Directory.Exists(destinationDirectory))
                        {
                            Directory.CreateDirectory(destinationDirectory);
                        }

                        entry.WriteToFile(destinationFilePath);
                    }
                }
            }
        }

        private static void UnarchiveWithSevenZipSharpCompress(string archivePath, string destinationPath, string[] foldersToExclude)
        {
            using (var archive = SevenZipArchive.Open(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.IsDirectory || ShouldExclude(entry.Key, foldersToExclude))
                        continue;

                    string destinationFilePath = Path.Combine(destinationPath, entry.Key);
                    string destinationDirectory = Path.GetDirectoryName(destinationFilePath);

                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    entry.WriteToFile(destinationFilePath);
                }
            }
        }

        private static bool ShouldExclude(string entryPath, string[] foldersToExclude)
        {
            // Normalize entryPath for comparison
            string normalizedPath = entryPath.TrimEnd('/').ToLower();

            // Check if the path contains any of the excluded folder names
            foreach (var folder in foldersToExclude)
            {
                string normalizedFolder = folder.TrimEnd('/').ToLower();
                // Match if the entry path contains the excluded folder name followed by a separator or end of path
                if (normalizedPath.Contains($"/{normalizedFolder}/") || normalizedPath.EndsWith($"/{normalizedFolder}"))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
