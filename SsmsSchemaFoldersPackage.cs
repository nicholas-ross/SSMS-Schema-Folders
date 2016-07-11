using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SsmsSchemaFoldersPackage.PackageGuidString)]
    [ProvideAutoLoad(Microsoft.SqlServer.Management.UI.VSIntegration.CommandGuids.ObjectExplorerToolWindowIDString)]
    [ProvideOptionPage(typeof(SchemaFolderOptions), "SQL Server Object Explorer", "Schema Folders", 114, 116, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SsmsSchemaFoldersPackage : Package
    {
        /// <summary>
        /// SsmsSchemaFoldersPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a88a775f-7c86-4a09-b5a6-890c4c38261b";

        public SchemaFolderOptions Options { get; set; }

        IObjectExplorerService _objExplorerService;
        ObjectExplorerExtender _objectExplorerExtender;

        // Ignore never assigned to warning for release build.
#pragma warning disable CS0649
        private IVsOutputWindowPane OutputWindowPane;
#pragma warning restore CS0649

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

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // OutputWindowPane for debug messages
#if DEBUG
            var outputWindow = this.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var guidPackage = new Guid(PackageGuidString);
            outputWindow.CreatePane(guidPackage, "Schema Folders debug output", 1, 0);
            outputWindow.GetPane(ref guidPackage, out OutputWindowPane);
#endif

            // Link with VS options.
            object obj;
            (this as IVsPackage).GetAutomationObject("SQL Server Object Explorer.Schema Folders", out obj);
            Options = obj as SchemaFolderOptions;

            try
            {
                /* Microsoft.SqlServer.Management.UI.VSIntegration.ServiceCache
                 * is from SqlPackageBase.dll and not from Microsoft.SqlServer.SqlTools.VSIntegration.dll
                 * the code below just throws null exception if you have wrong reference */

                _objExplorerService = (IObjectExplorerService)this.GetService(typeof(IObjectExplorerService));

                _objectExplorerExtender = new ObjectExplorerExtender(Options);

                AttachTreeViewEvents();

            }
            catch (Exception ex)
            {
                debug_message("Initialize::ERROR " + ex.Message);
            }

            // Reg setting is removed after initialize. Wait short delay then recreate it.
            DelayAddSkipLoadingReg();
        }


        //protected override int QueryClose(out bool canClose)
        //{
        //    AddSkipLoadingReg();
        //    return base.QueryClose(out canClose);
        //}

        private void AddSkipLoadingReg()
        {
            var myPackage = this.UserRegistryRoot.CreateSubKey(@"Packages\{" + SsmsSchemaFoldersPackage.PackageGuidString + "}");
            myPackage.SetValue("SkipLoading", 1);
        }

        private void DelayAddSkipLoadingReg()
        {
            var delay = new Timer();
            delay.Tick += delegate (object o, EventArgs e)
            {
                delay.Stop();
                AddSkipLoadingReg();
            };
            delay.Interval = 1000;
            delay.Start();
        }

        #endregion

        void AttachTreeViewEvents()
        {
            var treeView = GetObjectExplorerTreeView();
            treeView.BeforeExpand += new TreeViewCancelEventHandler(ObjectExplorerTreeViewBeforeExpandCallback);
            treeView.AfterExpand += new TreeViewEventHandler(ObjectExplorerTreeViewAfterExpandCallback);
        }

        /// <summary>
        /// Gets the underlying object which is responsible for displaying object explorer structure
        /// </summary>
        /// <returns></returns>
        private TreeView GetObjectExplorerTreeView()
        {
            Type t = _objExplorerService.GetType();
            PropertyInfo pi = t.GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi != null)
                return (TreeView)pi.GetValue(_objExplorerService, null);
            else
                return null;
        }

        /// <summary>
        /// Adds new nodes and move items between them
        /// </summary>
        /// <param name="node"></param>
        void ReorganizeFolders(TreeNode node)
        {
            debug_message("ReorganizeFolders");
            if (!Options.Enabled)
                return;
            try
            {
                if (node != null && node.Parent != null && (node.Tag == null || node.Tag.ToString() != "SchemaFolder"))
                {
                    INodeInformation ni = _objectExplorerExtender.GetNodeInformation(node);
                    if (ni != null && !string.IsNullOrEmpty(ni.UrnPath))
                    {
                        debug_message(ni.UrnPath);
                        switch (ni.UrnPath)
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
                                _objectExplorerExtender.ReorganizeNodes(node, "SchemaFolder", string.Empty);
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                debug_message(ex.ToString());
            }
        }


        /// <summary>
        /// After expand node
        /// </summary>
        /// <param name="sender">object explorer</param>
        /// <param name="e">expanding node</param>
        void ObjectExplorerTreeViewAfterExpandCallback(object sender, TreeViewEventArgs e)
        {
            debug_message("ObjectExplorerTreeViewAfterExpandCallback");
            // Wait for the async node expand to finish or we could miss nodes
            try
            {
                var node = e.Node as ILazyLoadingNode;
                int waitCount = 0;
                while (node != null && node.Expanding && waitCount < 50000)
                {
                    Application.DoEvents();
                    waitCount++;
                }
                debug_message(String.Format("node.Expanding  waitCount:{0}", waitCount));

                ReorganizeFolders(e.Node);
            }
            catch (Exception ex)
            {
                debug_message(ex.ToString());
            }
        }

        /// <summary>
        /// Object explorer tree view: event before expand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ObjectExplorerTreeViewBeforeExpandCallback(object sender, TreeViewCancelEventArgs e)
        {
            debug_message("ObjectExplorerTreeViewBeforeExpandCallback");
            try
            {
                ReorganizeFolders(e.Node);
            }
            catch (Exception ex)
            {
                debug_message(ex.ToString());
            }
            
        }

        private void debug_message(string message)
        {
            if (OutputWindowPane != null)
            {
                OutputWindowPane.OutputString(message);
                OutputWindowPane.OutputString("\r\n");
            }
            /*
            VsShellUtilities.ShowMessageBox(
                this,
                message,
                "Schema Folders",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            // */
        }

    }
}
