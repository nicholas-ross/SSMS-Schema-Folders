namespace SsmsSchemaFolders
{
    using Localization;

    using Microsoft.VisualStudio.Shell;

    public class SchemaFolderOptions : DialogPage, ISchemaFolderOptions
    {
        [CategoryResources(nameof(SchemaFolderOptions) + "Active")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Enabled))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Enabled))]
        public bool Enabled { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(AppendDot))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(AppendDot))]
        public bool AppendDot { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(CloneParentNode))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(CloneParentNode))]
        public bool CloneParentNode { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(UseObjectIcon))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(UseObjectIcon))]
        public bool UseObjectIcon { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "ObjectDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(RenameNode))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(RenameNode))]
        public bool RenameNode { get; set; } = false;

    }
}
