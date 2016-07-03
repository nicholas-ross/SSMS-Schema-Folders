using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SsmsSchemaFolders
{
    /// <summary>
    /// Used to organize Databases and Tables in Object Explorer into groups
    /// </summary>
    public class ObjectExplorerExtender
    {

        //settings
        public bool AppendDot { get; set; }
        public bool CloneParentNode { get; set; }
        public bool UseObjectIcon { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public ObjectExplorerExtender()
        {
            AppendDot = true;
            UseObjectIcon = true;
            CloneParentNode = true;
        }


        /// <summary>
        /// Gets node information from underlying type of tree node view
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public INodeInformation GetNodeInformation(TreeNode node)
        {
            INodeInformation service = null;
            IServiceProvider provider = node as IServiceProvider;

            if (provider != null)
            {
                service = provider.GetService(typeof(INodeInformation)) as INodeInformation;
            }
            return service;
        }


        /// <summary>
        /// Get urn from tree node view
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Urn GetNodeUrn(TreeNode node)
        {
            INodeInformation service = GetNodeInformation(node);
            return GetServiceUrn(service);
        }

        /// <summary>
        /// Get urn from underlying node information
        /// </summary>
        /// <param name="service">INodeInformation of object explorer item</param>
        /// <returns>Urn of node</returns>
        private Urn GetServiceUrn(INodeInformation service)
        {
            Urn urn = null;
            if (service != null)
                urn = new Urn(service.Context);
            return urn;
        }
        

        /// <summary>
        /// Create schema nodes and move tables under its schema node, functions and stored procedures
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="imageName">Image of new node</param>
        /// <param name="subItemImage">Image for subnodes, if empty - current image will be used</param>
        public void ReorganizeNodes(TreeNode node, string imageName, string subItemImage)
        {
            var nodesToMove = new List<KeyValuePair<string, List<TreeNode>>>();
            var createNodes = new List<KeyValuePair<string, TreeNode>>();

            for (int i = node.Nodes.Count - 1; i > -1; i--)
            {
                TreeNode tn = node.Nodes[i];
                Urn rn = GetNodeUrn(tn);
                if (rn == null)
                    continue;
                //MessageBox.Show(rn.Type);
                string schema = rn.GetAttribute("Schema");
                if (string.IsNullOrEmpty(schema))
                    continue;

                var kvpCreateNd = new KeyValuePair<string, TreeNode>(schema, null);
                var kvpNodesToMove = new KeyValuePair<string, List<TreeNode>>(schema, new List<TreeNode>());
                kvpNodesToMove.Value.Add(tn);

                if (!createNodes.Contains(kvpCreateNd))
                    createNodes.Add(kvpCreateNd);
                if (!nodesToMove.Contains(kvpNodesToMove))
                    nodesToMove.Add(kvpNodesToMove);
            }

            DoReorganization(node, nodesToMove, createNodes, imageName, subItemImage);
        }


        /// <summary>
        /// Reorganizing nodes according to move list and new node list
        /// </summary>
        /// <param name="node">parent node</param>
        /// <param name="nodesToMove">nodes to move</param>
        /// <param name="createNodes">nodes to create</param>
        /// <param name="imageName">icon name for created nodes</param>
        /// <param name="subItemImage">icon name for moved nodes</param>
        private void DoReorganization(TreeNode node, List<KeyValuePair<string, List<TreeNode>>> nodesToMove, List<KeyValuePair<string, TreeNode>> createNodes, string imageName, string subItemImage )
        {
            if (createNodes == null)
                return;

            var imageIndex = node.ImageIndex;
            if (UseObjectIcon && node.Nodes.Count > 0)
            {
                // First node icon is usually system objects folder so use second node if it exists.
                //imageIndex = (node.Nodes.Count > 1) ? node.Nodes[1].ImageIndex : node.Nodes[0].ImageIndex;
                imageIndex = node.Nodes[node.Nodes.Count - 1].ImageIndex;
            }

            for (int i = createNodes.Count - 1; i > -1; i--)
            {
                KeyValuePair<string, TreeNode> kvp = createNodes[i];
                if (kvp.Value == null)
                {
                    if (node.Nodes.ContainsKey(kvp.Key))
                    {
                        KeyValuePair<string, TreeNode> kvpNew = new KeyValuePair<string, TreeNode>(kvp.Key, node.Nodes[kvp.Key]);
                        createNodes[i] = kvpNew;
                    }
                    else
                    {
                        //KeyValuePair<string, TreeNode> kvpNew = new KeyValuePair<string, TreeNode>(kvp.Key, node.Nodes.Add(kvp.Key, kvp.Key, imageName, imageName));
                        var schemaNode = (CloneParentNode) ? CreateChildTreeNodeWithMenu(node) : node.Nodes.Add(kvp.Key, kvp.Key, imageIndex, imageIndex);
                        schemaNode.Name = kvp.Key;
                        schemaNode.Text = (AppendDot) ? kvp.Key + "." : kvp.Key;
                        schemaNode.ImageIndex = imageIndex;
                        schemaNode.SelectedImageIndex = imageIndex;
                        //schemaNode.ToolTipText = ;
                        schemaNode.Tag = "SchemaFolder";
                        var kvpNew = new KeyValuePair<string, TreeNode>(kvp.Key, schemaNode);
                        createNodes[i] = kvpNew;
                    }
                }
            }
            // MessageBox.Show(string.Format("Test {0} {1} ", createNodes.Count, nodesToMove.Count));

            if (nodesToMove == null)
                return;

            for (int i = nodesToMove.Count - 1; i > -1; i--)
            {
                KeyValuePair<string, List<TreeNode>> kvp = nodesToMove[i];
                if (kvp.Value != null && kvp.Value.Count > 0)
                {
                    foreach (TreeNode tn in kvp.Value)
                    {
                        node.Nodes.Remove(tn);
                        if (!string.IsNullOrEmpty(subItemImage))
                        {
                            tn.ImageKey = subItemImage;
                            tn.SelectedImageKey = subItemImage;
                        }
                    }
                    TreeNode[] trnar = new TreeNode[kvp.Value.Count];
                    for (int j = 0; j < kvp.Value.Count; j++)
                        trnar[j] = kvp.Value[j];

                    for (int k = createNodes.Count - 1; k > -1; k--)
                    {
                        if (createNodes[k].Key.Equals(kvp.Key))
                        {
                            createNodes[k].Value.Nodes.AddRange(trnar);
                            break;
                        }
                    }
                }
            }
        }

        private TreeNode CreateChildTreeNodeWithMenu(TreeNode parent)
        {
            var node = new SchemaFolderTreeNode(parent);
            parent.Nodes.Add(node);
            return node;
        }

    }

}
