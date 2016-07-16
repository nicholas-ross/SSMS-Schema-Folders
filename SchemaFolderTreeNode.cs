using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Drawing;

namespace SsmsSchemaFolders
{

    internal class SchemaFolderTreeNode : HierarchyTreeNode, INodeWithMenu, IServiceProvider
    //IServiceProvider causes the SchemaFolders.DoReorganization to run on itself
    {
        object parent;

        public SchemaFolderTreeNode(object o)
        {
            parent = o;
        }

        public override Icon Icon
        {
            get { return (parent as INodeWithIcon).Icon; }
        }

        public override Icon SelectedIcon
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

        public void ShowContextMenu(Point screenPos)
        {
            (parent as INodeWithMenu).ShowContextMenu(screenPos);
        }

    }

}
