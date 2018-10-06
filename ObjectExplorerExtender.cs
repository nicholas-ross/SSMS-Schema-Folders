using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Collections.Generic;
//using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows.Forms;

namespace SsmsSchemaFolders
{
    /// <summary>
    /// Used to organize Databases and Tables in Object Explorer into groups
    /// </summary>
    public class ObjectExplorerExtender : IObjectExplorerExtender
    {

        private ISchemaFolderOptions Options { get; }
        private IServiceProvider Package { get; }
        //private Regex NodeSchemaRegex;


        /// <summary>
        /// 
        /// </summary>
        public ObjectExplorerExtender(IServiceProvider package, ISchemaFolderOptions options)
        {
            Package = package;
            Options = options;
            //NodeSchemaRegex = new Regex(@"@Schema='((''|[^'])+)'");
        }


        public string GetFolderName(TreeNode node, int folderLevel)
        {
            FolderType folderType = FolderType.None;
            switch (folderLevel)
            {
                case 1:
                    folderType = Options.Level1FolderType;
                    break;

                case 2:
                    folderType = Options.Level2FolderType;
                    break;
            }
            switch (folderType)
            {
                case FolderType.Schema:
                    return GetNodeSchema(node);

                case FolderType.Alphabetical:
                    var name = GetNodeName(node);
                    //debug_message("{0} > {1}", node.Text, name);

                    if (!string.IsNullOrEmpty(name))
                    {
                        return name.Substring(0, 1).ToUpper();
                    }
                    break;

            }
            return null;
        }

        private int GetFolderLevelMinNodeCount(int folderLevel)
        {
            switch (folderLevel)
            {
                case 1:
                    return Options.Level1MinNodeCount;

                case 2:
                    return Options.Level2MinNodeCount;
            }
            return 0;
        }

        /// <summary>
        /// Gets the underlying object which is responsible for displaying object explorer structure
        /// </summary>
        /// <returns></returns>
        public TreeView GetObjectExplorerTreeView()
        {
            var objectExplorerService = (IObjectExplorerService)Package.GetService(typeof(IObjectExplorerService));
            if (objectExplorerService != null)
            {
                var oesTreeProperty = objectExplorerService.GetType().GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (oesTreeProperty != null)
                    return (TreeView)oesTreeProperty.GetValue(objectExplorerService, null);
                //else
                //    debug_message("Object Explorer Tree property not found.");
            }
            //else
            //    debug_message("objectExplorerService == null");

            return null;
        }

        /// <summary>
        /// Gets node information from underlying type of tree node view
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <remarks>Copy of private method in ObjectExplorerService</remarks>
        private INodeInformation GetNodeInformation(TreeNode node)
        {
            INodeInformation result = null;
            IServiceProvider serviceProvider = node as IServiceProvider;
            if (serviceProvider != null)
            {
                result = (serviceProvider.GetService(typeof(INodeInformation)) as INodeInformation);
                //debug_message(node.Text);
                //debug_message("NodeInformation\n UrnPath:{0}\n Name:{1}\n InvariantName:{2}\n Context:{3}\n NavigationContext:{4}", result.UrnPath, result.Name, result.InvariantName, result.Context, result.NavigationContext);
            }
            return result;
        }

        public bool GetNodeExpanding(TreeNode node)
        {
            var lazyNode = node as ILazyLoadingNode;
            if (lazyNode != null)
                return lazyNode.Expanding;
            else
                return false;
        }

        public string GetNodeUrnPath(TreeNode node)
        {
            var ni = GetNodeInformation(node);
            if (ni != null)
                return ni.UrnPath;
            else
                return null;
        }

        private String GetNodeFullName(TreeNode node)
        {
            var ni = GetNodeInformation(node);
            if (ni != null)
            {
                return ni.InvariantName;
            }
            return null;
        }

        private String GetNodeName(TreeNode node)
        {
            var ni = GetNodeInformation(node);
            // Only return name if object is schema bound.
            if (ni != null && ni.Context.Contains("@Schema="))
            {
                return ni.Name;
            }
            return null;
        }

        private String GetNodeSchema(TreeNode node)
        {
            var ni = GetNodeInformation(node);
            if (ni != null)
            {
                // parse ni.Context = Server[@Name='NR-DEV\SQL2008R2EXPRESS']/Database[@Name='tempdb']/Table[@Name='test.''escape''[value]' and @Schema='dbo']
                // or compare ni.Name vs ni.InvariantName = ObjectName vs SchemaName.ObjectName

                //var match = NodeSchemaRegex.Match(ni.Context);
                //if (match.Success)
                //    return match.Groups[1].Value;

                if (ni.InvariantName.EndsWith("." + ni.Name))
                    return ni.InvariantName.Replace("." + ni.Name, String.Empty);
            }
            return null;
        }

        /// <summary>
        /// Removes schema name from object node.
        /// </summary>
        /// <param name="node">Object node to rename</param>
        public void RenameNode(TreeNode node)
        {
            // Simple method, doesn't work correctly when schema name contains a dot.
            node.Text = node.Text.Substring(node.Text.IndexOf('.') + 1);
        }

        /// <summary>
        /// Create schema nodes and move tables, functions and stored procedures under its schema node
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        /// <returns>The count of schema nodes.</returns>
        public int ReorganizeNodes(TreeNode node, string nodeTag)
        {
            return ReorganizeNodes(node, nodeTag, 1);
        }

        /// <summary>
        /// Create schema nodes and move tables, functions and stored procedures under its schema node
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        /// <param name="folderLevel">The folder level of the current node</param>
        /// <returns>The count of schema nodes.</returns>
        private int ReorganizeNodes(TreeNode node, string nodeTag, int folderLevel)
        {
            debug_message("ReorganizeNodes");

            //BUG: folder node count should be ignored on after expanding event
            if (node.Nodes.Count <= 1 || node.Nodes.Count < GetFolderLevelMinNodeCount(folderLevel))
                return 0;

            //debug_message(DateTime.Now.ToString("ss.fff"));

            node.TreeView.BeginUpdate();

            //can't move nodes while iterating forward over them
            //create list of nodes to move then perform the update

            var folders = new SortedDictionary<string, List<TreeNode>>();

            foreach (TreeNode childNode in node.Nodes)
            {
                //skip schema node folders but make sure they are in the folders list
                if (childNode.Tag != null && childNode.Tag.ToString() == nodeTag)
                {
                    if (!folders.ContainsKey(childNode.Name))
                        folders.Add(childNode.Name, new List<TreeNode>());

                    continue;
                }

                var folderName = GetFolderName(childNode, folderLevel);

                if (string.IsNullOrEmpty(folderName))
                    continue;

                //create schema node
                if (!node.Nodes.ContainsKey(folderName))
                {
                    TreeNode folderNode;
                    if (Options.CloneParentNode)
                    {
                        folderNode = new SchemaFolderTreeNode(node);
                        node.Nodes.Add(folderNode);
                    }
                    else
                    {
                        folderNode = node.Nodes.Add(folderName);
                    }

                    folderNode.Name = folderName;
                    folderNode.Text = folderName;
                    folderNode.Tag = nodeTag;

                    if (Options.AppendDot)
                        folderNode.Text += ".";

                    if (Options.UseObjectIcon)
                    {
                        folderNode.ImageIndex = childNode.ImageIndex;
                        folderNode.SelectedImageIndex = childNode.ImageIndex;
                    }
                    else
                    {
                        folderNode.ImageIndex = node.ImageIndex;
                        folderNode.SelectedImageIndex = node.ImageIndex;
                    }
                }

                //add node to folder list
                List<TreeNode> folderNodeList;
                if (!folders.TryGetValue(folderName, out folderNodeList))
                {
                    folderNodeList = new List<TreeNode>();
                    folders.Add(folderName, folderNodeList);
                }
                folderNodeList.Add(childNode);
            }

            //debug_message(DateTime.Now.ToString("ss.fff"));

            //BUG: Alphabetical may not be in order if no schema folder.
            bool sortRequired = true;

            //move nodes to schema node
            //debug_message("move nodes to schema node");
            foreach (string nodeName in folders.Keys)
            {
                //debug_message("folderNode:{0}", nodeName);
                var folderNode = node.Nodes[nodeName];

                if (sortRequired)
                {
                    node.Nodes.Remove(folderNode);
                    node.Nodes.Add(folderNode);
                }

                foreach (TreeNode childNode in folders[nodeName])
                {
                    //debug_message("childNode:{0}", childNode.Text);
                    node.Nodes.Remove(childNode);
                    if (Options.RenameNode)
                    {
                        // Note: Node is renamed back to orginal after expanding.
                        RenameNode(childNode);
                    }
                    folderNode.Nodes.Add(childNode);
                }
            }

            node.TreeView.EndUpdate();

            //debug_message(DateTime.Now.ToString("ss.fff"));

            //process next folder level
            if (folderLevel < 2)
            {
                foreach (string nodeName in folders.Keys)
                {
                    //debug_message("Next ReorganizeNodes: {1} > {0}", nodeName, folderLevel);
                    ReorganizeNodes(node.Nodes[nodeName], nodeTag, folderLevel + 1);
                }
            }

            return folders.Count;
        }

        private void debug_message(string message)
        {
            if (Package is IDebugOutput)
            {
                ((IDebugOutput)Package).debug_message(message);
            }
        }

        private void debug_message(string message, params object[] args)
        {
            if (Package is IDebugOutput)
            {
                ((IDebugOutput)Package).debug_message(message, args);
            }
        }

    }

}
