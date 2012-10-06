using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Yam.Core;

namespace ReferenceResolveTask
{
    public class ResolveWithCopiesTask : Task
    {
        [Required]
        public ITaskItem[] InputProjects { get; set; }

        [Required]
        public ITaskItem ConfigFile { get; set; }

        public ITaskItem RuntimeProfile { get; set; }

        [Output]
        public ITaskItem[] OutProjects { get; private set; }

        [Output]
        public ITaskItem[] Copies { get; private set; }

        public override bool Execute()
        {
            var rootDir = Path.GetDirectoryName(BuildEngine2.ProjectFileOfTaskNode);
            var resolveConfig = new ResolveConfig(ConfigFile.ItemSpec, rootDir, GacUtil.GetGacSet());
            var runtimeProfile = RuntimeProfile == null ? String.Empty : RuntimeProfile.ItemSpec;
            var msBuildPatch = new MSBuildPatcher(resolveConfig).Resolve(InputProjects.Select(t => t.GetMetadata("FullPath")).ToArray(), runtimeProfile);
            Copies = msBuildPatch.CopyItemSets.Select(CreateCopyTaskItem).ToArray();
            OutProjects = msBuildPatch.CompileProjects.Select(CreateProjectItem).ToArray();
            return true;
        }

        private static ITaskItem CreateCopyTaskItem(CopyItemSet copyItemSet)
        {
            var taskItem = new TaskItem(copyItemSet.DestProject, new Dictionary<string,string>
                {
                    {"CopyItemProjects", ToPaths(copyItemSet.ItemProjects)}, 
                    {"CopyProjects", ToPaths(copyItemSet.Projects)}, 
                    {"CopyLibs", ToPaths(copyItemSet.Libs)}, 
                });
            return taskItem;
        }

        private static ITaskItem CreateProjectItem(ProjectNode node)
        {
            return new TaskItem(node.FullPath, new Dictionary<string, string>
                {
                    { "AssemblyName", node.Output }
                });
        }

        public static string ToPaths(IEnumerable<string> paths)
        {
            return String.Join(";", paths.ToArray());
        }
    }
}