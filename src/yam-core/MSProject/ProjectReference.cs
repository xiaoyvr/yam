namespace Yam.Core.MSProject
{
    internal class ProjectReference
    {
        public ProjectNode Node
        {
            get;
            set;
        }

        public bool CopyLocal { get; set; }
    }
}