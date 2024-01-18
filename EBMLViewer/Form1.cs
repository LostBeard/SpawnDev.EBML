using SpawnDev.EBML;
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
        }

        bool eventAttached = false;

        void CloseSource()
        {
            if (string.IsNullOrEmpty(SourceFile))
            {
                return;
            }
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
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            treeView1.BeforeExpand += TreeView1_BeforeExpand;
        }

        private void TreeView1_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            var parentNode = e.Node;
            if (parentNode != null)
            {
                PopulateNode(parentNode);
            }
        }

        void SaveChanges()
        {
            if (Parser == null || string.IsNullOrEmpty(SourceFile)) return;
            var sourceFile = SourceFile;
            try
            {
                var sourceFilenameBase = Path.GetFileNameWithoutExtension(SourceFile);
                var ext = Path.GetExtension(SourceFile);
                var dir = Path.GetDirectoryName(SourceFile);
                var destFile = Path.Combine(dir, $"{sourceFilenameBase}.fixed_temp{ext}");
                using (var fixedStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fixedStream.SetLength(Parser.Length);
                    Parser.CopyTo(fixedStream);
                }
                // close source and overwrite original
                CloseSource();
                File.Delete(sourceFile);
                File.Move(destFile, sourceFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to save file");
                return;
            }
            // reload source
            LoadFile(sourceFile);
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

        void PopulateNode(TreeNode parentNode, bool expandAll = false)
        {
            var isLoadingReady = parentNode.Nodes.Count == 1 && (parentNode.Nodes[0].Text == loadingReadyText);
            if (!isLoadingReady)
            {
                return;
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
    }
}
