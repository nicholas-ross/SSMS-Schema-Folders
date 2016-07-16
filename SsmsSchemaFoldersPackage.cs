extern alias Ssms2012;
extern alias Ssms2014;
extern alias Ssms2016;
//using Microsoft.SqlServer.Management.UI.VSIntegration;
//using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
//using Ssms2016 = Ssms2016::SsmsSchemaFolders;

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
    [ProvideAutoLoad("d114938f-591c-46cf-a785-500a82d97410")] //CommandGuids.ObjectExplorerToolWindowIDString
    [ProvideOptionPage(typeof(SchemaFolderOptions), "SQL Server Object Explorer", "Schema Folders", 114, 116, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SsmsSchemaFoldersPackage : Package
    {
        /// <summary>
        /// SsmsSchemaFoldersPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a88a775f-7c86-4a09-b5a6-890c4c38261b";

        public SchemaFolderOptions Options { get; set; }
        
        private IObjectExplorerExtender _objectExplorerExtender;

        // Ignore never assigned to warning for release build.
#pragma warning disable CS0649
        private IVsOutputWindowPane _outputWindowPane;
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

            AttachTreeViewEvents();

            // Reg setting is removed after initialize. Wait short delay then recreate it.
            DelayAddSkipLoadingReg();
        }

        //protected override int QueryClose(out bool canClose)
        //{
        //    AddSkipLoadingReg();
        //    return base.QueryClose(out canClose);
        //}

        #endregion

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

        private IObjectExplorerExtender GetObjectExplorerExtender()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName();
                //debug_message(assemblyName.Name + ":" + assemblyName.Version.ToString());

                if (assemblyName.Name == "SqlWorkbench.Interfaces") // && BitConverter.ToString(name.GetPublicKeyToken()) == "89-84-5D-CD-80-80-CC-91")
                {

                    switch (assemblyName.Version.ToString())
                    {
                        case "13.0.0.0":
                            debug_message("SsmsVersion:2016");
                            return new Ssms2016::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        case "12.0.0.0":
                            debug_message("SsmsVersion:2014");
                            return new Ssms2014::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        case "11.0.0.0":
                            debug_message("SsmsVersion:2012");
                            return new Ssms2012::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);

                        default:
                            debug_message("SqlWorkbench.Interfaces:" + assemblyName.Version.ToString());
                            break;
                    }
                }
            }

            debug_message("Unknown SSMS Version. Defaulting to 2016.");
            return new Ssms2016::SsmsSchemaFolders.ObjectExplorerExtender(this, Options);
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
                debug_message("Object Explorer TreeView == null");
        }

        /// <summary>
        /// Adds new nodes and move items between them
        /// </summary>
        /// <param name="node"></param>
        private void ReorganizeFolders(TreeNode node)
        {
            debug_message("ReorganizeFolders");
            try
            {
                // uses node.Tag to prevent this running again on already orgainsed schema folder
                if (node != null && node.Parent != null && (node.Tag == null || node.Tag.ToString() != "SchemaFolder"))
                {
                    var urnPath = _objectExplorerExtender.GetNodeUrnPath(node);
                    if (!string.IsNullOrEmpty(urnPath))
                    {
                        //debug_message(String.Format("NodeInformation\n UrnPath:{0}\n Name:{1}\n InvariantName:{2}\n Context:{3}\n NavigationContext:{4}", ni.UrnPath, ni.Name, ni.InvariantName, ni.Context, ni.NavigationContext));
                        
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
                                _objectExplorerExtender.ReorganizeNodes(node, "SchemaFolder");
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
            debug_message("\nObjectExplorerTreeViewAfterExpandCallback");
            // Wait for the async node expand to finish or we could miss nodes
            try
            {
                debug_message(String.Format("Node.Count:{0}", e.Node.GetNodeCount(false)));

                if (!Options.Enabled)
                    return;

                if (_objectExplorerExtender.GetNodeExpanding(e.Node))
                {
                    debug_message("node.Expanding");
                    Application.DoEvents();
                    debug_message(String.Format("Node.Count:{0}", e.Node.GetNodeCount(false)));
                }

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
            debug_message("\nObjectExplorerTreeViewBeforeExpandCallback");
            try
            {
                debug_message(String.Format("Node.Count:{0}",e.Node.GetNodeCount(false)));

                if (!Options.Enabled)
                    return;

                if (e.Node.GetNodeCount(false) == 1)
                    return;

                ReorganizeFolders(e.Node);
            }
            catch (Exception ex)
            {
                debug_message(ex.ToString());
            }
            
        }

        private void debug_message(string message)
        {
            if (_outputWindowPane != null)
            {
                _outputWindowPane.OutputString(message);
                _outputWindowPane.OutputString("\r\n");
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
