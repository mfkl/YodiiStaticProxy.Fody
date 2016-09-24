using System;
using System.IO;
using System.Linq;

namespace YodiiStaticProxy.Fody.Finders
{
    public static class Util
    {
        static string _assemblyFolderPath;
        
        public static string ProxyAssemblyFolderPath
        {
            get
            {
                if(string.IsNullOrEmpty(_assemblyFolderPath))
                    _assemblyFolderPath = GetProxyAssemblyFolderPath();
                return _assemblyFolderPath;
            }
        }

        /// <summary>
        /// Creates lib folder at solution level unless it already exists.
        /// </summary>
        /// <returns>lib folder full path</returns>
        static string GetProxyAssemblyFolderPath()
        {
            var solutionDir = TryGetSolutionDirectoryInfo();
            var libDir = Path.Combine(solutionDir.FullName, "lib");
            Directory.CreateDirectory(libDir);
            return libDir;
        }
        
        /// <summary>
        /// Gets the DirectoryInfo at the solution level
        /// </summary>
        static DirectoryInfo TryGetSolutionDirectoryInfo()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while(directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }
    }
}
