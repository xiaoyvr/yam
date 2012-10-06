namespace Yam.Core
{
    public class AssemblyReference
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
    public enum AssemblyReferneceType
    {
        None,
        Project,
        Lib
    }
}