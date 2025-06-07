using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageAnalyzer.Core.FileSystem
{
    /// <summary>
    /// The File Provider
    /// </summary>
    public interface IFileProvider
    {
        List<FileInfo> FindFile(string filePath, string fileName, bool returnFirst = false);
        string FindFolder(string directoryPath, string targetFolderName);

    }
}
