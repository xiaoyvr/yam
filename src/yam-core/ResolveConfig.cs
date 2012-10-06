using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Yam.Core
{
    public class ResolveConfig
    {
        public const string LIB = "lib";
        public const string PROJECT = "project";
        private readonly string rootDir;
        private readonly HashSet<string> gacSet;
        private readonly IDictionary<string, string> assemblyToProjectMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly LibConfig libConfig = new LibConfig();

        public ResolveConfig(string configFile, string rootDir)
        {
            this.rootDir = rootDir;
            this.gacSet = GacUtil.GetGacSet();
            InitConfig(configFile);
        }

        public string[] GetProjects()
        {
            return assemblyToProjectMapping.Values.ToArray();
        }

        private void InitConfig(string configFile)
        {
            var lines = File.ReadAllLines(configFile);
            lines.Where(line => line.Trim() != string.Empty).ToList().ForEach(AddToMap);
        }

        private void AddToMap(string line)
        {
            var strings = line.Split(',');
            var dispatch = strings[0].Trim();
            var assemblyName = strings[1].Trim();
            var fileFullName = ToFullName(strings[2].Trim());

            if (dispatch.ToLower() == PROJECT)
            {
                if(assemblyToProjectMapping.ContainsKey(assemblyName))
                {
                    throw new ApplicationException(string.Format("Configuration error: duplicated project definition in configuration file. \nProject [{0}] already defined, it's mapped to {1}", assemblyName, assemblyToProjectMapping[assemblyName]));
                }
                assemblyToProjectMapping.Add(assemblyName, fileFullName);
            }            
            if (dispatch.ToLower() == LIB)
            {
                libConfig.AddPath(fileFullName);
            }
        }

        private string ToFullName(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(rootDir, relativePath));
        }


        public string[] GetProjectPathsByAssemblyNames(IEnumerable<string> assemblyFullNames)
        {
            return assemblyFullNames
                .Select(GetProjectPathByAssemblyName)
                .Where(p=> !string.IsNullOrEmpty(p))
                .ToArray();
        }

        public string GetProjectPathByAssemblyName(string name)
        {
            return assemblyToProjectMapping.ContainsKey(name) ? assemblyToProjectMapping[name] : string.Empty;
        }

        public string GetAssemblyNameByProjectPath(string path)
        {
            try
            {
                var keyValuePair = assemblyToProjectMapping.First(k => k.Value.Equals(path, StringComparison.OrdinalIgnoreCase));
                return keyValuePair.Key;
            }
            catch (Exception)
            {
                Console.WriteLine(path + " has no mapping dll. ");
                return string.Empty;
            }
        }

        public string GetLibPath(string name, string hintPath, string projectDirName)
        {
            return libConfig.GetLibPath(name, hintPath, projectDirName);
        }

        public bool IsGAC(string name)
        {
            return gacSet.Contains(name);
        }

        public bool IsManagedProject(string name)
        {
            return !string.IsNullOrEmpty(name) && assemblyToProjectMapping.ContainsKey(name);
        }

        public bool IsManagedLib(string name, string hintPath, string projectDirName)
        {
            return libConfig.IsManagedLib(name, hintPath, projectDirName);
        }

        public bool IsManaged(string name, string hintPath, string projectDirName)
        {
            return IsManagedProject(name) || IsManagedLib(name, hintPath, projectDirName);
        }
    }

    public class LibConfig
    {
        private readonly HashSet<string> paths = new HashSet<string>();

        public bool IsManagedLib(string name, string hintPath, string projectDirName)
        {
            return !string.IsNullOrEmpty(GetLibPath(name, hintPath, projectDirName));
        }
        public string GetLibPath(string name, string hintPath, string projectDirName)
        {
            return new RefinablePath(name, GetPaths(name)).Refine(hintPath, projectDirName);
        }

        private string[] GetPaths(string name)
        {
            return paths.Where(p => name.Equals(Path.GetFileNameWithoutExtension(p), 
                StringComparison.InvariantCultureIgnoreCase)).ToArray();
        }

        public void AddPath(string path)
        {
            paths.Add(path);
        }
    }
}