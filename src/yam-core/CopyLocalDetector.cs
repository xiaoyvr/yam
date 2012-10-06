using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Yam.Core.MSProject;

namespace Yam.Core
{
    public class CopyLocalDetector
    {
        private readonly string[] deployHints;
        private readonly ResolveConfig resolveConfig;

        public CopyLocalDetector(string[] deployHints, ResolveConfig resolveConfig)
        {
            this.resolveConfig = resolveConfig;
            this.deployHints = deployHints;
        }

        public List<ProjectCopyLocal> Dectect()
        {
            return resolveConfig.GetProjects().Where(p => !IsDeployNode(p))
                .Select(p => new ProjectCopyLocal(p, GetCopyLocals(resolveConfig, p)))
                .Where(cl => cl.CopyLocals.Length > 0).ToList();
        }

        private bool IsDeployNode(string project)
        {
            var directoryName = Path.GetDirectoryName(project);
            return directoryName != null && deployHints.Any(h => File.Exists(Path.Combine(directoryName, h)));
        }

        private static string[] GetCopyLocals(ResolveConfig config, string project)
        {
            var extractor = new ProjectExtractor(project, config);
            var projectReferenceNodes = extractor.GetProjectReferenceNodes().ToArray();
            Array.ForEach(projectReferenceNodes, n => n.Include = config.GetAssemblyNameByProjectPath(ToFullPath(n.Include, project)));

            var nodes = extractor.GetAssemblyReferenceNodes()
               .Concat(projectReferenceNodes.Where(n => !string.IsNullOrEmpty(n.Include)))
               .Where(n => n.IsPrivate && config.IsManaged(n.Include, n.HintPath, project));

            return nodes.Select(n => n.Include).ToArray();
        }

        private static string ToFullPath(string include, string project)
        {
            var projectDir = Path.GetDirectoryName(project);
            Debug.Assert(projectDir != null, "projectDir != null");
            return Path.GetFullPath(Path.Combine(projectDir, include));
        }
    }

    public class ProjectCopyLocal
    {
        public ProjectCopyLocal(string project, string[] copyLocals)
        {
            Project = project;
            CopyLocals = copyLocals;
        }

        public string Project { get; private set; }
        public string[] CopyLocals { get; private set; }
    }
}