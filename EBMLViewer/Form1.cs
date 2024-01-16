using SpawnDev.EBML;
using SpawnDev.EBML.Matroska;
using SpawnDev.EBML.WebM;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows.Forms;
using System.Xml.Linq;
using static EBMLViewer.Form1;

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
            if (Parser is IDisposable disposable)
            {
                disposable.Dispose();
            }
            treeView1.Nodes.Clear();
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
                var origLength = Parser.Length;
                if (!SourceFile.Contains("fixed", StringComparison.OrdinalIgnoreCase))
                {
                    if (Parser.FixDuration())
                    {
                        var sourceFilenameBase = Path.GetFileNameWithoutExtension(SourceFile);
                        var ext = Path.GetExtension(SourceFile);
                        var dir = Path.GetDirectoryName(SourceFile);
                        var destFile = Path.Combine(dir, $"{sourceFilenameBase}.fixed_abc{ext}");
                        var newLength = Parser.Length;
                        var diff = origLength - newLength;
                        using var fixedStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None);
                        fixedStream.SetLength(Parser.Length);
                        Parser.CopyTo(fixedStream);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loading EBML file failed");
                CloseSource();
                return;
            }
            Text = $"{DefaultTitle} - {Parser.DocType} - {sourceFilename}";
            TreeNode sourceNode = new TreeNode($"{Parser.DocType} - {sourceFilename}");
            sourceNode.Tag = Parser;
            sourceNode.Nodes.Add(loadingReadyText);
            treeView1.Nodes.Add(sourceNode);
            PopulateNode(sourceNode, true);
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
    }
}
