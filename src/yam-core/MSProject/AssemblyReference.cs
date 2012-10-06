namespace Yam.Core.MSProject
{
    internal class AssemblyReference
    {
        public string FullPath { get; set; }
        public string ReferenceName { get; set; }
        public string SimpleName
        {
            get
            {
                return ReferenceName.Split(',')[0].Trim();
            }
        }
        public bool CopyLocal { get; set; }

        public AssemblyReferneceType AssemblyReferneceType { get; set; }
    }

    internal enum AssemblyReferneceType
    {
        None,
        Project,
        Lib
    }
}