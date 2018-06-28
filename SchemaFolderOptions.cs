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

    }
}
