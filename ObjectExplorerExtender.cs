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
                //debug_message("NodeInformation\n UrnPath:{0}\n Name:{1}\n InvariantName:{2}\n Context:{3}\n NavigationContext:{4}", ni.UrnPath, ni.Name, ni.InvariantName, ni.Context, ni.NavigationContext);
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
        /// Create schema nodes and move tables, functions and stored procedures under its schema node
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        /// <returns>The count of schema nodes.</returns>
        public int ReorganizeNodes(TreeNode node, string nodeTag)
        {
            debug_message("ReorganizeNodes");

            if (node.Nodes.Count <= 1)
                return 0;

            debug_message(DateTime.Now.ToString("ss.fff"));

            node.TreeView.BeginUpdate();

            //can't move nodes while iterating forward over them
            //create list of nodes to move then perform the update

            var schemas = new Dictionary<String, List<TreeNode>>();

            foreach (TreeNode childNode in node.Nodes)
            {
                //skip schema node folders but make sure they are in schemas list
                if (childNode.Tag != null && childNode.Tag.ToString() == nodeTag)
                {
                    if (!schemas.ContainsKey(childNode.Name))
                        schemas.Add(childNode.Name, new List<TreeNode>());

                    continue;
                }

                var schema = GetNodeSchema(childNode);

                if (string.IsNullOrEmpty(schema))
                    continue;

                //create schema node
                if (!node.Nodes.ContainsKey(schema))
                {
                    TreeNode schemaNode;
                    if (Options.CloneParentNode)
                    {
                        schemaNode = new SchemaFolderTreeNode(node);
                        node.Nodes.Add(schemaNode);
                    }
                    else
                    {
                        schemaNode = node.Nodes.Add(schema);
                    }

                    schemaNode.Name = schema;
                    schemaNode.Text = schema;
                    schemaNode.Tag = nodeTag;

                    if (Options.AppendDot)
                        schemaNode.Text += ".";

                    if (Options.UseObjectIcon)
                    {
                        schemaNode.ImageIndex = childNode.ImageIndex;
                        schemaNode.SelectedImageIndex = childNode.ImageIndex;
                    }
                    else
                    {
                        schemaNode.ImageIndex = node.ImageIndex;
                        schemaNode.SelectedImageIndex = node.ImageIndex;
                    }
                }

                //add node to schema list
                List<TreeNode> schemaNodeList;
                if (!schemas.TryGetValue(schema, out schemaNodeList))
                {
                    schemaNodeList = new List<TreeNode>();
                    schemas.Add(schema, schemaNodeList);
                }
                schemaNodeList.Add(childNode);
            }

            //move nodes to schema node
            foreach (string schema in schemas.Keys)
            {
                var schemaNode = node.Nodes[schema];
                foreach (TreeNode childNode in schemas[schema])
                {
                    node.Nodes.Remove(childNode);
                    schemaNode.Nodes.Add(childNode);
                }
            }

            node.TreeView.EndUpdate();

            debug_message(DateTime.Now.ToString("ss.fff"));

            return schemas.Count;
        }

        /// <summary>
        /// Create schema nodes and move tables under its schema node, functions and stored procedures
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        public int ReorganizeNodes_old(TreeNode node, string nodeTag)
        {
            debug_message("ReorganizeNodes");
            debug_message(DateTime.Now.ToString("ss.fff"));

            var nodesToMove = new List<KeyValuePair<string, List<TreeNode>>>(); //why is kvp value a list?
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

            debug_message(DateTime.Now.ToString("ss.fff"));

            return DoReorganization(node, nodesToMove, createNodes, nodeTag);
        }


        /// <summary>
        /// Reorganizing nodes according to move list and new node list
        /// </summary>
        /// <param name="parentNode">parent node</param>
        /// <param name="nodesToMove">nodes to move</param>
        /// <param name="createNodes">nodes to create</param>
        /// <param name="nodeTag">tag for created nodes</param>
        private int DoReorganization(TreeNode parentNode, List<KeyValuePair<string, List<TreeNode>>> nodesToMove, List<KeyValuePair<string, TreeNode>> createNodes, string nodeTag)
        {
            debug_message("DoReorganization");

            if (createNodes == null)
                return 0;

            var imageIndex = parentNode.ImageIndex;
            if (Options.UseObjectIcon && parentNode.Nodes.Count > 0)
            {
                // First few node icons are usually folders so use icon of last node.
                //imageIndex = parentNode.Nodes[parentNode.Nodes.Count - 1].ImageIndex;
                imageIndex = parentNode.LastNode.ImageIndex;
            }

            debug_message("createNodes.Count:{0}", createNodes.Count);
            debug_message(DateTime.Now.ToString("ss.fff"));

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

            debug_message(DateTime.Now.ToString("ss.fff"));

            if (nodesToMove == null)
                return createNodes.Count;

            //parentNode.TreeView.UseWaitCursor = true; //doesn't work
            //parentNode.TreeView.Cursor = Cursors.WaitCursor;
            //parentNode.TreeView.Cursor = Cursors.AppStarting;
            parentNode.TreeView.BeginUpdate();
            //parentNode.TreeView.SuspendLayout();

            //if (nodesToMove.Count > 100) use timer to execute batches

            debug_message("nodesToMove.Count:{0}", nodesToMove.Count);

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
                //update in batches
                if (i % 100 == 0)
                {
                    parentNode.TreeView.EndUpdate();
                    parentNode.TreeView.BeginUpdate();
                }
            }

            parentNode.TreeView.EndUpdate();
            //parentNode.TreeView.UseWaitCursor = false;
            //parentNode.TreeView.Cursor = Cursors.Default;

            debug_message(DateTime.Now.ToString("ss.fff"));

            return createNodes.Count;
        }
        
        private TreeNode CreateChildTreeNodeWithMenu(TreeNode parent)
        {
            var node = new SchemaFolderTreeNode(parent);
            parent.Nodes.Add(node);
            return node;
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
