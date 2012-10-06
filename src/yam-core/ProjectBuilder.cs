using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Yam.Core
{
    public class ProjectBuilder
    {
        private readonly string projectFullPath;
        private readonly IDictionary<string, ProjectNode> cache;

        private readonly string runtimeProfile;
        private readonly ResolveConfig resolveConfig;

        public ProjectNode BuildUp()
        {
            if (!cache.ContainsKey(projectFullPath))
            {
                var extractor = new ProjectExtractor(projectFullPath, resolveConfig);
                var project = new ProjectNode
                                  {
                                      Id = extractor.GetId(),
                                      AssemblyReferences = GetAssemblyReferences(extractor),
                                      ProjectReferences = GetProjectReferences(extractor),
                                      RuntimeReferences = GetRuntimeReferences(extractor),
                                      FullPath = projectFullPath, 
                                      Output = resolveConfig.GetAssemblyNameByProjectPath(projectFullPath)
                                  };
                cache[projectFullPath] = project;
            }
            return cache[projectFullPath];
        }

        public ProjectBuilder(string projectPath, IDictionary<string, ProjectNode> cache, string runtimeProfile, ResolveConfig resolveConfig)
        {
            this.cache = cache;
            this.runtimeProfile = runtimeProfile;
            this.resolveConfig = resolveConfig;
            projectFullPath = projectPath;
        }

        private AssemblyReference[] GetRuntimeReferences(ProjectExtractor extractor)
        {
            var references = extractor.GetCommonRuntimeReferenceNodes();
            if (!string.IsNullOrEmpty(runtimeProfile))
            {
                var profileReferences = extractor.GetRuntimeReferenceNodes(runtimeProfile);
                references = references.Concat(profileReferences);
            }
            else
            {
                var defaultReferences = extractor.GetDefaultRuntimeReferenceNodes();
                references = references.Concat(defaultReferences);
            }

            return references.Select(r => CreateAssemblyReference(r, ()=> true))
                .Where(ar => ar.AssemblyReferneceType != AssemblyReferneceType.None).ToArray();
        }

        private AssemblyReference[] GetAssemblyReferences(ProjectExtractor extractor)
        {
            var references = extractor.GetAssemblyReferenceNodes();
            return references.Select(r => CreateAssemblyReference(r, () => r.IsPrivate))
                .Where(ar => ar.AssemblyReferneceType != AssemblyReferneceType.None).ToArray();
        }

        private AssemblyReference CreateAssemblyReference(ReferenceNode node, Func<bool> isPrivateGetter)
        {
            var name = node.Include;
            var hintPath = node.HintPath;

            if (resolveConfig.IsManagedProject(name))
            {
                return new AssemblyReference
                           {
                               ReferenceName = name,
                               CopyLocal = isPrivateGetter(),
                               AssemblyReferneceType = AssemblyReferneceType.Project
                           };
            }

            var projectDirName = Path.GetDirectoryName(projectFullPath);
            if (resolveConfig.IsManagedLib(name, hintPath, projectDirName))
            {
                return new AssemblyReference
                           {
                               ReferenceName = name,
                               CopyLocal = isPrivateGetter(),
                               FullPath = resolveConfig.GetLibPath(name, hintPath, projectDirName),
                               AssemblyReferneceType = AssemblyReferneceType.Lib
                           };                
            }

            return new AssemblyReference
                           {
                               ReferenceName = name,
                               CopyLocal = isPrivateGetter(),
                               AssemblyReferneceType = AssemblyReferneceType.None
                           };                
        }

        private ProjectReference[] GetProjectReferences(ProjectExtractor extractor)
        {
            var nodesByXPath = extractor.GetProjectReferenceNodes(); 
            return nodesByXPath.Select(CreateProjectReference).ToArray();
        }

        private ProjectReference CreateProjectReference(ReferenceNode node)
        {
            var fullPath = GetFullPath(node.Include);
            var project = new ProjectBuilder(fullPath, cache, runtimeProfile, resolveConfig).BuildUp();
            return new ProjectReference {Node = project, CopyLocal = node.IsPrivate};
        }

        private string GetFullPath(string path)
        {
            var directoryName = Path.GetDirectoryName(projectFullPath);
            Debug.Assert(directoryName != null, "directoryName != null");
            return Path.IsPathRooted(path) ? Path.GetFullPath(path) 
                       : Path.GetFullPath(Path.Combine(directoryName, path));
        }
    }
}