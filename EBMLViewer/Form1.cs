using EBMLViewer.Controls;
using SpawnDev.EBML;
using SpawnDev.EBML.Matroska;
using SpawnDev.EBML.WebM;

namespace EBMLViewer
{
    public partial class Form1 : Form
    {

        WebMDocumentReader? Parser = null;
        FileStream? fileStream = null;
        string SourceFile { get; set; } = "";
        string DefaultTitle = "EBML Viewer";

        public Form1()
        {
            InitializeComponent();
            Text = DefaultTitle;
            var deleteOption = new ToolStripMenuItem { Text = "Delete", Name = "delete", };
            deleteOption.Click += DeleteOption_Click;
            baseElementContextMenu.Items.Add(deleteOption);
        }

        private void DeleteOption_Click(object? sender, EventArgs e)
        {
            if (Parser == null) return;
            var node = treeView1.SelectedNode;
            if (node == null) return;
            var element = (BaseElement?)node.Tag;
            if (element == null) return;
            var trackEntryElement = element as TrackEntryElement;
            var trackId = trackEntryElement?.TrackNumber ?? 0;
            element.Remove();
            var parentNode = node.Parent;
            if (parentNode == null) return;
            //
            if (trackEntryElement != null)
            {
                var resp = MessageBox.Show($"Delete all SimpleBlocks that belong to this track and update the other track ids if needed?", "Remove track blocks?", MessageBoxButtons.YesNo);
                if (resp == DialogResult.Yes)
                {
                    // remove SimpleBlocks for the specified track and update trackId of others if needed.
                    var simpleBlocks = Parser.GetElements<SimpleBlockElement>(MatroskaId.Segment, MatroskaId.Cluster, MatroskaId.SimpleBlock);
                    foreach (var simpleBlock1 in simpleBlocks)
                    {
                        if (simpleBlock1.TrackId > trackId)
                        {
                            simpleBlock1.TrackId -= 1;
                        }
                        else if (simpleBlock1.TrackId == trackId)
                        {
                            simpleBlock1.Remove();
                        }
                    }
                    // remove Blocks for the specified track and update trackId of others if needed.
                    var blocks = Parser.GetElements<BlockElement>(MatroskaId.Segment, MatroskaId.Cluster, MatroskaId.BlockGroup, MatroskaId.Block);
                    foreach (var block in blocks)
                    {
                        if (block.TrackId > trackId)
                        {
                            block.TrackId -= 1;
                        }
                        else if (block.TrackId == trackId)
                        {
                            block.Parent!.Remove();
                        }
                    }
                    // update trackId of other tracks if needed.
                    var tracks = Parser.Tracks;
                    foreach (var track in tracks)
                    {
                        if (track.TrackNumber > trackId)
                        {
                            track.TrackNumber -= 1;
                        }
                    }
                    // Update SeekHead and Cues
                    // ATM just removing them as the resulting media file will play without them. 
                    var seekHead = Parser.GetContainer(MatroskaId.Segment, MatroskaId.SeekHead);
                    if (seekHead != null) seekHead.Remove();
                    var cues = Parser.GetContainer(MatroskaId.Segment, MatroskaId.Cues);
                    if (cues != null) cues.Remove();
                }
                PopulateNode(treeView1.Nodes[0], forceRefrash: true);
            }
            else
            {
                PopulateNode(parentNode, forceRefrash: true);
            }
        }

        bool eventAttached = false;

        void CloseSource()
        {
            if (string.IsNullOrEmpty(SourceFile))
            {
                return;
            }
            UnloadNodeView();
            fileStream?.Dispose();
            fileStream = null;
            SourceFile = "";
            Text = DefaultTitle;
            closeToolStripMenuItem.Enabled = false;
            if (Parser != null && eventAttached)
            {
                eventAttached = false;
                Parser.OnDataChanged -= Parser_OnDataChanged;
                if (Parser is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            treeView1.Nodes.Clear();
            addDurationToolStripMenuItem.Enabled = false;
            webMOptionsToolStripMenuItem.Visible = false;
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            treeView1.BeforeExpand += TreeView1_BeforeExpand;
            treeView1.AfterSelect += TreeView1_AfterSelect;
        }

        UserControl? nodeView = null;

        void UnloadNodeView()
        {
            if (nodeView == null) return;
            splitContainer1.Panel2.Controls.Remove(nodeView);
            nodeView.Dispose();
            nodeView = null;
        }
        void LoadNodeView(BaseElement? element)
        {
            if (element == null) return;
            if (nodeView != null) UnloadNodeView();
            var elementType = element.GetType();
            var elementViewType = EBMLFormsControls.ElementToControlTypeMap.TryGetValue(elementType, out var et) ? et : typeof(BaseElementView);
            nodeView = (UserControl)Activator.CreateInstance(elementViewType)!;
            var elementControl = (IElementControl)nodeView;
            elementControl.LoadElement(element);
            nodeView.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(nodeView);
        }

        private void TreeView1_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            UnloadNodeView();
            LoadNodeView((BaseElement?)e.Node?.Tag);
        }

        private void TreeView1_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            var parentNode = e.Node;
            if (parentNode != null)
            {
                PopulateNode(parentNode);
            }
        }

        string ShowSaveAsDialog()
        {
            if (string.IsNullOrEmpty(SourceFile)) return "";
            var dir = Path.GetDirectoryName(SourceFile);
            var ext = Path.GetExtension(SourceFile);
            var filename = Path.GetFileName(SourceFile);
            ext = string.IsNullOrEmpty(ext) || !ext.StartsWith(".") ? "" : ext.Substring(1);
            var saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.InitialDirectory = dir;
            saveFileDialog1.FileName = filename;
            saveFileDialog1.Title = "Save as";
            saveFileDialog1.CheckFileExists = false;
            saveFileDialog1.CheckPathExists = true;
            saveFileDialog1.DefaultExt = ext;
            var currentExtFilter = string.IsNullOrEmpty(ext) ? "" : $"{saveFileDialog1.DefaultExt} files (*.{saveFileDialog1.DefaultExt})|*.{saveFileDialog1.DefaultExt}|";
            saveFileDialog1.Filter = $"{currentExtFilter}All files (*.*)|*.*";
            saveFileDialog1.RestoreDirectory = true;
            return saveFileDialog1.ShowDialog() == DialogResult.OK ? saveFileDialog1.FileName : "";
        }

        void SaveAsChanges()
        {
            var destFile = ShowSaveAsDialog();
            SaveChanges(destFile, false);
        }

        void SaveChanges()
        {
            SaveChanges(SourceFile);
        }

        void SaveChanges(string destFile, bool confirmOverwrite = true)
        {
            if (Parser == null || string.IsNullOrEmpty(SourceFile) || string.IsNullOrEmpty(destFile)) return;
            var sourceFile = SourceFile;
            var destFileOrig = destFile;
            var filename = Path.GetFileName(destFile);
            var dir = Path.GetDirectoryName(destFile);
            var exists = File.Exists(destFile);
            if (confirmOverwrite && exists)
            {
                var resp = MessageBox.Show($"Are you sure you want to overwrite {filename}?", "Warning: File will be overwritten.", MessageBoxButtons.OKCancel);
                if (resp != DialogResult.OK)
                {
                    return;
                }
            }

            try
            {
                var destIsSource = destFile.Equals(sourceFile, StringComparison.OrdinalIgnoreCase);
                if (destIsSource) destFile = Path.Combine(dir, Guid.NewGuid().ToString());
                else if (exists)
                {
                    File.Delete(destFile);
                }
                using (var fixedStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fixedStream.SetLength(Parser.Length);
                    Parser.CopyTo(fixedStream);
                }
                // close source and overwrite original
                CloseSource();
                if (destIsSource)
                {
                    var origTemp = Path.Combine(dir, Guid.NewGuid().ToString());
                    File.Move(sourceFile, origTemp);
                    var succ = false;
                    var retries = 5;
                    while (retries > 0)
                    {
                        try
                        {
                            File.Move(destFile, sourceFile);
                            succ = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            retries -= 1;
                            Thread.Sleep(250);
                        }
                    }
                    if (!succ)
                    {
                        File.Move(origTemp, sourceFile);
                        MessageBox.Show($"Save file failed.", "Save Failed", MessageBoxButtons.OK);
                    }
                    else
                    {
                        File.Delete(origTemp);
                    }
                }
                LoadFile(destFileOrig);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to save file");
            }
        }

        void AddDurationElement()
        {
            if (Parser != null && Parser.FixDuration())
            {
                var resp = MessageBox.Show("Duration element was added. Would you like to save your changes?", "Success", MessageBoxButtons.YesNo);
                if (resp == DialogResult.Yes)
                {
                    SaveChanges();
                }
            }
            else
            {
                MessageBox.Show("Failed to add duration element.", "Failed");
            }
        }

        void LoadFile(string sourceFile)
        {
            CloseSource();
            if (string.IsNullOrEmpty(sourceFile)) return;
            if (!File.Exists(sourceFile)) return;
            SourceFile = sourceFile;
            var sourceFilename = Path.GetFileName(SourceFile);
            closeToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            try
            {
                fileStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                Parser = new WebMDocumentReader(fileStream);
                Parser.OnDataChanged += Parser_OnDataChanged;
                eventAttached = true;
                addDurationToolStripMenuItem.Enabled = Parser != null && Parser.DocType == "webm" && Parser.Duration == null;
                webMOptionsToolStripMenuItem.Visible = Parser != null && Parser.DocType == "webm";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loading EBML file failed");
                CloseSource();
                return;
            }
            if (Parser == null)
            {
                CloseSource();
                return;
            }
            var docType = string.IsNullOrEmpty(Parser.DocType) ? "?" : Parser.DocType;
            Text = $"{DefaultTitle} - {docType} - {sourceFilename}";
            var sourceNode = new TreeNode($"{docType} - {sourceFilename}");
            sourceNode.Tag = Parser;
            sourceNode.Nodes.Add(loadingReadyText);
            treeView1.Nodes.Add(sourceNode);
            PopulateNode(sourceNode, true);
        }

        private void Parser_OnDataChanged(BaseElement obj)
        {
            saveToolStripMenuItem.Enabled = true;
            addDurationToolStripMenuItem.Enabled = Parser != null && Parser.Duration == null;
        }

        void PopulateNode(TreeNode parentNode, bool expandAll = false, bool forceRefrash = false)
        {
            if (!forceRefrash)
            {
                var isLoadingReady = parentNode.Nodes.Count == 1 && (parentNode.Nodes[0].Text == loadingReadyText);
                if (!isLoadingReady)
                {
                    return;
                }
            }
            parentNode.Nodes[0].Text = loadingText;
            TaskRun(() =>
            {
                var nodes = GetNodes(parentNode);
                UIRun(() =>
                {
                    treeView1.BeginUpdate();
                    parentNode.Nodes.Clear();
                    parentNode.Nodes.AddRange(nodes.ToArray());
                    treeView1.EndUpdate();
                    if (expandAll) parentNode.Expand();
                });
            });
        }

        void UIRun(Action action)
        {
            if (InvokeRequired) Invoke(action);
            else action();
        }
        void TaskRun(Action action) => Task.Run(action);

        List<TreeNode> GetNodes(TreeNode parentNode)
        {
            var nodes = new List<TreeNode>();
            try
            {
                if (parentNode.Tag is MasterElement masterEl)
                {
                    var children = masterEl.Data;
                    foreach (var el in children)
                    {
                        var key = el.ToString();
                        var node = new TreeNode();
                        node.Tag = el;
                        node.Text = key;
                        node.Name = key;
                        nodes.Add(node);
                        if (el is MasterElement childMasterEl)
                        {
                            node.Nodes.Add(loadingReadyText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return nodes;
        }

        string loadingText = "Loading...";
        string loadingReadyText = "...";

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openFileDialog1 = new OpenFileDialog
            {
                Title = "Open EBML file",
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "EBML sources (*.webm;*.mkv)|*.webm;*.mkv|All files (*.*)|*.*",
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadFile(openFileDialog1.FileName);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseSource();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseSource();
            Close();
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void addDurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddDurationElement();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsChanges();
        }

        ContextMenuStrip baseElementContextMenu = new ContextMenuStrip();
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeView1.SelectedNode = e.Node;
                var element = (BaseElement?)e.Node.Tag;
                if (element != null)
                {
                    if (element is TrackEntryElement trackEntryElement)
                    {
                        e.Node.ContextMenuStrip = baseElementContextMenu;
                    }
                    else
                    {
                        e.Node.ContextMenuStrip = baseElementContextMenu;
                    }
                }
            }

        }
    }
}
