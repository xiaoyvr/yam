using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Yam.Core;

namespace ReferenceResolveTask
{
    public class CopyLocalDetectionTask : Task
    {
        [Required]
        public ITaskItem ConfigFile { get; set; }
        [Required]
        public ITaskItem RootDir { get; set; }

        public ITaskItem[] DeployHints { get; set; }

        [Output]
        public ITaskItem[] ProjectCopyLocals { get; private set; }

        public override bool Execute()
        {
            var config = new ResolveConfig(ConfigFile.ItemSpec, RootDir.ItemSpec);
            var copyLocalDetector = new CopyLocalDetector(DeployHints.Select(dh => dh.ItemSpec).ToArray(), config);
            ProjectCopyLocals = copyLocalDetector.Dectect().Select(CreateProjectCopyLocal).ToArray();
            return true;
        }

        private static ITaskItem CreateProjectCopyLocal(ProjectCopyLocal projectCopyLocal)
        {
            var copies = projectCopyLocal.CopyLocals;
            var result = new TaskItem(projectCopyLocal.Project);
            result.SetMetadata("CopyLocals", string.Join(Environment.NewLine, copies.ToArray()));    
            return result;
        }
    }
}