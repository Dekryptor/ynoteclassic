using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SS.Ynote.Classic.Features.RunScript;
using WeifenLuo.WinFormsUI.Docking;

namespace SS.Ynote.Classic.Features.Project
{
    public partial class ProjectPanel : DockContent, IProjectPanel
    {
        #region Private Variables

        /// <summary>
        ///     Gets the List of Open Projects
        /// </summary>
        private readonly IList<YnoteProject> _openprojects;

        /// <summary>
        ///     Ynote Reference to the Object
        /// </summary>
        private readonly IYnote _ynote;

        /// <summary>
        ///     Recent Projects
        /// </summary>
        private IList<string> RecentProjects { get; set; }

        #endregion Private Variables

        #region Constructor

        /// <summary>
        ///     Default Constructor
        /// </summary>
        /// <param name="ynote"></param>
        public ProjectPanel(IYnote ynote)
        {
            InitializeComponent();
            _ynote = ynote;
            _openprojects = new List<YnoteProject>();
            RecentProjects = GetUserProjects();
        }

        #endregion Constructor

        #region Methods

        public void OpenProject(string filename)
        {
            if (!File.Exists(filename) ||
                string.IsNullOrEmpty(filename)) return;
            var proj = YnoteProject.Read(filename);
            OpenProject(proj);
        }

        /// <summary>
        ///     Opens a project file in the Project Explorer
        /// </summary>
        /// <param name="filename"></param>
        private void OpenProject(YnoteProject project)
        {
            // initialize the node
            var projectnode = new ExTreeNode(project.ProjectName, project.Folder, 2, 2, project, ProjectNodeType.Project);
            if (!Directory.Exists(project.Folder))
                MessageBox.Show("Error : Can't find directory : " + project.Folder, "Project Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            else
                BeginInvoke((MethodInvoker) (() => ListDirectory(projectnode, project.Folder)));
            projtree.Nodes.Add(projectnode);
            _openprojects.Add(project);
        }

        /// <summary>
        ///     Lists a Directory in Project Explorer
        /// </summary>
        /// <param name="iTreeNode"></param>
        /// <param name="path"></param>
        private static void ListDirectory(TreeNode iTreeNode, string path)
        {
            var stack = new Stack<TreeNode>();
            var rootDirectory = new DirectoryInfo(path);
            var node = new ExTreeNode(rootDirectory.Name, path, 0, 0, rootDirectory, ProjectNodeType.Folder);
            stack.Push(node);

            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();
                var directoryInfo = (DirectoryInfo) currentNode.Tag;
                for (var i = 0; i < directoryInfo.GetDirectories().Length; i++)
                {
                    var directory = directoryInfo.GetDirectories()[i];
                    var childDirectoryNode = new ExTreeNode(directory.Name, directory.FullName, 0, 0, directory,
                        ProjectNodeType.Folder);
                    currentNode.Nodes.Add(childDirectoryNode);
                    stack.Push(childDirectoryNode);
                }
                for (var i = 0; i < directoryInfo.GetFiles().Length; i++)
                {
                    var file = directoryInfo.GetFiles()[i];
                    if (Path.GetExtension(file.FullName) != ".ynoteproj")
                        currentNode.Nodes.Add(new ExTreeNode(file.Name, file.FullName, 1, 1, null, ProjectNodeType.File));
                }
            }

            iTreeNode.Nodes.Add(node);
        }

        /// <summary>
        ///     Adds a New Folder to existing folder
        /// </summary>
        private void AddNewFolder()
        {
            var path = projtree.SelectedNode as ExTreeNode;
            if (path != null && path.Type == ProjectNodeType.Folder)
            {
                using (var util = new ProjectUtils())
                {
                    if (util.ShowDialog(this) != DialogResult.OK) return;
                    var dir = Path.Combine(path.Name, util.FileName);
                    var node = new ExTreeNode(util.FileName, dir, 0, 0, null, ProjectNodeType.Folder);
                    Directory.CreateDirectory(dir);
                    projtree.SelectedNode.Nodes.Add(node);
                }
            }
        }

        /// <summary>
        ///     Finds the root Node
        /// </summary>
        /// <param name="treeNode"></param>
        /// <returns></returns>
        private static TreeNode FindRootNode(TreeNode treeNode)
        {
            while (treeNode.Parent != null)
                treeNode = treeNode.Parent;
            return treeNode;
        }

        /// <summary>
        ///     Delete File/Folder
        /// </summary>
        private void DeleteFile()
        {
            var node = projtree.SelectedNode;
            var result = MessageBox.Show("Are you sure you want to delete " + node.Name + " ?", "Project",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (IsDir(node.Name) && result == DialogResult.Yes)
            {
                Directory.Delete(node.Name, true);
                projtree.Nodes.Remove(node);
            }
            else if (!IsDir(node.Name) && result == DialogResult.Yes)
            {
                File.Delete(node.Name);
                projtree.Nodes.Remove(node);
            }
        }

        /// <summary>
        ///     Renames Directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newpath"></param>
        /// <param name="node"></param>
        private void RenameDirectory(string path, string newpath, ExTreeNode node)
        {
            Directory.Move(path, newpath);
            node.Text = Path.GetFileName(newpath);
            node.Name = newpath;
            RefreshProjects();
        }

        /// <summary>
        ///     Rename
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newpath"></param>
        /// <param name="node"></param>
        private static void RenameFile(string path, string newpath, ExTreeNode node)
        {
            File.Move(path, newpath);
            node.Name = newpath;
        }

        /// <summary>
        ///     DoRename
        /// </summary>
        private void DoRename()
        {
            var node = projtree.SelectedNode as ExTreeNode;
            var filename = projtree.SelectedNode.Name;
            var dir = Path.GetDirectoryName(projtree.SelectedNode.Name);
            using (var dlg = new ProjectUtils())
            {
                var result = dlg.ShowDialog() == DialogResult.OK;
                if (result)
                {
                    if (node.Type == ProjectNodeType.Folder)
                        RenameDirectory(filename, dir + @"\" + dlg.FileName, node);
                    else if (node.Type == ProjectNodeType.File)
                        RenameFile(filename, dir + @"\" + dlg.FileName, node);
                    node.Text = dlg.FileName;
                }
            }
        }

        /// <summary>
        ///     Copies a directory from strSource to strDestination
        /// </summary>
        /// <param name="strSource"></param>
        /// <param name="strDestination"></param>
        private static void CopyDirectory(string strSource, string strDestination)
        {
            if (!Directory.Exists(strDestination))
            {
                Directory.CreateDirectory(strDestination);
            }
            var dirInfo = new DirectoryInfo(strSource);
            var files = dirInfo.GetFiles();
            foreach (var tempfile in files)
            {
                tempfile.CopyTo(Path.Combine(strDestination, tempfile.Name));
            }
            var dirctororys = dirInfo.GetDirectories();
            foreach (var tempdir in dirctororys)
                CopyDirectory(Path.Combine(strSource, tempdir.Name), Path.Combine(strDestination, tempdir.Name));
        }

        /// <summary>
        ///     Add New File
        /// </summary>
        private void AddNewFile()
        {
            try
            {
                var path = projtree.SelectedNode as ExTreeNode;
                if (path != null && path.Type == ProjectNodeType.Folder)
                {
                    using (var util = new ProjectUtils())
                    {
                        if (util.ShowDialog(this) != DialogResult.OK) return;
                        var file = Path.Combine(path.Name, util.FileName);
                        var node = new ExTreeNode(util.FileName, file, 1, 1, null, ProjectNodeType.File);
                        File.WriteAllText(file, "");
                        projtree.SelectedNode.Nodes.Add(node);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an Error : " + ex.Message, "Project Manager", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        ///     Checks whether a Path is a Directory or File
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static bool IsDir(string input)
        {
            return (File.GetAttributes(input) & FileAttributes.Directory)
                   == FileAttributes.Directory;
        }

        /// <summary>
        ///     Refreshes the ProjectList
        /// </summary>
        private void RefreshProjects()
        {
            projtree.Nodes.Clear();
            var projects = _openprojects.ToArray();
            foreach (var project in projects)
                OpenProject(project.ProjectFile);
            _openprojects.Clear();
        }

        /// <summary>
        ///     Saves Recent File to List
        /// </summary>
        /// <param name="path"></param>
        /// <param name="recent"></param>
        private void SaveUserProjects()
        {
            //writing menu list to file
            using (var stringToWrite = new StreamWriter(Settings.SettingsDir + "User.projects"))
            {
                foreach (var item in RecentProjects)
                    stringToWrite.WriteLine(item); //write list to stream
                stringToWrite.Flush(); //write stream to file
                stringToWrite.Close(); //close the stream and reclaim memory
            }
        }

        /// <summary>
        ///     Loads the List of Recent files from list
        /// </summary>
        internal static IList<string> GetUserProjects()
        {
            var _mru = new List<string>();
            try
            {
                using (var listToRead = new StreamReader(Settings.SettingsDir + "User.projects"))
                {
                    //read file stream
                    string line;
                    while ((line = listToRead.ReadLine()) != null) //read each line until end of file
                        _mru.Add(line); //insert to list
                    listToRead.Close(); //close the stream
                }
            }
            catch
            {
                ;
            }
            return _mru;
        }

        #endregion

        #region Events

        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // initialize new project-wizard
            using (var wizard = new ProjectWizard(this))
            {
                // showdialog(this);
                if (wizard.ShowDialog() == DialogResult.OK)
                {
                    if (wizard.ResultingProject != null)
                    {
                        OpenProject(wizard.ResultingProject);
                        RecentProjects.Add(wizard.ResultingProject.ProjectFile);
                    }
                }
            }
        }

        private void openProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Ynote Project Files(*.ynoteproj)|*.ynoteproj";
                dlg.ShowDialog();
                if (string.IsNullOrEmpty(dlg.FileName)) return;
                OpenProject(dlg.FileName);
                if (!RecentProjects.Contains(dlg.FileName))
                    RecentProjects.Add(dlg.FileName);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var rootnode = FindRootNode(projtree.SelectedNode);
            _openprojects.Remove((YnoteProject) (rootnode.Tag));
            RefreshProjects();
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var node = e.Node.Name;
            var n = e.Node as ExTreeNode;
            if (n.Type == ProjectNodeType.Folder ||
                n.Type == ProjectNodeType.Project) return;
            _ynote.OpenFile(node);
        }

        private void buildProjectToolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            if (buildProjectToolStripMenuItem.DropDownItems.Count != 0) return;
            foreach (var proj in _openprojects)
                buildProjectToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem(proj.ProjectName, null,
                    build_click)
                {
                    Name = proj.BuildFile
                });
        }

        private void build_click(object sender, EventArgs e)
        {
            var buildfile = ((ToolStripMenuItem) (sender)).Name;
            var console = new Shell("cmd.exe", "/k " + buildfile);
            console.Show(DockPanel, DockState.DockBottom);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshProjects();
        }

        private void openExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(projtree.SelectedNode.Name);
        }

        private void menuItem3_Click(object sender, EventArgs e)
        {
            var node = projtree.SelectedNode as ExTreeNode;
            if (node.Type != ProjectNodeType.Project) return;
            var buildfile = (node.Tag as YnoteProject).BuildFile;
            var console = new Shell("cmd.exe", "/k " + buildfile);
            console.Show(DockPanel, DockState.DockBottom);
        }

        private void menuItem12_Click(object sender, EventArgs e)
        {
            RefreshProjects();
        }

        private void menuItem4_Click(object sender, EventArgs e)
        {
            var proj = projtree.SelectedNode.Tag as YnoteProject;
            var result = MessageBox.Show("Are you sure you want to delete the project along with all it's files ?",
                "Project Manager",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (proj != null && result == DialogResult.Yes)
            {
                Directory.Delete(proj.Folder, true);
                File.Delete(proj.Folder);
                projtree.Nodes.Remove(projtree.SelectedNode);
                RecentProjects.Remove(proj.ProjectFile);
            }
        }

        private void menuItem1_Click(object sender, EventArgs e)
        {
            AddNewFolder();
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            AddNewFile();
        }

        private void menuItem15_Click(object sender, EventArgs e)
        {
            DeleteFile();
        }

        private void menuItem14_Click(object sender, EventArgs e)
        {
            DoRename();
        }

        private void menuItem5_Click(object sender, EventArgs e)
        {
            DoRename();
        }

        private void menuItem10_Click(object sender, EventArgs e)
        {
            if (projtree.SelectedNode == null) return;
            var folder = projtree.SelectedNode.Name;
            Process.Start(folder);
        }

        private void menuItem16_Click(object sender, EventArgs e)
        {
            var selected = projtree.SelectedNode as ExTreeNode;
            if (selected == null) return;
            var path = selected.Name;
            var fileName = Path.ChangeExtension(path, "") + "-Copy" + Path.GetExtension(path);
            File.Copy(path, fileName);
            var parent = selected.Parent as ExTreeNode;
            if (parent != null && parent.Type == ProjectNodeType.Folder)
                parent.Nodes.Add(new ExTreeNode(Path.GetFileName(fileName), fileName, 1, 1, null, ProjectNodeType.File));
        }

        private void menuItem13_Click(object sender, EventArgs e)
        {
            _ynote.OpenFile(projtree.SelectedNode.Name);
        }

        private void menuItem7_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "All Files (*.*)|*.*";
                    var res = dlg.ShowDialog() == DialogResult.OK;
                    if (!res) return;
                    var selectedNode = projtree.SelectedNode as ExTreeNode;
                    var dir = selectedNode.Name;
                    var newfile = Path.Combine(dir, Path.GetFileName(dlg.FileName));
                    File.Copy(dlg.FileName, newfile);
                    selectedNode.Nodes.Add(new ExTreeNode(Path.GetFileName(dlg.FileName), newfile, 1, 1, null,
                        ProjectNodeType.File));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message, "Project Manager", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        private void menuItem8_Click(object sender, EventArgs e)
        {
            using (var browser = new FolderBrowserDialog())
            {
                browser.ShowDialog();
                var node = projtree.SelectedNode as ExTreeNode;
                if (node.Type == ProjectNodeType.Folder && browser.SelectedPath != null)
                {
                    CopyDirectory(browser.SelectedPath,
                        node.Name + "\\" + Path.GetFileName(Path.GetDirectoryName(browser.SelectedPath)));
                    ListDirectory(node, browser.SelectedPath);
                    // var files = Directory.GetFiles(browser.SelectedPath);
                    // var folderNode = new ExTreeNode(Path.GetFileName(browser.SelectedPath),
                    //     browser.SelectedPath, 0, 0, null, ProjectNodeType.Folder);
                    // foreach (var file in files)
                    //     folderNode.Nodes.Add(new ExTreeNode(Path.GetFileName(file),
                    //         file, 1, 1, null, ProjectNodeType.File));
                    // node.Nodes.Add(folderNode);
                }
            }
        }

        private void menuItem11_Click(object sender, EventArgs e)
        {
            DeleteFile();
        }

        private void menuItem17_Click(object sender, EventArgs e)
        {
            var node = projtree.SelectedNode as ExTreeNode;
            if (node == null) return;
            var result = MessageBox.Show("This will Remove the Project from the Tree, Continue ?", null,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result == DialogResult.Yes
                && node.Type == ProjectNodeType.Project)
            {
                projtree.Nodes.Remove(node);
                RecentProjects.Remove(((YnoteProject) (node.Tag)).ProjectFile);
            }
        }

        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Select the clicked node
                projtree.SelectedNode = projtree.GetNodeAt(e.X, e.Y);

                if (projtree.SelectedNode != null)
                {
                    var node = projtree.SelectedNode as ExTreeNode;
                    if (node.Type == ProjectNodeType.File)
                        fileMenu.Show(projtree, e.Location);
                    else if (node.Type == ProjectNodeType.Folder)
                        folderMenu.Show(projtree, e.Location);
                    else if (node.Type == ProjectNodeType.Project)
                        projMenu.Show(projtree, e.Location);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            foreach (var item in RecentProjects)
                BeginInvoke((MethodInvoker) (() => OpenProject(item)));
            ;
            base.OnLoad(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveUserProjects();
            base.OnClosed(e);
        }

        #endregion
    }
}