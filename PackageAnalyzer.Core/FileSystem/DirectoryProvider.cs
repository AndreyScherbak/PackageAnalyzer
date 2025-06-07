using Serilog;

namespace PackageAnalyzer.Core.FileSystem
{
    public class DirectoryProvider : IFileProvider
    {
        private List<FileInfo> filesCollection;

        public DirectoryProvider()
        {
            filesCollection = new List<FileInfo>();
        }

        public List<FileInfo> FindFile(string directoryPath, string targetFileName, bool returnFirst = false)
        {
            try
            {
                string[] files = Directory.GetFiles(directoryPath, targetFileName, SearchOption.AllDirectories);

                // If no exact match is found, try wildcard search
                if (files.Length == 0)
                {
                    //string directory = Path.GetDirectoryName(directoryPath);
                    string searchPattern = Path.GetFileName(targetFileName);
                    files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
                }

                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        filesCollection.Add(new FileInfo(file));
                        if (returnFirst)
                            break; // Stop after adding the first file if returnFirst is true
                    }
                    return filesCollection;
                }
                else
                {
                    Log.Information($"Files not found matching '{targetFileName}' in the current directory or its subdirectories.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error searching for file in the directory: {ex.Message}\r\n{ex.StackTrace}");
                throw new Exception($"Error searching for file in the directory: {ex.Message}", ex);
            }
        }

        public string FindFolder(string directoryPath, string targetFolderName)
        {
            try
            {
                string[] directories = Directory.GetDirectories(directoryPath, targetFolderName, SearchOption.AllDirectories);

                if (directories.Length > 0)
                {
                    return directories[0]; // Return the first match found
                }
                else
                {
                    Log.Information($"Folder not found matching '{targetFolderName}' in the current directory or its subdirectories.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error searching for folder in the directory: {ex.Message}\r\n{ex.StackTrace}");
                throw new Exception($"Error searching for folder in the directory: {ex.Message}", ex);
            }
        }
    }
}
