using System;
using System.IO;

namespace Yam.Core
{
    internal class RefinablePath
    {
        private readonly string name;
        private readonly string[] paths;

        public RefinablePath(string name, string[] paths)
        {
            this.name = name;
            this.paths = paths;
        }

        public string Refine(string hintPath, string projectDirName)
        {
            if (!HasPath())
            {
                return string.Empty;
            }
            if (IsClear())
            {
                return paths[0];
            }

            if (NoClue(hintPath) && HasPath())
            {
                throw new ApplicationException(string.Format("cannot determine which path should use. {0} ? {1}", name, string.Join(" : ", paths)));
            }
            return Clarify(hintPath, projectDirName);
        }

        private bool HasPath()
        {
            return paths.Length > 0;
        }

        private bool IsClear()
        {
            return paths.Length == 1;
        }

        private static string Clarify(string hintPath, string prjectDirName)
        {
            return Path.GetFullPath(Path.Combine(prjectDirName, hintPath));
        }

        private static bool NoClue(string hintPath)
        {
            return string.IsNullOrEmpty(hintPath);
        }
    }
}