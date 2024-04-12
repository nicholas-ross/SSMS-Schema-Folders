using System.Windows.Forms;

namespace SsmsSchemaFolders
{
    public interface IObjectExplorerExtender
    {
        bool GetNodeExpanding(TreeNode node);
        string GetNodeUrnPath(TreeNode node);
        TreeView GetObjectExplorerTreeView();
        void RenameNode(TreeNode node);
        int ReorganizeNodes(TreeNode node, string nodeTag, bool expanding);
    }
}
