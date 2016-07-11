using Microsoft.VisualStudio.Shell;

namespace SsmsSchemaFolders
{
    public class SchemaFolderOptions : DialogPage
    {
        public bool Enabled { get; set; } = true;
        public bool AppendDot { get; set; } = true;
        public bool CloneParentNode { get; set; } = true;
        public bool UseObjectIcon { get; set; } = true;
    }
}
