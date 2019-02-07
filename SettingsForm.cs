using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Aveva.Pdms.Database;
using Aveva.PDMS.Database.Filters;

namespace Polymetal.Pdms.Design.DrawListManager
{
    public partial class SettingsForm : Form
    {
        private bool _permitCheck;
        private static readonly DbElementType[] Types =
        {DbElementTypeInstance.EQUIPMENT, DbElementTypeInstance.BRANCH, DbElementTypeInstance.SCTN,
         DbElementTypeInstance.FLOOR,DbElementTypeInstance.GWALL,  DbElementTypeInstance.PANEL, 
         DbElementTypeInstance.CABLE, DbElementTypeInstance.CWBRAN};
        internal List<DbElement> ListofElements = new List<DbElement>(); 

        public SettingsForm()
        {
            InitializeComponent();
            FillTreeView();
        }

        #region Events

        private void TreeViewFilterAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_permitCheck)
                return;

            _permitCheck = true;

            CheckParent(e.Node);
            CheckChilds(e.Node, e.Node.Checked);

            _permitCheck = false;
        }

        private static void CheckChilds(TreeNode root, bool check)
        {
            foreach (TreeNode node in root.Nodes)
            {
                node.Checked = check;
                CheckChilds(node, check);
            }
        }

        private static void CheckParent(TreeNode child)
        {
            var root = child.Parent;
            if (root == null) return;

            var checkedChilds = 0;
            foreach (TreeNode node in root.Nodes)
            {
                if (node.Checked)
                {
                    checkedChilds++;
                }
            }

            var rChecked = root.Checked;
            root.Checked = checkedChilds == root.Nodes.Count;
            if (rChecked != root.Checked)
                CheckParent(root);
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CheckedElements();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            foreach (TreeNode elem in treeView1.Nodes)
            {
                elem.Expand();
            }
        }

        #endregion
        

        #region Functions

        private void FillTreeView()
        {
            treeView1.Nodes.Clear();
            treeView2.Nodes.Clear();
            var world = DbElement.GetElement("/*");

            var siteList = new List<DbElement>();
            var projList = new List<string>();

            foreach (var site in world.Members(DbElementTypeInstance.SITE))
            {
                if (!site.GetAsString(DbAttributeInstance.PURP).ToUpper().Equals("JOB")) continue;

                siteList.Add(site);
                var projName = site.GetAsString(DbAttributeInstance.FLNN).Split('-')[0];
                if (projList.Contains(projName)) continue;
                projList.Add(projName);
            }

            foreach (var projName in projList)
            {
                var proektSite = "Проект " + projName;
                var proekts = treeView1.Nodes.Add(proektSite);

                proekts.ImageIndex = -1;
                proekts.SelectedImageIndex = -1;

                foreach (var site in siteList)
                {
                    var siteName = site.GetAsString(DbAttributeInstance.FLNN);
                    if (!siteName.StartsWith(projName)) continue;

                    var splitName = siteName.Split('-');

                    var otdelName = splitName.Length > 1 ? "Отдел " + splitName[1] : siteName;
                    var otdels = proekts.Nodes.Add(site.GetAsString(DbAttributeInstance.REF), otdelName, 1, 1);

                    foreach (var zone in site.Members(DbElementTypeInstance.ZONE))
                    {
                        var spec = zone.GetAsString(DbAttributeInstance.FLNN).Replace(site.GetAsString(DbAttributeInstance.FLNN), "");
                        otdels.Nodes.Add(zone.GetAsString(DbAttributeInstance.REF), spec.Substring(spec.IndexOf("-", StringComparison.Ordinal) + 1), 2, 2);
                    }
                }
                proekts.Expand();
            }

            foreach (var dbtype in Types)
            {
                var node = treeView2.Nodes.Add(dbtype.ToString());
                node.Checked = !dbtype.Equals(DbElementTypeInstance.CABLE) & !dbtype.Equals(DbElementTypeInstance.CWBRAN);
            }

        }

        internal List<DbElement> GetCheckedRoots()
        {
            var list = new List<DbElement>();

            foreach (TreeNode proj in treeView1.Nodes)
            {
                foreach (TreeNode site in proj.Nodes)
                {
                    if (site.Checked)
                    {
                        var item = DbElement.GetElement(site.Name);
                        if (!item.IsNull)
                            list.Add(item);

                    }
                    else
                    {
                        foreach (TreeNode zone in site.Nodes)
                        {
                            if (!zone.Checked) continue;

                            var item = DbElement.GetElement(zone.Name);
                            if (!item.IsNull)
                                list.Add(item);
                        }
                    }
                }
            }

            return list;
        }

        private List<DbElement> GetUnCheckedRoots()
        {
            var list = new List<DbElement>();

            foreach (TreeNode element in treeView1.Nodes)
            {
                foreach (TreeNode node in element.Nodes)
                {
                    var item = DbElement.GetElement(node.Name);
                    if (!item.IsNull)
                        list.Add(item);
                }
            }
            return list;
        }

        internal List<DbElementType> GetCheckedTypes()
        {
            var list = new List<DbElementType>();

            foreach (TreeNode element in treeView2.Nodes)
            {
                if (element.Checked)
                {
                    list.Add(DbElementType.GetElementType(element.Text));
                }
            }
            return list;
        }

        private void CheckedElements()
        {
            ListofElements = new List<DbElement>();
            var roots = GetCheckedRoots();

            var typeFilterList = new List<DbElementType>();
            foreach (TreeNode element in treeView2.Nodes)
            {
                if (element.Checked)
                {
                    typeFilterList.Add(DbElementType.GetElementType(element.Text));
                }
            }

            foreach (var root in roots)
            {
                ListofElements.AddRange(new DBElementCollection(root, new TypeFilter(typeFilterList.ToArray())).Cast<DbElement>().ToList());
            }
        }
        
        internal void CheckAllElements()
        {
            ListofElements = new List<DbElement>();
            var roots = GetUnCheckedRoots();

            var typeFilterList = new List<DbElementType>();
            foreach (TreeNode element in treeView2.Nodes)
            {
                if (element.Checked)
                {
                    typeFilterList.Add(DbElementType.GetElementType(element.Text));
                }
            }

            foreach (var root in roots)
            {
                ListofElements.AddRange(new DBElementCollection(root, new TypeFilter(typeFilterList.ToArray())).Cast<DbElement>().ToList());
            }
        }

        #endregion


    }
}
