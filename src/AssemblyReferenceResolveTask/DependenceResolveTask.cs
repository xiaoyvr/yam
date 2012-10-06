using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Yam.Core;

namespace ReferenceResolveTask
{
    public class DependenceResolveTask : Task
    {
        [Required]
        public ITaskItem[] InputProjects { get; set; }

        [Required]
        public ITaskItem ConfigFile { get; set; }

        public bool Reverse { get; set; }

        public ITaskItem[] ExcludeNodes { get; set; }

        public ITaskItem[] EndNodes { get; set; }

        public ITaskItem RuntimeProfile { get; set; }

        [Output]
        public ITaskItem[] Dependences { get; private set; }

        public override bool Execute()
        {
            var excludes = (ExcludeNodes??new ITaskItem[0]).Select(t => t.GetMetadata("FullPath")).ToArray();
            var resolveConfig = new ResolveConfig(ConfigFile.ItemSpec, Path.GetDirectoryName(BuildEngine2.ProjectFileOfTaskNode));
            var inputNodePaths = InputProjects.Select(t => t.GetMetadata("FullPath")).ToArray();
            var endNodePaths = (EndNodes??new ITaskItem[0]).Select(n => n.GetMetadata("FullPath")).ToArray();
            var runtimeProfile = RuntimeProfile == null ? string.Empty : RuntimeProfile.ItemSpec;
            var result = new DependencyResolver(resolveConfig, excludes, Reverse).Resolve(inputNodePaths, endNodePaths, runtimeProfile);
            Dependences = result.Select(CreateTaskItem).ToArray();
            return true;
        }

        private static ITaskItem CreateTaskItem(KeyValuePair<string, string[]> de)
        {
            var taskItem = new TaskItem(de.Key);
            taskItem.SetMetadata("To", string.Join(";", de.Value));
            return taskItem;
        }
    }
}