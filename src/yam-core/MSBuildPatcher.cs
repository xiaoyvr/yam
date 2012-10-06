using System.Collections.Generic;
using System.Linq;

namespace Yam.Core
{
    public class MSBuildPatcher
    {
        private readonly ResolveConfig resolveConfig;

        public MSBuildPatcher(ResolveConfig resolveConfig)
        {
            this.resolveConfig = resolveConfig;
        }

        public MSBuildPatch Resolve(string[] inputs, string runtimeProfile = "")
        {
            var resolveContext = new ResolveContext(resolveConfig, new Dictionary<string, ProjectNode>(), runtimeProfile);
            var projects = resolveContext.GetValidProjects(inputs);
            var dag = new ProjectDAGBuilder(resolveContext, projects).BuildDAG();
            var copyItemSets = CollectCopies(projects, dag, resolveContext);
            dag.Simplify();
            var compileProjects = dag.Out().OfType<ProjectNode>().ToArray();

            return new MSBuildPatch(compileProjects, copyItemSets);
        }

        private static CopyItemSet[] CollectCopies(IEnumerable<ProjectNode> projects, DAG<IDependencyNode> dag, ResolveContext context)
        {
            return projects
                .Select(p => CreateCopyLocalSet(p, context))
                .Where(c => c.CopyLocalSources.Length > 0)
                .ToArray()
                .Select(cls => cls.CreateCopyTaskItem(dag))
                .ToArray();
        }

        private static CopyLocalSet CreateCopyLocalSet(ProjectNode p, ResolveContext context)
        {
            return new CopyLocalSet
            {
                Dest = p,
                ProjectCopySource = context.GetProjectReferencesNeedCopy(p).ToArray(),
                AssemblyCopySource = context.GetAssemblyReferencesNeedCopy(p).ToArray(),
                RuntimeCopySources = context.GetRuntimeReferencesNeedCopy(p).ToArray()
            };
        }

    }

    public class MSBuildPatch
    {
        public MSBuildPatch(ProjectNode[] compileProjects, CopyItemSet[] copyItemSets)
        {
            CompileProjects = compileProjects;
            CopyItemSets = copyItemSets;
        }

        public ProjectNode[] CompileProjects { get; private set; }
        public CopyItemSet[] CopyItemSets { get; private set; }
    }
}