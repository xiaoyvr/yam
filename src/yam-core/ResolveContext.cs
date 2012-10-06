using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Yam.Core
{
    public class ResolveContext
    {
        private ResolveConfig ResolveConfig { get; set; }
        private IDictionary<string, ProjectNode> ProjectCache { get; set; }
        private readonly string runtimeProfile;

        public ResolveContext(ResolveConfig resolveConfig, IDictionary<string, ProjectNode> projectCache, string runtimeProfile)
        {
            ResolveConfig = resolveConfig;
            ProjectCache = projectCache;
            this.runtimeProfile = runtimeProfile;
        }

        public IDependencyNode LoadAssembly(AssemblyReference assemblyReference)
        {
            if (assemblyReference.AssemblyReferneceType == AssemblyReferneceType.Lib )
            {
                return new AssemblyNode {FullPath = assemblyReference.FullPath};
            }
            var path = ResolveConfig.GetProjectPathByAssemblyName(assemblyReference.SimpleName);
            return new ProjectBuilder(path, ProjectCache, runtimeProfile, ResolveConfig).BuildUp();
        }

        public IEnumerable<IDependencyNode> GetReferencesNeedCopy(ProjectNode dest)
        {
            var projectSources = dest.ProjectReferences.Where(r => r.CopyLocal)
                .Select(r => (IDependencyNode)r.Node);
            var assemblySources = dest.AssemblyReferences.Where(r => r.CopyLocal)
                .Select(LoadAssembly);
            return projectSources.Concat(assemblySources);
        }

        public IEnumerable<ProjectNode> GetProjectReferencesNeedCopy(ProjectNode dest)
        {
            return dest.ProjectReferences.Where(r => r.CopyLocal)
                .Select(r => r.Node);
        }

        public IEnumerable<IDependencyNode> GetAssemblyReferencesNeedCopy(ProjectNode dest)
        {
            return dest.AssemblyReferences.Where(r => r.CopyLocal).Select(LoadAssembly);
        }

        public IEnumerable<IDependencyNode> GetRuntimeReferencesNeedCopy(ProjectNode dest)
        {
            return dest.RuntimeReferences.Select(LoadAssembly);
        }

        private ProjectNode[] GetProjects(string[] paths)
        {
            return paths.Select(p => new ProjectBuilder(p, ProjectCache, runtimeProfile, ResolveConfig).BuildUp()).ToArray();
        }

        public virtual ProjectNode[] GetAllProjects()
        {
            return ResolveConfig.GetProjects().Select(p => new ProjectBuilder(p, ProjectCache, runtimeProfile, ResolveConfig).BuildUp()).ToArray();
        }

        public IDependencyNode[] LoadAssemblyNodes(IEnumerable<AssemblyReference> assemblyReferences)
        {
            return assemblyReferences.Select(LoadAssembly).ToArray();
        }

        public ProjectNode[] GetValidProjects(string[] inputs)
        {
            var projectPaths = inputs.Where(IsVaildProject).ToArray();
            return GetProjects(projectPaths);
        }

        private static bool IsVaildProject(string projectPath)
        {
            var extension = Path.GetExtension(projectPath);
            return !string.IsNullOrEmpty(extension) && extension.ToLower() != ".dbp";
        }
    }
}