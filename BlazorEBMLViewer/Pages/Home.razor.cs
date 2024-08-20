using BlazorEBMLViewer.Components;
using BlazorEBMLViewer.Layout;
using BlazorEBMLViewer.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.EBML;
using SpawnDev.EBML.Elements;

namespace BlazorEBMLViewer.Pages
{
    public partial class Home : IDisposable
    {
        public bool DocumentBusy { get; set; }
        public EBMLDocument? Document { get; set; }
        public MasterElement? ActiveContainer { get; set; }
        public string Path => ActiveContainer?.Path ?? "";
        bool CanGoUp => Path.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 0;
        [Inject]
        BlazorJSRuntime JS { get; set; }
        [Inject]
        MainLayoutService MainLayoutService { get; set; }
        [Inject]
        EBMLSchemaService EBMLSchemaService { get; set; }
        [Inject]
        ContextMenuService ContextMenuService { get; set; }

        EBMLDataGrid Grid { get; set; }

        [Inject]
        AppService AppService { get; set; }

        async Task RowSelect(BaseElement element)
        {
            StateHasChanged();
        }
        async Task RowDeselect(BaseElement element)
        {
            StateHasChanged();
        }
        async Task NewDocument(EBMLSchema schema)
        {
            var filename = "NewDocument.ebml";
            CloseDocument();
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            Document = new EBMLDocument(filename, schema.DocType, EBMLSchemaService.SchemaSet);
            Document.OnChanged += Document_OnChanged;
            MainLayoutService.Title = Document.Filename;
            ActiveContainer = Document;
            DocumentBusy = false;
            StateHasChanged();
            await SetPath(@"\\");
        }

        private void Document_OnChanged(BaseElement obj)
        {
            JS.Log("Document_OnChanged", obj.Depth, obj.Path);
        }

        protected override void OnInitialized()
        {
            //
        }
        public void Dispose()
        {
            //
        }
        private void UndoService_OnStateHasChanged()
        {
            StateHasChanged();
        }
        async Task _SetPath(string path)
        {
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            var element = Document?.GetContainer(path);
            if (element is MasterElement source)
            {
                ActiveContainer = source;
            }
            DocumentBusy = false;
            StateHasChanged();
        }
        List<string> History = new List<string>();
        async Task SetPath(string path)
        {
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            var element = Document?.GetContainer(path);
            if (element is MasterElement source)
            {
                ActiveContainer = source;
            }
            DocumentBusy = false;
            StateHasChanged();
        }
        async Task SetPath(BaseElement element)
        {
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
            var parts = Path.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return;
            var newPath = "\\" + string.Join('\\', parts.Take(parts.Length - 1));
            await SetPath(newPath);
        }
        IEnumerable<ContextMenuItem> GetAddElementOptions()
        {
            var ret = new List<ContextMenuItem>();
            if (ActiveContainer == null) return ret;
            var addables = ActiveContainer.GetAddableElementSchemas(true);
            var missing = new List<EBMLSchemaElement>();
            foreach (var addable in addables)
            {
                var requiresAdd = false;
                var atMaxCount = false;
                if (addable.MaxOccurs > 0 || addable.MinOccurs > 0)
                {
                    var count = ActiveContainer.Data.Count(o => o.Id == addable.Id);
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
            if (ActiveContainer == null) return;
            ContextMenuService.Close();
            if (args.Value is List<EBMLSchemaElement> missing)
            {
                foreach (var addable in missing)
                {
                    ActiveContainer.AddElement(addable);
                }
            }
            else if (args.Value is EBMLSchemaElement addable)
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
            Document = null;
            ActiveContainer = null;
            History.Clear();
            MainLayoutService.Title = "";
            if (DocumentSourceHandle != null)
            {
                DocumentSourceHandle.Dispose();
                DocumentSourceHandle = null;
            }
            //UndoService.Clear();
            StateHasChanged();
        }
        async Task ShowOpenFileDialogFallback()
        {
            var result = await FilePicker.ShowOpenFilePicker(".ebml,.webm,.mkv,.mka,.mks");
            var file = result?.FirstOrDefault();
            if (file == null) return;
            CloseDocument();
            DocumentBusy = true;
            StateHasChanged();
            await Task.Delay(50);
            var arrayBuffer = await file.ArrayBuffer();
            var fileStream = new ArrayBufferStream(arrayBuffer);
            Document = new EBMLDocument(file.Name, fileStream, EBMLSchemaService.SchemaSet);
            MainLayoutService.Title = file.Name;
            ActiveContainer = Document;
            DocumentBusy = false;
            StateHasChanged();
            await SetPath(@"\\");
        }
        async Task ShowOpenFileDialog()
        {
            if (JS.IsUndefined("window.showOpenFilePicker"))
            {
                await ShowOpenFileDialogFallback();
                return;
            }
            DocumentBusy = true;
            StateHasChanged();
            FileSystemFileHandle? file = null;
            try
            {
                using var result = await JS.WindowThis!.ShowOpenFilePicker(new ShowOpenFilePickerOptions
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
                file = result.FirstOrDefault();
                if (file == null) return;
                CloseDocument();
                DocumentSourceHandle = file;
                using var f = await file.GetFile();
                using var arrayBuffer = await f.ArrayBuffer();
                var fileStream = new ArrayBufferStream(arrayBuffer);
                Document = new EBMLDocument(file.Name, fileStream, EBMLSchemaService.SchemaSet);
                MainLayoutService.Title = file.Name;
                ActiveContainer = Document;
                DocumentBusy = false;
                StateHasChanged();
                await SetPath(@"\\");
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
                Document.CopyTo(mem);
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
