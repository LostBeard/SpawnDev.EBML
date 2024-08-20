using BlazorEBMLViewer.Components.AppTray;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace BlazorEBMLViewer.Services
{
    public class FullscreenTrayIconService : IBackgroundService
    {
        BlazorJSRuntime JS;
        Document? Document;
        Window? Window;
        AppTrayService TrayIconService;
        AppTrayIcon FullscreenTrayIcon;
        public FullscreenTrayIconService(BlazorJSRuntime js, AppTrayService trayIconService, DialogService dialogService)
        {
            JS = js;
            TrayIconService = trayIconService;
            if (JS.IsWindow)
            {
                Window = JS.Get<Window>("window");
                Document = JS.Get<Document>("document");
                Document!.OnFullscreenChange += Document_OnFullscreenChange;
                Window!.OnResize += Window_OnResized;
                // fullscreen indicator and toggle icon
                FullscreenTrayIcon = new AppTrayIcon
                {
                    ClickCallback = FullscreenTrayIcon_ClickCallback,
                };
                TrayIconService.Add(FullscreenTrayIcon);
                UpdateTrayIcon();
            }
        }
        void UpdateTrayIcon()
        {
            var isFullscreen = IsFullscreen;
            if (isFullscreen)
            {
                FullscreenTrayIcon.Title = "Exit Fullscreen";
                FullscreenTrayIcon.Icon = "fullscreen_exit";
            }
            else
            {
                FullscreenTrayIcon.Title = "Enter Fullscreen";
                FullscreenTrayIcon.Icon = "fullscreen";
            }
            TrayIconService.StateHasChanged();
        }
        void FullscreenTrayIcon_ClickCallback(MouseEventArgs mouseEventArgs)
        {
            _ = ToggleFullscreen();
        }
        public event Action OnFullscreenStateChanged;
        void Window_OnResized()
        {
            // Need this?
        }
        void Document_OnFullscreenChange()
        {
            UpdateTrayIcon();
        }
        public async Task EnterFullscreen()
        {
            if (Document == null) return;
            try
            {
                await JS.CallVoidAsync("document.body.requestFullscreen");
            }
            catch { }
        }
        public async Task ExitFullscreen()
        {
            if (Document == null) return;
            try
            {
                await Document.ExitFullscreen();
            }
            catch { }
        }
        public Element? GetFullscreenElement() => Document?.FullscreenElement;
        public async Task ToggleFullscreen()
        {
            if (IsFullscreen)
            {
                await ExitFullscreen();
            }
            else
            {
                await EnterFullscreen();
            }
        }
        public bool IsFullscreen
        {
            get
            {
                using var element = GetFullscreenElement();
                return element != null;
            }
        }
    }
}
