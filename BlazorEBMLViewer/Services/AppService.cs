using SpawnDev.BlazorJS;
using SpawnDev.EBML;
using SpawnDev.EBML.Elements;

namespace BlazorEBMLViewer.Services
{
    public class AppService : IAsyncBackgroundService
    {

        public Task Ready => _Ready ??= InitAsync();
        private Task? _Ready = null;
        BlazorJSRuntime JS;
        public AppService(BlazorJSRuntime js)
        {
            JS = js;
        }
        async Task InitAsync()
        {

        }
        public string GetElementTypeIcon(SchemaElement? elementType)
        {
            return GetElementTypeIcon(elementType?.Type);
        }
        public string GetElementTypeIcon(string? elementType)
        {
            return elementType switch
            {
                MasterElement.TypeName => "folder",
                UintElement.TypeName => "tag",
                IntElement.TypeName => "tag",
                FloatElement.TypeName => "tag",
                StringElement.TypeName => "text_snippet",
                UTF8Element.TypeName => "text_snippet",
                BinaryElement.TypeName => "grid_view",
                DateElement.TypeName => "calendar_month",
                _ => "help_outline",
            };
        }
    }
}
