using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Data.SqlClient;

namespace Ssms2012Extender
{
    /// <summary>
    /// Used to organize Databases and Tables in Object Explorer into groups
    /// </summary>
    public class ObjectExplorerExtender
    {
        /// <summary>
        /// Default text displayed if column has no extended description property in DB
        /// <value>Description not found in DB</value>
        /// </summary>
        public const string DescriptionNotFound = "Description not found in DB";

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
        /// Gets node name from underlying type of tree node view
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public   string NodeName(TreeNode node)
        {
            Type t = node.GetType();
            PropertyInfo property = t.GetProperty("NodeName", typeof(string));

            if (property != null)
            {
                return Convert.ToString(property.GetValue(node, null));
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets node information from underlying type of tree node view
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public   INodeInformation GetNodeInformation(TreeNode node)
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
        public   Urn GetNodeUrn(TreeNode node)
        {
            INodeInformation service = GetNodeInformation(node);
            return GetServiceUrn(service);
        }

        /// <summary>
        /// Get urn from underlying node information
        /// </summary>
        /// <param name="service">INodeInformation of object explorer item</param>
        /// <returns>Urn of node</returns>
        public   Urn GetServiceUrn(INodeInformation service)
        {
            Urn urn = null;
            if (service != null)
                urn = new Urn(service.Context);
            return urn;
        }

        /// <summary>
        /// Gets connection string from tree node view item
        /// </summary>
        /// <param name="node">TreeNode view item</param>
        /// <returns>Connection string of the tree node item</returns>
        public   string GetConnectionString(TreeNode node)
        {
            INodeInformation service = GetNodeInformation(node);
            Urn urn = GetServiceUrn(service);

            //System.Diagnostics.Debug.Print(service.Connection.ConnectionString);

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(service.Connection.ConnectionString);

            builder.InitialCatalog = urn.GetAttribute("Name", "Database");

            return builder.ToString();
        }

        /// <summary>
        /// Gets urn path from tree node view
        /// </summary>
        /// <param name="node">TreeNode item</param>
        /// <returns>urn path of the object explorer node from tree node view</returns>
        public   string GetNodeUrnPath(TreeNode node)
        {
            string urnPath = string.Empty;
            INodeInformation service = GetNodeInformation(node);
            if (service != null)
                urnPath = service.UrnPath;
            return urnPath;
        }

        /// <summary>
        /// Gets context string of node
        /// </summary>
        /// <param name="node">TreeNode item from view</param>
        /// <returns>Context path</returns>
        public   string GetNodeContext(TreeNode node)
        {
            INodeInformation ni = GetNodeInformation(node);
            if (ni != null)
                return ni.Context;
            return string.Empty;
        }

        /// <summary>
        /// Sets the childrenEnumerated field for a node
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="enumerated">if set to <c>true</c> [enumerated].</param>
        /// <remarks>
        /// This is to suppress an error when SSMS assumes the nodes we add are HierarchyTreeNodes as opposed to 
        /// bog standard TreeNodes.
        /// </remarks>
        public   void ChildrenEnumerated(TreeNode node, bool enumerated)
        {
            Type t = node.GetType();
            FieldInfo field = t.GetField("childrenEnumerated", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(node, enumerated);
            }
        }


        /// <summary>
        /// Create schema nodes and move tables under its schema node, functions and stored procedures
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="imageName">Image of new node</param>
        /// <param name="subItemImage">Image for subnodes, if empty - current image will be used</param>
        public void ReorganizeNodes(TreeNode node, string imageName, string subItemImage)
        {
            List<KeyValuePair<string, List<TreeNode>>> nodesToMove = new List<KeyValuePair<string, List<TreeNode>>>();
            List<KeyValuePair<string, TreeNode>> createNodes = new List<KeyValuePair<string, TreeNode>>();

            for (int i = node.Nodes.Count - 1; i > -1; i--)
            {
                TreeNode tn = node.Nodes[i];
                Urn rn = GetNodeUrn(tn);
                if (rn == null)
                    continue;
                //MessageBox.Show(rn.Type);
                string schema = rn.GetAttribute("Schema");
                //string schema = rn.GetAttribute("Schema", "Table");
                //if (string.IsNullOrEmpty(schema))
                //    schema = rn.GetAttribute("Schema", "StoredProcedure");
                //if (string.IsNullOrEmpty(schema))
                //    schema = rn.GetAttribute("Schema", "UserDefinedFunction");
                if (string.IsNullOrEmpty(schema))
                    continue;

                KeyValuePair<string, TreeNode> kvpCreateNd = new KeyValuePair<string, TreeNode>(schema, null);
                KeyValuePair<string, List<TreeNode>> kvpNodesToMove = new KeyValuePair<string, List<TreeNode>>(schema, new List<TreeNode>());
                kvpNodesToMove.Value.Add(tn);

                if (!createNodes.Contains(kvpCreateNd))
                    createNodes.Add(kvpCreateNd);
                if (!nodesToMove.Contains(kvpNodesToMove))
                    nodesToMove.Add(kvpNodesToMove);
            }

           DoReorganization(node, nodesToMove, createNodes, imageName, subItemImage);
        }

        /// <summary>
        /// Creates new DB Group folders and move DB under them
        /// </summary>
        /// <param name="node">Server/DatabaseFolder node</param>
        /// <param name="imageName">folder icon</param>
        /// <param name="subItemImage">database icon</param>
        public void ReorganizeDbNodes(TreeNode node, string imageName, string subItemImage, Dictionary<string, Dictionary<string, string>> dbFolderLinks)
        {
            if (dbFolderLinks == null || dbFolderLinks.Count == 0)
                return;
            Dictionary<string, string> dbGroups = null;

           var nodesToMove = new List<KeyValuePair<string, List<TreeNode>>>();
           var createNodes = new List<KeyValuePair<string, TreeNode>>();

            string serverName = string.Empty;

            for (int i = node.Nodes.Count - 1; i > -1; i--)
            {
                TreeNode tn = node.Nodes[i];
                Urn rn = GetNodeUrn(tn);
                if (rn == null)
                    continue;
                if (string.IsNullOrEmpty(serverName))
                    serverName = rn.GetAttribute("Name", "Server");
                if (dbGroups == null && !string.IsNullOrEmpty(serverName) && dbFolderLinks.ContainsKey(serverName))
                    dbGroups = dbFolderLinks[serverName];

                string dbName = rn.GetAttribute("Name", "Database");

                if (!string.IsNullOrEmpty(dbName) && dbGroups != null && dbGroups.ContainsKey(dbName))
                {
                    var kvpCreateNd = new KeyValuePair<string, TreeNode>(dbGroups[dbName], null);
                    var kvpNodesToMove = new KeyValuePair<string, List<TreeNode>>(dbGroups[dbName], new List<TreeNode>());
                    kvpNodesToMove.Value.Add(tn);

                    if (!createNodes.Contains(kvpCreateNd))
                        createNodes.Add(kvpCreateNd);
                    if (!nodesToMove.Contains(kvpNodesToMove))
                        nodesToMove.Add(kvpNodesToMove);
                }
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

        public TreeNode CreateChildTreeNodeWithMenu(TreeNode parent)
        {
            var node = new MyTreeNode(parent);
            parent.Nodes.Add(node);
            return node;
        }

    }

    public class MyTreeNode : HierarchyTreeNode, INodeWithMenu, IServiceProvider
    //IServiceProvider causes the SchemaFolders.DoReorganization to run on itself
    {
        object parent;

        public MyTreeNode(object o)
        {
            parent = o;
            //this.ToolTipText = "INodeInformation is " + ((ni == null) ? "null" : "not null");
            //this.ToolTipText = (o as INodeWithAltName).NodeName; //database name
        }

        public override System.Drawing.Icon Icon
        {
            get { return (parent as INodeWithIcon).Icon; }
        }

        public override System.Drawing.Icon SelectedIcon
        {
            get { return (parent as INodeWithIcon).SelectedIcon; }
        }

        public override bool ShowPolicyHealthState
        {
            get
            {
                return false;
            }
            set
            {
                //throw new NotImplementedException();
            }
        }

        public override int State
        {
            get { return (parent == null) ? 0 : (parent as INodeWithIcon).State; }
        }


        public object GetService(Type serviceType)
        {
            return (parent == null) ? null : (parent as IServiceProvider).GetService(serviceType);
        }


        public void DoDefaultAction()
        {
            (parent as INodeWithMenu).DoDefaultAction();
        }

        public void ShowContextMenu(System.Drawing.Point screenPos)
        {
            (parent as INodeWithMenu).ShowContextMenu(screenPos);
        }

    }

}
