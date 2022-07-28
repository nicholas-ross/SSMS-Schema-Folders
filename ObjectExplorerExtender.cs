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

            //debug_message(DateTime.Now.ToString("ss.fff"));

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

            //debug_message(DateTime.Now.ToString("ss.fff"));

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
                }
            }

            node.TreeView.EndUpdate();

            //debug_message(DateTime.Now.ToString("ss.fff"));

            return schemas.Count;
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
