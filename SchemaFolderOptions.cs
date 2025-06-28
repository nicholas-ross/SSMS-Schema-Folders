namespace SsmsSchemaFolders
{
    using Localization;
    using Microsoft.VisualStudio.Shell;
    using System.ComponentModel;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;

    public class SchemaFolderOptions : DialogPage, ISchemaFolderOptions
    {
        [CategoryResources(nameof(SchemaFolderOptions) + "Active")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Enabled))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Enabled))]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "Active")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(EnabledModifierKeys))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(EnabledModifierKeys))]
        [DefaultValue(Keys.Control)]
        //[Editor(typeof(ShortcutKeysEditor), typeof(UITypeEditor))]
        public Keys EnabledModifierKeys { get; set; } = Keys.Control;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(AppendDot))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(AppendDot))]
        [DefaultValue(true)]
        public bool AppendDot { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(CloneParentNode))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(CloneParentNode))]
        [DefaultValue(true)]
        public bool CloneParentNode { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(UseObjectIcon))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(UseObjectIcon))]
        [DefaultValue(true)]
        public bool UseObjectIcon { get; set; } = true;

        [CategoryResources(nameof(SchemaFolderOptions) + "ObjectDisplayOptions")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(RenameNode))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(RenameNode))]
        [DefaultValue(false)]
        public bool RenameNode { get; set; } = false;

        [CategoryResources(nameof(SchemaFolderOptions) + "Performance")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(QuickSchema))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(QuickSchema))]
        [DefaultValue(0)]
        public int QuickSchema { get; set; } = 0;

        [CategoryResources(nameof(SchemaFolderOptions) + "Performance")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(UnresponsiveTimeout))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(UnresponsiveTimeout))]
        [DefaultValue(200)]
        public int UnresponsiveTimeout { get; set; } = 200;

        [CategoryResources(nameof(SchemaFolderOptions) + "Performance")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(UseClear))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(UseClear))]
        [DefaultValue(0)]
        public int UseClear { get; set; } = 0;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel1")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level1FolderType))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level1FolderType))]
        [DefaultValue(FolderType.Schema)]
        public FolderType Level1FolderType { get; set; } = FolderType.Schema;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel1")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level1MinNodeCount))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level1MinNodeCount))]
        [DefaultValue(0)]
        public int Level1MinNodeCount { get; set; } = 0;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel1")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level1Regex))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level1Regex))]
        [DefaultValue("")]
        public string Level1Regex { get; set; } = "";

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel1")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level1GroupNonMatchingAsOther))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level1GroupNonMatchingAsOther))]
        [DefaultValue(false)]
        public bool Level1GroupNonMatchingAsOther { get; set; } = false;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel2")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level2FolderType))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level2FolderType))]
        [DefaultValue(FolderType.None)]
        public FolderType Level2FolderType { get; set; } = FolderType.None;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel2")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level2MinNodeCount))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level2MinNodeCount))]
        [DefaultValue(0)]
        public int Level2MinNodeCount { get; set; } = 0;

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel2")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level2Regex))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level2Regex))]
        [DefaultValue("")]
        public string Level2Regex { get; set; } = "";

        [CategoryResources(nameof(SchemaFolderOptions) + "FolderLevel2")]
        [DisplayNameResources(nameof(SchemaFolderOptions) + nameof(Level2GroupNonMatchingAsOther))]
        [DescriptionResources(nameof(SchemaFolderOptions) + nameof(Level2GroupNonMatchingAsOther))]
        [DefaultValue(false)]
        public bool Level2GroupNonMatchingAsOther { get; set; } = false;
    }
}
