using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace SsmsSchemaFolders
{
    public class SchemaFolderOptions : DialogPage, ISchemaFolderOptions
    {
        [Category("Active")]
        [DisplayName("Enabled")]
        [Description("Group sql objects in Object Explorer (tables, views, etc.) into schema folders.")]
        public bool Enabled { get; set; } = true;

        [Category("Folder Display Options")]
        [DisplayName("Append Dot")]
        [Description("Add a dot after the schema name on the folder label. ")]
        public bool AppendDot { get; set; } = true;

        [Category("Folder Display Options")]
        [DisplayName("Clone Parent Node")]
        [Description("Add the right click and connection properties of the parent node to the schema folder node.")]
        public bool CloneParentNode { get; set; } = true;

        [Category("Folder Display Options")]
        [DisplayName("Use Object Icon")]
        [Description("Use the icon of the last child node as the folder icon. If false then use the parent node (i.e. folder) icon.")]
        public bool UseObjectIcon { get; set; } = true;

        [Category("Object Display Options")]
        [DisplayName("Rename Node")]
        [Description("Remove the schema name from the object node.")]
        public bool RenameNode { get; set; } = false;

        [Category("Folder Level 1")]
        [DisplayName("Folder Type")]
        [Description("The type of sorting to use to create the folders.")]
        public FolderType Level1FolderType { get; set; } = FolderType.Schema;

        [Category("Folder Level 1")]
        [DisplayName("Minimum Node Count")]
        [Description("Sort nodes into folders only when it contains at least this many nodes.")]
        public int Level1MinNodeCount { get; set; } = 0;

        [Category("Folder Level 2")]
        [DisplayName("Folder Type")]
        [Description("The type of sorting to use to create the folders.")]
        public FolderType Level2FolderType { get; set; } = FolderType.Alphabetical;

        [Category("Folder Level 2")]
        [DisplayName("Minimum Node Count")]
        [Description("Sort nodes into folders only when it contains at least this many nodes.")]
        public int Level2MinNodeCount { get; set; } = 200;

    }
}
