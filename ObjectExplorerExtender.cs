using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Collections.Generic;
//using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SsmsSchemaFolders
{
    /// <summary>
    /// Used to organize Databases and Tables in Object Explorer into groups
    /// </summary>
    public class ObjectExplorerExtender
    {

        private ISchemaFolderOptions Options { get; }
        //private Regex NodeSchemaRegex;


        /// <summary>
        /// 
        /// </summary>
        public ObjectExplorerExtender(ISchemaFolderOptions options)
        {
            Options = options;
            //NodeSchemaRegex = new Regex(@"@Schema='((''|[^'])+)'");
        }


        /// <summary>
        /// Gets node information from underlying type of tree node view
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <remarks>Copy of private method in ObjectExplorerService</remarks>
        public INodeInformation GetNodeInformation(TreeNode node)
        {
            INodeInformation result = null;
            IServiceProvider serviceProvider = node as IServiceProvider;
            if (serviceProvider != null)
            {
                result = (serviceProvider.GetService(typeof(INodeInformation)) as INodeInformation);
            }
            return result;
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
        /// Create schema nodes and move tables under its schema node, functions and stored procedures
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        public void ReorganizeNodes(TreeNode node, string nodeTag)
        {
            var nodesToMove = new List<KeyValuePair<string, List<TreeNode>>>();
            var createNodes = new List<KeyValuePair<string, TreeNode>>();

            for (int i = node.Nodes.Count - 1; i > -1; i--)
            {
                var tn = node.Nodes[i];
                string schema = GetNodeSchema(tn);
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

            DoReorganization(node, nodesToMove, createNodes, nodeTag);
        }


        /// <summary>
        /// Reorganizing nodes according to move list and new node list
        /// </summary>
        /// <param name="parentNode">parent node</param>
        /// <param name="nodesToMove">nodes to move</param>
        /// <param name="createNodes">nodes to create</param>
        /// <param name="nodeTag">tag for created nodes</param>
        private void DoReorganization(TreeNode parentNode, List<KeyValuePair<string, List<TreeNode>>> nodesToMove, List<KeyValuePair<string, TreeNode>> createNodes, string nodeTag)
        {
            if (createNodes == null)
                return;

            var imageIndex = parentNode.ImageIndex;
            if (Options.UseObjectIcon && parentNode.Nodes.Count > 0)
            {
                // First few node icons are usually folders so use icon of last node.
                imageIndex = parentNode.Nodes[parentNode.Nodes.Count - 1].ImageIndex;
            }

            for (int createNodeIndex = createNodes.Count - 1; createNodeIndex > -1; createNodeIndex--)
            {
                var kvp = createNodes[createNodeIndex];
                if (kvp.Value == null)
                {
                    if (parentNode.Nodes.ContainsKey(kvp.Key))
                    {
                        var kvpNew = new KeyValuePair<string, TreeNode>(kvp.Key, parentNode.Nodes[kvp.Key]);
                        createNodes[createNodeIndex] = kvpNew;
                    }
                    else
                    {
                        TreeNode schemaNode;
                        if (Options.CloneParentNode)
                            schemaNode = CreateChildTreeNodeWithMenu(parentNode);
                        else
                            schemaNode = parentNode.Nodes.Add(kvp.Key, kvp.Key, imageIndex, imageIndex);

                        schemaNode.Name = kvp.Key;
                        schemaNode.Text = kvp.Key;
                        if (Options.AppendDot)
                            schemaNode.Text += ".";
                        schemaNode.ImageIndex = imageIndex;
                        schemaNode.SelectedImageIndex = imageIndex;
                        schemaNode.Tag = nodeTag;

                        var kvpNew = new KeyValuePair<string, TreeNode>(kvp.Key, schemaNode);
                        createNodes[createNodeIndex] = kvpNew;
                    }
                }
            }

            if (nodesToMove == null)
                return;

            for (int i = nodesToMove.Count - 1; i > -1; i--)
            {
                var kvp = nodesToMove[i];
                if (kvp.Value != null && kvp.Value.Count > 0)
                {
                    foreach (var tn in kvp.Value)
                    {
                        parentNode.Nodes.Remove(tn);
                    }
                    var trnar = new TreeNode[kvp.Value.Count];
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
