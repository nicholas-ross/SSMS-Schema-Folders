﻿using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;

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

        // Index of the custom green-folder image inside the Object Explorer TreeView image list.
        // -1 until we inject it.
        private int _greenFolderImageIndex = -1;

        /// <summary>
        /// 
        /// </summary>
        public ObjectExplorerExtender(IServiceProvider package, ISchemaFolderOptions options)
        {
            Package = package;
            Options = options;
            //NodeSchemaRegex = new Regex(@"@Schema='((''|[^'])+)'");
        }


        /// Ensures the custom green folder icon is present in the supplied TreeView image list and remembers the index. Safe to call multiple times.
        private void EnsureGreenFolderIcon(TreeView treeView)
        {
            if (treeView == null)
                return;

            if (_greenFolderImageIndex >= 0)
                return; // already done

            if (treeView.ImageList == null)
                return; // Should not happen – OE TreeView always has one.

            try
            {
                var asm = typeof(ObjectExplorerExtender).Assembly;
                // Find the resource regardless of namespace/case.
                var resName = asm.GetManifestResourceNames()
                                 .FirstOrDefault(n => n.EndsWith("greenFolder.png", StringComparison.OrdinalIgnoreCase));

                if (resName != null)
                {
                    using (var stream = asm.GetManifestResourceStream(resName))
                    {
                        if (stream != null)
                        {
                            using (var img = Image.FromStream(stream))
                            {
                                _greenFolderImageIndex = treeView.ImageList.Images.Count;
                                treeView.ImageList.Images.Add(img);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore icon load issues – fallback will be default folder.
                _greenFolderImageIndex = -1;
            }
        }


        /// Applies a green folder icon (if loaded) to the supplied node.
        private void ApplyGreenFolderIcon(TreeNode folderNode, TreeView treeView)
        {
            EnsureGreenFolderIcon(treeView);
            if (_greenFolderImageIndex >= 0)
            {
                folderNode.ImageIndex = _greenFolderImageIndex;
                folderNode.SelectedImageIndex = _greenFolderImageIndex;
            }
        }

        public string GetFolderName(TreeNode node, int folderLevel, bool quickSchemaName, bool expanding)
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
                    string schema = (quickSchemaName) ? GetNodeSchemaQuick(node) : GetNodeSchema(node);

                    if (schema != null && Options.Level1FolderType == Options.Level2FolderType)
                    {
                        int dotIndex = schema.IndexOf('.');
                        if (dotIndex == -1)
                        {
                            dotIndex = schema.IndexOf('_');
                        }
                        if (dotIndex != -1)
                        {
                            schema = (folderLevel == 1) ? schema.Substring(0, dotIndex) : schema.Substring(dotIndex + 1);
                        }
                        else
                        {
                            if (folderLevel == 2)
                                // Already sorted by schema. Don't add again.
                                return null;
                        }
                    }
                    return schema;

                case FolderType.Alphabetical:
                    var name = GetNodeName(node);
                    //debug_message("{0} > {1}", node.Text, name);

                    if (!string.IsNullOrEmpty(name))
                    {
                        return name.Substring(0, 1).ToUpper();
                    }
                    break;
                case FolderType.Regex:
                    string regex = (folderLevel == 1) ? Options.Level1Regex : Options.Level2Regex;
                    //DebugLogger.Log("GetFolderName: Node: '{0}', Regex: '{1}'", node.Text, regex);
                    if (!string.IsNullOrEmpty(regex))
                    {
                        var nodeName = GetNodeName(node);
                        if (string.IsNullOrEmpty(nodeName))
                        {
                            var text = node.Text;
                            var dotIndex = text.IndexOf('.');
                            if (dotIndex >= 0)
                            {
                                nodeName = text.Substring(dotIndex + 1);
                            }
                            else
                            {
                                nodeName = text;
                            }
                        }
                        //DebugLogger.Log("GetFolderName: Derived NodeName: '{0}'", nodeName);

                        if (!string.IsNullOrEmpty(nodeName))
                        {
                            var nameRegex = new Regex(regex, RegexOptions.IgnoreCase);
                            var match = nameRegex.Match(nodeName);
                            if (match.Success && match.Groups.Count > 1)
                            {
                                string folder = null;
                                // Some patterns (alternations etc.) may define multiple capture groups.
                                // Use the first group that actually captured content.
                                for (int gi = 1; gi < match.Groups.Count; gi++)
                                {
                                    var grp = match.Groups[gi];
                                    if (grp.Success && grp.Length > 0)
                                    {
                                        folder = grp.Value;
                                        break;
                                    }
                                }
                                if (!string.IsNullOrEmpty(folder))
                                {
                                    // DebugLogger.Log("GetFolderName: Match success, returning folder: '{0}'", folder);
                                    return folder;
                                }
                            }
                            
                            bool groupAsOther = (folderLevel == 1) ? Options.Level1GroupNonMatchingAsOther : Options.Level2GroupNonMatchingAsOther;
                            if (expanding && groupAsOther)
                            {
                                //DebugLogger.Log("GetFolderName: No match, returning 'Other'");
                                return "Other";
                            }
                            //DebugLogger.Log("GetFolderName: No match, returning null");
                            return null;
                        }
                    }
                    //DebugLogger.Log("GetFolderName: No regex or nodename, returning null");
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

                //debug_message("ni:{0}", sw.ElapsedMilliseconds);
                //var name = ni.Name;
                //debug_message("ni.Name:{0}", sw.ElapsedMilliseconds);
                //var invariantName = ni.InvariantName;
                //debug_message("ni.InvariantName:{0}", sw.ElapsedMilliseconds);

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
            if (GetNodeName(node) is string name)
            {
                node.Text = name;
            }
        }

        /// <summary>
        /// Removes schema name from object node.
        /// </summary>
        /// <param name="node">Object node to rename</param>
        /// <param name="quick">Use quick substring method</param>
        private void RenameNode(TreeNode node, bool quick)
        {
            if (quick)
            {
                // Simple method, doesn't work correctly when schema name contains a dot.
                node.Text = node.Text.Substring(node.Text.IndexOf('.') + 1);
            }
            else
            {
                RenameNode(node);
            }
        }

        /// <summary>
        /// Create schema nodes and move tables, functions and stored procedures under its schema node
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        /// <param name="expanding">Executing in AfterExpand event</param>
        /// <returns>The count of schema nodes.</returns>
        public int ReorganizeNodes(TreeNode node, string nodeTag, bool expanding)
        {
            if (node.Nodes.Count <= 1)
                // 1 is the lazy expanding placeholder node.
                return 0;

            if (!string.IsNullOrWhiteSpace(nodeTag) && node.Tag != null && node.Tag.ToString().Contains(nodeTag))
                // Already done this node.
                return 0;

            int nodesReorganized = ReorganizeNodes(node, nodeTag, expanding, 1);
            if (nodesReorganized > 0)
            {
                // Re-sort nodes
                var sortedChildren = node.Nodes.Cast<TreeNode>()
                                         .OrderBy(n => n, new SchemaFolderNodeSorter())
                                         .ToArray();
                node.Nodes.Clear();
                node.Nodes.AddRange(sortedChildren);

                // Log the final ordering of the top-level items after reorganisation. This will help trace any issues where nodes appear out of order in Object Explorer.
                
                // DebugLogger.Log("Top-level items for '{0}' (count: {1})", node.Text, sortedChildren.Length);
                // foreach (var child in sortedChildren)
                // {
                //     DebugLogger.Log("    {0}", child.Text);
                // }
            }
            return nodesReorganized;
        }

        //Stopwatch sw = new Stopwatch();

        /// <summary>
        /// Create schema nodes and move tables, functions and stored procedures under its schema node
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        /// <param name="folderLevel">The folder level of the current node</param>
        /// <param name="expanding">Executing in AfterExpand event</param>
        /// <returns>The count of schema nodes.</returns>
        private int ReorganizeNodes(TreeNode node, string nodeTag, bool expanding, int folderLevel)
        {
            //DebugLogger.Log("ReorganizeNodes: Starting for node '{0}', child count: {1}, folder level: {2}", node.Text, node.Nodes.Count, folderLevel);

            if (!expanding && node.Nodes.Count < GetFolderLevelMinNodeCount(folderLevel))
                return 0;
            
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
            
            var nodeText = node.Text;
            node.Text += " (sorting...)";
            //node.TreeView.Update();

            var quickAndDirty = (Options.QuickSchema > 0 && node.Nodes.Count > Options.QuickSchema);

            //var sw = Stopwatch.StartNew();
            //sw.Restart();
            //debug_message("BeginUpdate:{0}", sw.ElapsedMilliseconds);

            node.TreeView.BeginUpdate();

            var unresponsive = Stopwatch.StartNew();

            //can't move nodes while iterating forward over them
            //create list of nodes to move then perform the update

            var folders = new Dictionary<string, List<TreeNode>>();
            int folderNodeIndex = -1;
            var newFolderNodes = new List<TreeNode>();

            //debug_message("Sort Nodes:{0}", sw.ElapsedMilliseconds);

            foreach (TreeNode childNode in node.Nodes.Cast<TreeNode>().ToList())
            {
                //DebugLogger.Log("ReorganizeNodes: Processing child node '{0}'", childNode.Text);
                //debug_message("  {0}:{1}", childNode.Text, sw.ElapsedMilliseconds);

                //skip schema node folders but make sure they are in the folders list
                if (childNode.Tag != null && childNode.Tag.ToString() == nodeTag)
                {
                    if (!folders.ContainsKey(childNode.Name))
                        folders.Add(childNode.Name, new List<TreeNode>());

                    folderNodeIndex = childNode.Index;

                    continue;
                }

                //debug_message("GetFolderName:begin:{0}", sw.ElapsedMilliseconds);

                string folderName = GetFolderName(childNode, folderLevel, quickAndDirty, expanding);
                //DebugLogger.Log("ReorganizeNodes: Child node '{0}' assigned to folder: '{1}'", childNode.Text, folderName ?? "null");

                //debug_message("GetFolderName:end:{0}", sw.ElapsedMilliseconds);

                if (string.IsNullOrEmpty(folderName))
                {
                    //DebugLogger.Log("ReorganizeNodes: Skipping child node '{0}' due to empty folder name.", childNode.Text);
                    
                    // Node did not match any folder pattern. If the user wants schema removed, rename it here.
                    if (Options.RenameNode)
                    {
                        RenameNode(childNode, quickAndDirty);
                    }

                    // Nothing else to do with this node.
                    continue;
                }

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
                    newFolderNodes.Add(folderNode);

                    folderNode.Name = folderName;
                    folderNode.Text = folderName;
                    folderNode.Tag = nodeTag;

                    if (Options.AppendDot)
                        folderNode.Text += ".";

                    // Assign icon – by default mimic SSMS folder, but override with green folder.
                    if (Options.UseObjectIcon && folderType != FolderType.Regex)
                    {
                        folderNode.ImageIndex = childNode.ImageIndex;
                        folderNode.SelectedImageIndex = childNode.ImageIndex;
                    }
                    else
                    {
                        folderNode.ImageIndex = node.ImageIndex;
                        folderNode.SelectedImageIndex = node.ImageIndex;
                    }

                    // Finally overwrite with the custom green icon
                    ApplyGreenFolderIcon(folderNode, node.TreeView);
                }

                //add node to folder list
                List<TreeNode> folderNodeList;
                if (!folders.TryGetValue(folderName, out folderNodeList))
                {
                    folderNodeList = new List<TreeNode>();
                    folders.Add(folderName, folderNodeList);
                }
                folderNodeList.Add(childNode);

                if (expanding && unresponsive.ElapsedMilliseconds > Options.UnresponsiveTimeout)
                {
                    node.TreeView.EndUpdate();
                    debug_message("Application.DoEvents():{0}", unresponsive.ElapsedMilliseconds);
                    Application.DoEvents();
                    if (node.TreeView == null)
                        return 0;
                    node.TreeView.BeginUpdate();
                    unresponsive.Restart();
                }
            }

            //debug_message("Move Nodes:{0}", sw.ElapsedMilliseconds);
            debug_message("Move Nodes");
            //DebugLogger.Log("ReorganizeNodes: Moving nodes to folders. Folder count: {0}", folders.Count);

            if (folderNodeIndex >= 0)
            {
                // Move schema nodes to top of tree
                foreach (var folderNode in newFolderNodes)
                {
                    node.Nodes.Remove(folderNode);
                    node.Nodes.Insert(++folderNodeIndex, folderNode);
                }
            }

            //BUG: Alphabetical may not be in order if no schema folder.
            bool sortRequired = (folderLevel == 1 && Options.Level1FolderType == FolderType.Alphabetical);

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
                        RenameNode(childNode, quickAndDirty);
                    }
                    folderNode.Nodes.Add(childNode);

                    if (expanding && unresponsive.ElapsedMilliseconds > Options.UnresponsiveTimeout)
                    {
                        node.TreeView.EndUpdate();
                        debug_message("Application.DoEvents():{0}", unresponsive.ElapsedMilliseconds);
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
            //DebugLogger.Log("ReorganizeNodes: Finished for node '{0}'", node.Text);

            //debug_message("Done:{0}", sw.ElapsedMilliseconds);
            //sw.Stop();

            //process next folder level
            if (folderLevel < 2)
            {
                foreach (string nodeName in folders.Keys)
                {
                    if (nodeName == "Other")
                    {
                        continue;
                    }
                    //debug_message("Next ReorganizeNodes: {1} > {0}", nodeName, folderLevel);
                    ReorganizeNodes(node.Nodes[nodeName], nodeTag, expanding, folderLevel + 1);
                }
            }

            // Finally sort current level (both folders and items) alphabetically when we are *not* grouping non-matches as "Other".
            bool groupOtherCurrent = (folderLevel == 1) ? Options.Level1GroupNonMatchingAsOther : Options.Level2GroupNonMatchingAsOther;
            if (!groupOtherCurrent)
            {
                var sorted = node.Nodes.Cast<TreeNode>()
                                         .OrderBy(n => n, new SchemaFolderNodeSorter())
                                         .ToArray();
                node.Nodes.Clear();
                node.Nodes.AddRange(sorted);
            }

            return folders.Count;
        }

        /// <summary>
        /// Create schema nodes and move tables, functions and stored procedures under its schema node
        /// </summary>
        /// <param name="node">Table node to reorganize</param>
        /// <param name="nodeTag">Tag of new node</param>
        /// <param name="expanding">Executing in AfterExpand event</param>
        /// <returns>The count of schema nodes.</returns>
        public int ReorganizeNodesWithClear(TreeNode node, string nodeTag, bool expanding)
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

                    // Assign icon – by default mimic SSMS folder, but override with green folder.
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

                    // Finally overwrite with the custom green icon
                    ApplyGreenFolderIcon(schemaNode, node.TreeView);
                }
            }

            //debug_message("Clear Nodes:{0}", sw.ElapsedMilliseconds);

            //node.TreeView.BeginUpdate();
            node.Text = nodeText + " (clearing...)";
            node.TreeView.Update();
            node.Nodes.Clear();

            //debug_message("DoEvents:{0}", sw.ElapsedMilliseconds);

            if (expanding && sw.ElapsedMilliseconds > Options.UnresponsiveTimeout)
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
            //Trace.WriteLine(string.Format(message, args));
            var output = (IDebugOutput)Package.GetService(typeof(IDebugOutput));
            output.debug_message(string.Format(message, args));
        }

    }

    public class SchemaFolderNodeSorter : System.Collections.IComparer, IComparer<TreeNode>
    {
        public int Compare(object x, object y)
        {
            return Compare(x as TreeNode, y as TreeNode);
        }

        public int Compare(TreeNode x, TreeNode y)
        {
            if (x == null) return (y == null) ? 0 : -1;
            if (y == null) return 1;

            // Treat the special "Other" folder as last regardless of punctuation.
            bool xIsOther = string.Equals(x.Text.TrimEnd('.'), "Other", StringComparison.OrdinalIgnoreCase);
            bool yIsOther = string.Equals(y.Text.TrimEnd('.'), "Other", StringComparison.OrdinalIgnoreCase);

            if (xIsOther && yIsOther) return 0;
            if (xIsOther) return 1;
            if (yIsOther) return -1;

            // Build sort keys so that:
            // 1. Trailing dots used on folder nodes are ignored.
            // 2. Schema prefixes like "dbo." are ignored, allowing tables and folders to mingle alphabetically by their logical name.
            string GetSortKey(TreeNode n)
            {
                if (n == null) return string.Empty;

                var text = n.Text?.TrimEnd('.') ?? string.Empty;

                // If there is a dot elsewhere (e.g. "dbo.TableName"), drop the prefix.
                int firstDot = text.IndexOf('.');
                if (firstDot >= 0 && firstDot < text.Length - 1)
                {
                    text = text.Substring(firstDot + 1);
                }

                return text;
            }

            return string.Compare(GetSortKey(x), GetSortKey(y), StringComparison.OrdinalIgnoreCase);
        }
    }
}
