using Microsoft.SqlServer.Management.SqlStudio.Explorer;
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
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SsmsSchemaFoldersPackage : Package
    {
        /// <summary>
        /// SsmsSchemaFoldersPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a88a775f-7c86-4a09-b5a6-890c4c38261b";

        ObjectExplorerService _objExplorerService;
        ObjectExplorerExtender _objectExplorerExtender;
        delegate void TrvEventAfterExpand(object obj, TreeViewEventArgs e);
        delegate void TrvEventBeforeExpand(object obj, TreeViewCancelEventArgs e);

        ContextService _cs;
        TreeView _trv = null;
        bool _attached = false;

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

            try
            {
                /* Microsoft.SqlServer.Management.UI.VSIntegration.ServiceCache
                 * is from SqlPackageBase.dll and not from Microsoft.SqlServer.SqlTools.VSIntegration.dll
                 * the code below just throws null exception if you have wrong reference */

                _objExplorerService = (ObjectExplorerService)this.GetService(typeof(IObjectExplorerService));
                foreach (var c in _objExplorerService.Container.Components)
                {
                    if (c is ContextService)
                    {
                        _cs = (ContextService)c;
                        _cs.ObjectExplorerContext.CurrentContextChanged += new NodesChangedEventHandler(ObjectExplorerContext_CurrentContextChanged);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                debug_message("Initialize::ERROR " + ex.Message);
            }

        }


        protected override int QueryClose(out bool canClose)
        {
            AddSkipLoadingReg();
            return base.QueryClose(out canClose);
        }

        private void AddSkipLoadingReg()
        {
            var myPackage = this.UserRegistryRoot.CreateSubKey(@"Packages\{" + SsmsSchemaFoldersPackage.PackageGuidString + "}");
            myPackage.SetValue("SkipLoading", 1);
        }

        #endregion


        /// <summary>
        /// when the object explorer is loaded and change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void ObjectExplorerContext_CurrentContextChanged(object sender, NodesChangedEventArgs args)
        {
            if (_attached) return;
            try
            {
                if (_trv == null)
                {
                    _trv = GetObjectExplorerTreeView();
                }

                if (_trv != null && !_trv.InvokeRequired)
                {
                    if (_objectExplorerExtender == null)
                    {
                        //-logger.LogStart("ObjectExplorerExtender");
                        _objectExplorerExtender = new ObjectExplorerExtender();
                        //logger.LogEnd("ObjectExplrerExtender");
                        if (_trv != null)
                        {
                            //logger.LogStart("ImageList");
                            //if (!_trv.ImageList.Images.ContainsKey("FolderSelected"))
                            {
                                //_trv.ImageList.Images.Add("FolderSelected", Resources.folder);
                                //_trv.ImageList.Images.Add("Table", Resources.tab_ico);
                                //_trv.ImageList.Images.Add("FolderDown", Resources.folder_closed);
                                //_trv.ImageList.Images.Add("FolderEdit", Resources.folder);
                                //_trv.ImageList.Images.Add("FunctionFun", Resources.fun_ico);

                                _trv.BeforeExpand += new TreeViewCancelEventHandler(_trv_BeforeExpand);
                                _trv.AfterExpand += new TreeViewEventHandler(_trv_AfterExpand);
                                //TODO: _trv.NodeMouseClick += new TreeNodeMouseClickEventHandler(_trv_NodeMouseClick);
                                _attached = true;
                            }
                            //logger.LogEnd("ImageList");
                        }
                    }
                }
                else if (_trv != null)
                {
                    //-logger.LogStart(" Inovke Required sender is " + (sender == null ? "null" : "not null"), "args is " +
                    //-(args == null ? "null" : "not null"));
                    //IntPtr ptr = _trv.Handle;
                    _trv.BeginInvoke(new NodesChangedEventHandler(ObjectExplorerContext_CurrentContextChanged), new object[] { sender, args });
                    //-logger.LogEnd("Invoke Required ");
                }
            }
            catch (Exception ObjectExplorerContextException)
            {
                debug_message(String.Format("ObjectExplorerContext_CurrentContextChanged::ERROR:{0}", ObjectExplorerContextException.Message));
            }
            // */
        }

        /// <summary>
        /// Gets the underlying object which is responsible for displaying object explorer structure
        /// </summary>
        /// <returns></returns>
        private TreeView GetObjectExplorerTreeView()
        {

            Type t = _objExplorerService.GetType();

            //FieldInfo field = t.GetField("Tree", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = t.GetField("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null)
            {
                return (TreeView)field.GetValue(_objExplorerService);
            }
            else
            {
                PropertyInfo pi = t.GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi != null) return (TreeView)pi.GetValue(_objExplorerService, null);
                return null;
            }
        }

        /// <summary>
        /// Adds new nodes and move items between them
        /// </summary>
        /// <param name="node"></param>
        void ReorganizeFolders(TreeNode node)
        {
            //-logger.LogStart(System.Reflection.MethodBase.GetCurrentMethod().Name);
            try
            {
                if (node != null && node.Parent != null && (node.Tag == null || node.Tag.ToString() != "SchemaFolder"))
                {
                    INodeInformation ni = _objectExplorerExtender.GetNodeInformation(node);
                    if (ni != null && !string.IsNullOrEmpty(ni.UrnPath))
                    {
                        //MessageBox.Show(ni.UrnPath);
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
                if (System.Diagnostics.Debugger.IsAttached)
                    MessageBox.Show(ex.ToString());
                else
                    MessageBox.Show(ex.ToString());
            }

            //-logger.LogEnd(System.Reflection.MethodBase.GetCurrentMethod().Name);
        }


        /// <summary>
        /// After expand node
        /// </summary>
        /// <param name="sender">object explorer</param>
        /// <param name="e">expanding node</param>
        void _trv_AfterExpand(object sender, TreeViewEventArgs e)
        {

            //-logger.LogStart(System.Reflection.MethodBase.GetCurrentMethod().Name);
            // Wait for the async node expand to finish or we could miss indexes
            try
            {
                HierarchyTreeNode htn = null;
                int cnt = 0;
                while ((htn = e.Node as HierarchyTreeNode) != null && htn.Expanding && cnt < 50000)
                {
                    Application.DoEvents();
                    cnt++;
                }
                if (_trv.InvokeRequired)
                    _trv.BeginInvoke(new TrvEventAfterExpand(_trv_AfterExpand), new object[] { sender, e });
                else
                    ReorganizeFolders(e.Node);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                debug_message(ex.ToString());
                debug_message(ex.StackTrace.ToString());
            }

            //-logger.LogEnd(System.Reflection.MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// Object explorer tree view: event before expand
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _trv_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            
            try
            {
                if (_trv.InvokeRequired)
                    _trv.BeginInvoke(new TrvEventBeforeExpand(_trv_BeforeExpand), new object[] { sender, e });
                else
                    ReorganizeFolders(e.Node);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                MessageBox.Show(ex.ToString());
            }
            
        }

        private void debug_message(string message)
        {
            //*
            VsShellUtilities.ShowMessageBox(
                this,
                message,
                "Connect.cs",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            // */
        }

    }
}
