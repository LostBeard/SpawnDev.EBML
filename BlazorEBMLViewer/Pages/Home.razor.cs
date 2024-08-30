using BlazorEBMLViewer.Components;
using BlazorEBMLViewer.Layout;
using BlazorEBMLViewer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Schemas;
using File = SpawnDev.BlazorJS.JSObjects.File;

namespace BlazorEBMLViewer.Pages
{
    public partial class Home : IDisposable
    {
        public bool DocumentBusy { get; set; }
        public SpawnDev.EBML.EBMLDocument? Document { get; set; }
        public string? ActiveContainerTypeName => ActiveContainer?.GetType().Name;
        public MasterElement? ActiveContainer { get; set; }
        public string Path => ActiveContainer?.InstancePath ?? "";
        bool CanGoUp => !(ActiveContainer?.DocumentRoot ?? true);
        [Inject]
        BlazorJSRuntime JS { get; set; }
        [Inject]
        MainLayoutService MainLayoutService { get; set; }
        [Inject]
        EBMLSchemaService EBMLSchemaService { get; set; }
        [Inject]
        ContextMenuService ContextMenuService { get; set; }
        [Inject]
        DialogService DialogService { get; set; }

        EBMLDataGrid Grid { get; set; }

        [Inject]
        AppService AppService { get; set; }

        async Task RowSelect(ElementBase element)
        {
            StateHasChanged();
        }
        async Task RowDeselect(ElementBase element)
        {
            StateHasChanged();
        }
        async Task NewDocument(Schema schema)
        {
            var filename = "NewDocument.ebml";
            CloseDocument();
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            Document = EBMLSchemaService.Parser.CreateDocument(schema.DocType, filename);
            //Document.OnElementAdded += Document_OnElementAdded;
            //Document.OnElementRemoved += Document_OnElementRemoved;
            Document.OnChanged += Document_OnChanged;
            MainLayoutService.Title = Document.Filename;
            ActiveContainer = Document;
            DocumentBusy = false;
            StateHasChanged();
            await SetPath(Document);
        }
        private void Document_OnChanged()
        {
            //var element = elements.First();
            //Console.WriteLine($"VIEW: Document_OnChanged: {elements.Count()} {element.Depth} {element.Name} {element.Path}");
        }
        //private void Document_OnElementRemoved(MasterElement masterElement, EBMLElement element)
        //{
        //    //Console.WriteLine($"VIEW: Document_OnElementRemoved: {element.Depth} {element.Name} {element.Path}");
        //}
        //private void Document_OnElementAdded(MasterElement masterElement, EBMLElement element)
        //{
        //    //Console.WriteLine($"VIEW: Document_OnElementAdded: {element.Depth} {element.Name} {element.Path}");
        //}
        protected override void OnInitialized()
        {
            //
        }
        public void Dispose()
        {
            //
        }
        List<string> History = new List<string>();
        async Task SetPath(string path)
        {
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            if (string.IsNullOrEmpty(path) || path.Trim(EBMLParser.PathDelimiters) == "")
            {
                await SetPath(Document);
            }
            else
            {
                var element = Document?.FindMaster(path);
                if (element != null)
                {
                    await SetPath(element);
                }
            }
            DocumentBusy = false;
            StateHasChanged();
        }
        bool CanUndo => Document?.CanUndo ?? false;
        bool CanRedo => Document?.CanRedo ?? false;
        async Task Undo()
        {
            Document?.Undo();
        }
        async Task Redo()
        {
            Document?.Redo();
        }
        async Task SetPath(ElementBase element)
        {
            if (element == null) return;
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            if (element is MasterElement source)
            {
                ActiveContainer = source;
            }
            DocumentBusy = false;
            StateHasChanged();
        }
        async Task GoUp()
        {
            if (ActiveContainer?.Parent != null)
            {
                await SetPath(ActiveContainer.Parent);
            }
        }
        IEnumerable<ContextMenuItem> GetAddElementOptions()
        {
            var ret = new List<ContextMenuItem>();
            if (ActiveContainer == null) return ret;
            var addables = ActiveContainer.GetAddableElementSchemas(true);
            var missing = new List<SchemaElement>();
            foreach (var addable in addables)
            {
                var requiresAdd = false;
                var atMaxCount = false;
                if (addable.MaxOccurs > 0 || addable.MinOccurs > 0)
                {
                    var count = ActiveContainer.Children.Count(o => o.Id == addable.Id);
                    requiresAdd = addable.MinOccurs > 0 && count < addable.MinOccurs;
                    atMaxCount = addable.MaxOccurs > 0 && count >= addable.MaxOccurs;
                    if (requiresAdd) missing.Add(addable);
                }
                var option = new ContextMenuItem
                {
                    Text = addable.Name + (requiresAdd ? " (required)" : ""),
                    Value = addable,
                    Disabled = atMaxCount,
                    Icon = AppService.GetElementTypeIcon(addable),
                };
                if (requiresAdd) option.IconColor = Colors.Danger;
                ret.Add(option);
            }
            if (missing.Count > 0)
            {
                ret.Insert(0, new ContextMenuItem
                {
                    Text = $"{missing.Count} required",
                    Value = missing,
                    Icon = "add",
                    IconColor = Colors.Danger,
                });
            }
            return ret;
        }
        void AddElementClicked(MenuItemEventArgs args)
        {
            if (ActiveContainer == null || Document == null) return;
            ContextMenuService.Close();
            if (args.Value is List<SchemaElement> missing)
            {
                Document.DisableDocumentEngines();
                foreach (var addable in missing)
                {
                    ActiveContainer.AddElement(addable);
                }
                Document.EnableDocumentEngines();
            }
            else if (args.Value is SchemaElement addable)
            {
                ActiveContainer.AddElement(addable);
            }
        }
        void AddElement(MenuItemEventArgs args)
        {
            ContextMenuService.Open(args, GetAddElementOptions(), AddElementClicked);
        }
        void CloseDocument()
        {
            if (Document == null) return;
            //Document.OnElementAdded -= Document_OnElementAdded;
            //Document.OnElementRemoved -= Document_OnElementRemoved;
            Document.OnChanged -= Document_OnChanged;
            Document = null;
            ActiveContainer = null;
            History.Clear();
            MainLayoutService.Title = "";
            if (DocumentSourceHandle != null)
            {
                DocumentSourceHandle.Dispose();
                DocumentSourceHandle = null;
            }
            StateHasChanged();
        }
        async Task ShowOpenFileDialogFallback()
        {
            File[] result;
            try
            {
                result = await FilePicker.ShowOpenFilePicker(".ebml,.webm,.mkv,.mka,.mks");
            }
            catch
            {
                return;
            }
            var file = result?.FirstOrDefault();
            if (file == null) return;
            CloseDocument();
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            var arrayBuffer = await file.ArrayBuffer();
            var fileStream = new ArrayBufferStream(arrayBuffer);
            Document = EBMLSchemaService.Parser.ParseDocument(fileStream, file.Name);
            MainLayoutService.Title = file.Name;
            ActiveContainer = Document;
            DocumentBusy = false;
            StateHasChanged();
            await SetPath(Document);
        }
        async Task GridRowContextMenu(RowContextMenuArgs args)
        {
            var element = args.Element;
            var options = new List<ContextMenuItem>();
            options.Add(new ContextMenuItem
            {
                Text = "Delete",
                Icon = "delete",
                Value = async () =>
                {
                    var confirm = await DialogService.Confirm($"Delete {element.Name}?");
                    if (confirm == true)
                    {
                        element.Remove();
                    }
                }
            });
            ContextMenuService.Open(args.MouseEventArgs, options, ContextMenuActionInvoker);
        }
        void ContextMenuActionInvoker(MenuItemEventArgs args)
        {
            if (args.Value is Action action) action();
            else if (args.Value is Func<Task> asyncAction) _ = asyncAction();
        }
        async Task ShowOpenFileDialog()
        {
            if (true || JS.IsUndefined("window.showOpenFilePicker"))
            {
                await ShowOpenFileDialogFallback();
                return;
            }
            DocumentBusy = true;
            StateHasChanged();
            FileSystemFileHandle? file = null;
            Array<FileSystemFileHandle> result;
            try
            {
                result = await JS.WindowThis!.ShowOpenFilePicker(new ShowOpenFilePickerOptions
                {
                    Multiple = false,
                    Types = new List<ShowOpenFilePickerType>
                {
             new ShowOpenFilePickerType{ Accept = new Dictionary<string, List<string>>
             {
                 { "application/octet-stream", new List<string>{ ".ebml", ".mks" } },
                 { "video/x-matroska", new List<string>{ ".mkv", ".webm" } },
                 { "audio/x-matroska", new List<string>{ ".mka" } },
             }, Description = "EBML Files" }
            }
                });
            }
            catch
            {
                return;
            }
            try
            {
                file = result.FirstOrDefault();
                result.Dispose();
                if (file == null) return;
                CloseDocument();
                DocumentSourceHandle = file;
                using var f = await file.GetFile();
                using var arrayBuffer = await f.ArrayBuffer();
                var fileStream = new ArrayBufferStream(arrayBuffer);
                Document = EBMLSchemaService.Parser.ParseDocument(fileStream, file.Name);
                MainLayoutService.Title = file.Name;
                ActiveContainer = Document;
                DocumentBusy = false;
                StateHasChanged();
                await SetPath(Document);
            }
            finally
            {
                DocumentBusy = false;
                StateHasChanged();
            }
        }
        async Task DownloadDocument()
        {
            if (Document == null) return;
            DocumentBusy = true;
            StateHasChanged();
            try
            {
                await Task.Delay(50);
                var mem = new MemoryStream();
                await Document.CopyToAsync(mem, CancellationToken.None);
                mem.Position = 0;
                var length = mem.Length;
                using var tmp = new Blob(new byte[][] { mem.ToArray() });
                await tmp.StartDownload(Document.Filename);
            }
            finally
            {
                DocumentBusy = false;
                StateHasChanged();
            }
        }
        FileSystemFileHandle? DocumentSourceHandle = null;
        async Task ShowSaveFileDialog()
        {
            if (Document == null) return;
            if (JS.IsUndefined("window.showSaveFilePicker"))
            {
                await DownloadDocument();
                return;
            }
            DocumentBusy = true;
            StateHasChanged();
            try
            {
                try
                {
                    var result = await JS.WindowThis!.ShowSaveFilePicker(new ShowSaveFilePickerOptions { SuggestedName = Document.Filename });
                    // TODO - create stream wrapper for FileSystemFileHandle
                    // then we could copy directly to it instead of usign a memory stream
                    //var writable = await result.CreateWritable();
                    //var writer = writable.GetWriter();
                    //await writer.Write(chunk);
                    if (result != null)
                    {
                        var mem = new MemoryStream();
                        Document.CopyTo(mem);
                        mem.Position = 0;
                        await result.Write(mem.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    var nmttt = true;
                }
            }
            finally
            {
                DocumentBusy = false;
            }
        }
    }
}
