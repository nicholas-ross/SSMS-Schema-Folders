using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private String GetNodeSchemaQuick(TreeNode node)
        {
            var dotIndex = node.Text.IndexOf('.');
            if (dotIndex != -1)
                return node.Text.Substring(0, dotIndex);
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
                    return ni.InvariantName.Substring(0, ni.InvariantName.Length - ni.Name.Length - 1);
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
            debug_message("ReorganizeNodes");

            if (node.Nodes.Count <= 1)
                return 0;
            
            if (Options.UseClear > 0 && node.Nodes.Count >= Options.UseClear)
                return ReorganizeNodesWithClear(node, nodeTag);

            var nodeText = node.Text;
            node.Text += " (sorting...)";
            //node.TreeView.Update();

            var quickAndDirty = (Options.QuickSchema > 0 && node.Nodes.Count > Options.QuickSchema);

            //var sw = Stopwatch.StartNew();
            //debug_message("BeginUpdate:{0}", sw.ElapsedMilliseconds);

            node.TreeView.BeginUpdate();

            var unresponsive = Stopwatch.StartNew();

            //can't move nodes while iterating forward over them
            //create list of nodes to move then perform the update

            var schemas = new Dictionary<String, List<TreeNode>>();
            int schemaNodeIndex = -1;
            var newSchemaNodes = new List<TreeNode>();

            //debug_message("Sort Nodes:{0}", sw.ElapsedMilliseconds);

            foreach (TreeNode childNode in node.Nodes)
            {
                //skip schema node folders but make sure they are in schemas list
                if (childNode.Tag != null && childNode.Tag.ToString() == nodeTag)
                {
                    if (!schemas.ContainsKey(childNode.Name))
                        schemas.Add(childNode.Name, new List<TreeNode>());

                    schemaNodeIndex = childNode.Index;

                    continue;
                }

                var schema = (quickAndDirty) ? GetNodeSchemaQuick(childNode) : GetNodeSchema(childNode);

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
                    newSchemaNodes.Add(schemaNode);

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

                if (unresponsive.ElapsedMilliseconds > Options.UnresponsiveTimeout)
                {
                    node.TreeView.EndUpdate();
                    Application.DoEvents();
                    if (node.TreeView == null)
                        return 0;
                    node.TreeView.BeginUpdate();
                    unresponsive.Restart();
                }
            }

            //debug_message("Move Nodes:{0}", sw.ElapsedMilliseconds);

            if (schemaNodeIndex >= 0)
            {
                // Move schema nodes to top of tree
                foreach (var schemaNode in newSchemaNodes)
                {
                    node.Nodes.Remove(schemaNode);
                    node.Nodes.Insert(++schemaNodeIndex, schemaNode);
                }
            }

            //move nodes to schema node
            foreach (string schema in schemas.Keys)
            {
                var schemaNode = node.Nodes[schema];
                foreach (TreeNode childNode in schemas[schema])
                {
                    node.Nodes.Remove(childNode);

                    if (Options.RenameNode)
                    {
                        // Note: Node is renamed back to orginal after expanding.
                        RenameNode(childNode);
                    }
                    schemaNode.Nodes.Add(childNode);

                    if (unresponsive.ElapsedMilliseconds > Options.UnresponsiveTimeout)
                    {
                        node.TreeView.EndUpdate(); 
                        Application.DoEvents();
                        if (node.TreeView == null)
                            return 0;
                        node.TreeView.BeginUpdate();
                        unresponsive.Restart();
                    }
                }
            }

            //debug_message("EndUpdate:{0}", sw.ElapsedMilliseconds);

            node.TreeView.EndUpdate();
            node.Text = nodeText;
            unresponsive.Stop();

            //debug_message("Done:{0}", sw.ElapsedMilliseconds);
            //sw.Stop();

            return schemas.Count;
        }

        /// <summary>
        /// Create schema nodes and move tables, functions and stored procedures under its schema node
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        /// <returns>The count of schema nodes.</returns>
        public int ReorganizeNodesWithClear(TreeNode node, string nodeTag)
        {
            debug_message("ReorganizeNodesWithClear");

            var nodeText = node.Text;
            node.Text += " (sorting...)";
            node.TreeView.Update();

            var quickAndDirty = (Options.QuickSchema > 0 && node.Nodes.Count > Options.QuickSchema);

            var sw = Stopwatch.StartNew();
            //debug_message("Sort Nodes:{0}", sw.ElapsedMilliseconds);

            var schemas = new Dictionary<string, List<TreeNode>>();
            var schemaNodes = new Dictionary<string, TreeNode>();
            var nodeNodes = new List<TreeNode>();

            foreach (TreeNode childNode in node.Nodes)
            {
                // schema node folder
                if (childNode.Tag != null && childNode.Tag.ToString() == nodeTag)
                {
                    schemas.Add(childNode.Name, new List<TreeNode>());
                    schemaNodes.Add(childNode.Name, childNode);
                    nodeNodes.Add(childNode);
                    continue;
                }

                var schema = (quickAndDirty) ? GetNodeSchemaQuick(childNode) : GetNodeSchema(childNode);

                // other folder
                if (string.IsNullOrEmpty(schema))
                {
                    nodeNodes.Add(childNode);
                    continue;
                }

                List<TreeNode> schemaNodeList;
                if (schemas.TryGetValue(schema, out schemaNodeList))
                {
                    // add to existing schema
                    schemaNodeList.Add(childNode);
                }
                else
                {
                    // add to new schema
                    schemaNodeList = new List<TreeNode>();
                    schemaNodeList.Add(childNode);

                    schemas.Add(schema, schemaNodeList);

                    // create schema folder
                    TreeNode schemaNode;
                    if (Options.CloneParentNode)
                    {
                        schemaNode = new SchemaFolderTreeNode(node);
                    }
                    else
                    {
                        schemaNode = new TreeNode(schema);
                    }
                    schemaNodes.Add(schema, schemaNode);
                    nodeNodes.Add(schemaNode);

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
            }

            //debug_message("Clear Nodes:{0}", sw.ElapsedMilliseconds);

            //node.TreeView.BeginUpdate();
            node.Text = nodeText + " (clearing...)";
            node.TreeView.Update();
            node.Nodes.Clear();

            //debug_message("DoEvents:{0}", sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds > Options.UnresponsiveTimeout)
            {
                Application.DoEvents();
                if (node.TreeView == null)
                    return 0;
            }

            node.Text = nodeText + " (adding...)";
            node.TreeView.Update();

            //debug_message("Add schemaNode.Nodes:{0}", sw.ElapsedMilliseconds);

            foreach (string schema in schemas.Keys)
            {
                schemaNodes[schema].Nodes.AddRange(schemas[schema].ToArray());
            }

            //debug_message("Add node.Nodes:{0}", sw.ElapsedMilliseconds);

            node.Nodes.AddRange(nodeNodes.ToArray());
            node.Text = nodeText;

            //debug_message("EndUpdate:{0}", sw.ElapsedMilliseconds);

            //node.TreeView.EndUpdate();

            //debug_message("Done:{0}", sw.ElapsedMilliseconds);
            sw.Stop();

            return schemas.Count;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void debug_message(string message)
        {
            if (Package is IDebugOutput)
            {
                ((IDebugOutput)Package).debug_message(message);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void debug_message(string message, params object[] args)
        {
            if (Package is IDebugOutput)
            {
                ((IDebugOutput)Package).debug_message(message, args);
            }
        }

    }

}
