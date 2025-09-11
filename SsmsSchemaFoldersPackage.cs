﻿extern alias Ssms18;
extern alias Ssms19;
extern alias Ssms20;
extern alias Ssms21;
extern alias Ssms22;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace SsmsSchemaFolders
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SsmsSchemaFoldersPackage.PackageGuidString)]
    [ProvideAutoLoad("d114938f-591c-46cf-a785-500a82d97410", PackageAutoLoadFlags.BackgroundLoad)] //CommandGuids.ObjectExplorerToolWindowIDString
    [ProvideOptionPage(typeof(SchemaFolderOptions), "SQL Server Object Explorer", "Schema Folders", 114, 116, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SsmsSchemaFoldersPackage : AsyncPackage, IDebugOutput
    {
        /// <summary>
        /// SsmsSchemaFoldersPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a88a775f-7c86-4a09-b5a6-890c4c38261b";
        public static readonly Guid PackageGuid = new Guid(SsmsSchemaFoldersPackage.PackageGuidString);

        public const string SchemaFolderNodeTag = "SchemaFolder";

        public SchemaFolderOptions Options { get; set; }
        
        private IObjectExplorerExtender _objectExplorerExtender;

        private IVsOutputWindowPane _outputWindowPane = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsmsSchemaFoldersPackage"/> class.
        /// </summary>
        public SsmsSchemaFoldersPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // OutputWindowPane for debug messages
#if DEBUG
            var outputWindow = (IVsOutputWindow)GetService(typeof(SVsOutputWindow));
            var guidPackage = new Guid(PackageGuidString);
            outputWindow.CreatePane(guidPackage, "Schema Folders debug output", 1, 0);
            outputWindow.GetPane(ref guidPackage, out _outputWindowPane);
#endif

            // Link with VS options.
            object obj;
            (this as IVsPackage).GetAutomationObject("SQL Server Object Explorer.Schema Folders", out obj);
            Options = (SchemaFolderOptions)obj;

            _objectExplorerExtender = GetObjectExplorerExtender();

            if (_objectExplorerExtender != null)
                AttachTreeViewEvents();

        }

        private IObjectExplorerExtender GetObjectExplorerExtender()
        {
            try
            {
                var ssmsInterfacesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqlWorkbench.Interfaces.dll");

                if (File.Exists(ssmsInterfacesPath))
                {
                    var ssmsInterfacesVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(ssmsInterfacesPath);

                    switch (ssmsInterfacesVersion.FileMajorPart)
                    {
                        case 22:
                            debug_message("SsmsVersion:22");
                            return new Ssms22::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        case 21:
                            debug_message("SsmsVersion:21");
                            return new Ssms21::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        case 20:
                            debug_message("SsmsVersion:20");
                            return new Ssms20::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        case 19: // v19.1 changed file version numbers
                        case 16:
                            debug_message("SsmsVersion:19");
                            return new Ssms19::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        case 15:
                            debug_message("SsmsVersion:18");
                            return new Ssms18::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        default:
                            ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, String.Format("SqlWorkbench.Interfaces.dll v{0}:{1}", ssmsInterfacesVersion.FileMajorPart, ssmsInterfacesVersion.FileMinorPart));
                            break;
                    }
                }

                ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, "Unknown SSMS Version. Defaulting to 22.");
                return new Ssms22::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);
            }
            catch (Exception ex)
            {
                ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, ex.ToString());
                return null;
            }
        }

        private void AttachTreeViewEvents()
        {
            var treeView = _objectExplorerExtender.GetObjectExplorerTreeView();
            if (treeView != null)
            {
                treeView.BeforeExpand += new TreeViewCancelEventHandler(ObjectExplorerTreeViewBeforeExpandCallback);
                treeView.AfterExpand += new TreeViewEventHandler(ObjectExplorerTreeViewAfterExpandCallback);
            }
            else
                ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "Object Explorer TreeView == null");
        }

        /// <summary>
        /// Adds new nodes and move items between them
        /// </summary>
        /// <param name="node"></param>
        /// <param name="expanding">Executing in AfterExpand event</param>
        private void ReorganizeFolders(TreeNode node, bool expanding = false)
        {
            debug_message("ReorganizeFolders");
            try
            {
                // uses node.Tag to prevent this running again on already orgainsed schema folder
                if (node != null && node.Parent != null && (node.Tag == null || node.Tag.ToString() != SchemaFolderNodeTag))
                {
                    var urnPath = _objectExplorerExtender.GetNodeUrnPath(node);
                    if (!string.IsNullOrEmpty(urnPath))
                    {
                        switch (urnPath)
                        {
                            case "Server/Database/UserTablesFolder":
                            case "Server/Database/ViewsFolder":
                            case "Server/Database/SynonymsFolder":
                            case "Server/Database/StoredProceduresFolder":
                            case "Server/Database/Table-valuedFunctionsFolder":
                            case "Server/Database/Scalar-valuedFunctionsFolder":
                            case "Server/Database/SystemTablesFolder":
                            case "Server/Database/SystemViewsFolder":
                            case "Server/Database/SystemStoredProceduresFolder":
                                //node.TreeView.Cursor = Cursors.WaitCursor;
                                var schemaFolderCount = _objectExplorerExtender.ReorganizeNodes(node, SchemaFolderNodeTag, expanding);
                                if (expanding && schemaFolderCount == 1)
                                {
                                    node.LastNode.Expand();
                                }
                                //node.TreeView.Cursor = Cursors.Default;
                                break;

                            case "Server/Database/Table":
                            case "Server/Database/View":
                            case "Server/Database/Synonym":
                            case "Server/Database/StoredProcedure":
                            case "Server/Database/UserDefinedFunction":
                                if (Options.RenameNode)
                                {
                                    debug_message(node.Text);
                                    _objectExplorerExtender.RenameNode(node);
                                }
                                break;

                            default:
                                // Server/DatabasesFolder
                                // Server/Database
                                // Server/JobServer
                                // Server/JobServer/JobsFolder
                                debug_message(urnPath);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var error = ex.ToString();
                ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, error);
                MessageBox.Show(error,"SSMS Schema Folders", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// After expand node
        /// </summary>
        /// <param name="sender">object explorer</param>
        /// <param name="e">expanding node</param>
        void ObjectExplorerTreeViewAfterExpandCallback(object sender, TreeViewEventArgs e)
        {
            debug_message("\nObjectExplorerTreeViewAfterExpandCallback:{0}:{1}", e.Node.Text, Form.ModifierKeys);
            // Wait for the async node expand to finish or we could miss nodes
            try
            {
                debug_message("Node.Count:{0}", e.Node.GetNodeCount(false));

                if (Options.EnabledModifierKeys != Keys.None)
                {
                    var modifierKeys = Options.EnabledModifierKeys & Keys.Modifiers;
                    if (Options.Enabled && Form.ModifierKeys == modifierKeys
                        || !Options.Enabled && Form.ModifierKeys != modifierKeys)
                        return;
                }
                else if (!Options.Enabled)
                    return;

                if (e.Node.TreeView.InvokeRequired)
                    debug_message("TreeView.InvokeRequired");

                if (_objectExplorerExtender.GetNodeExpanding(e.Node))
                {
                    debug_message("node.Expanding");
                    //debug_message(DateTime.Now.ToString("ss.fff"));
                    var waitCount = 0;

                    //IExplorerHierarchy.EndAsynchronousUpdate += New EventHandler();

                    //e.Node.TreeView.Cursor = Cursors.WaitCursor;
                    e.Node.TreeView.Cursor = Cursors.AppStarting;

                    var nodeExpanding = new System.Windows.Forms.Timer();
                    nodeExpanding.Interval = 10;
                    EventHandler nodeExpandingEvent = null;
                    nodeExpandingEvent = (object o, EventArgs e2) =>
                    {
                        debug_message("nodeExpanding:{0}", waitCount);
                        waitCount++;
                        if (e.Node.TreeView.InvokeRequired)
                            debug_message("TreeView.InvokeRequired");
                        debug_message("Node.Count:{0}", e.Node.GetNodeCount(false));
                        //debug_message(DateTime.Now.ToString("ss.fff"));

                        if (!_objectExplorerExtender.GetNodeExpanding(e.Node))
                        {
                            nodeExpanding.Tick -= nodeExpandingEvent;
                            nodeExpanding.Stop();
                            nodeExpanding.Dispose();

                            ReorganizeFolders(e.Node, true);

                            if (e.Node.TreeView != null)
                                e.Node.TreeView.Cursor = Cursors.Default;
                        }
                        else
                        {
                            //ReorganizeFolders(e.Node);
                        }
                        //debug_message("Node.Count2:{0}", e.Node.GetNodeCount(false));
                    };
                    nodeExpanding.Tick += nodeExpandingEvent;
                    nodeExpanding.Start();

                }
                else
                {
                    debug_message("node.Expanded");
                    //ReorganizeFolders(e.Node, true);
                }
            }
            catch (Exception ex)
            {
                var error = ex.ToString();
                ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, error);
                MessageBox.Show(error, "SSMS Schema Folders", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Object explorer tree view: event before expand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ObjectExplorerTreeViewBeforeExpandCallback(object sender, TreeViewCancelEventArgs e)
        {
            debug_message("\nObjectExplorerTreeViewBeforeExpandCallback:{0}:{1}", e.Node.Text, Form.ModifierKeys);
            try
            {
                if (Options.EnabledModifierKeys != Keys.None)
                {
                    var modifierKeys = Options.EnabledModifierKeys & Keys.Modifiers;
                    if (Options.Enabled && Form.ModifierKeys == modifierKeys
                        || !Options.Enabled && Form.ModifierKeys != modifierKeys)
                        return;
                }
                else if (!Options.Enabled)
                    return;

                var nodeCount = e.Node.GetNodeCount(false);

                debug_message("Node.Count:{0}", nodeCount);

                if (nodeCount == 1)
                    return;

                if (_objectExplorerExtender.GetNodeExpanding(e.Node))
                {
                    debug_message("node.Expanding");
                    //foreach(TreeNode node in e.Node.Nodes)
                    //{
                    //    debug_message(node.Text);
                    //}
                    //doing a reorg before expand stops the treeview from jumping
                    ReorganizeFolders(e.Node);
                }
                else
                {
                    debug_message("node.Expanded");
                }
            }
            catch (Exception ex)
            {
                var error = ex.ToString();
                ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, error);
                MessageBox.Show(error, "SSMS Schema Folders", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        public void debug_message(string message)
        {
            if (_outputWindowPane != null)
            {
                _outputWindowPane.OutputString(message);
                _outputWindowPane.OutputString("\r\n");
            }
        }

        public void debug_message(string message, params object[] args)
        {
            if (_outputWindowPane != null)
            {
                _outputWindowPane.OutputString(String.Format(message, args));
                _outputWindowPane.OutputString("\r\n");
            }
        }

        private void ActivityLogEntry(__ACTIVITYLOG_ENTRYTYPE entryType, string message)
        {
            debug_message(message);

            // Logs to %AppData%\Microsoft\AppEnv\15.0\ActivityLog.XML.
            // Recommended to obtain the activity log just before writing to it. Do not cache or save the activity log for future use.
            var log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            if (log == null) return;

            int hr = log.LogEntryGuid(
                (UInt32)entryType,
                this.ToString(),
                message,
                SsmsSchemaFoldersPackage.PackageGuid);
        }

    }
}
