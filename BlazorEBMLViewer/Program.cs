using BlazorEBMLViewer;
using BlazorEBMLViewer.Components.AppTray;
using BlazorEBMLViewer.Layout;
using BlazorEBMLViewer.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using SpawnDev.BlazorJS;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// SpawnDev.BlazorJS Javascript interop
builder.Services.AddBlazorJSRuntime(out var JS);
// Radzen components
builder.Services.AddRadzenComponents();
// MainLayoutService
builder.Services.AddScoped<MainLayoutService>();
// AppTray service
builder.Services.AddScoped<AppTrayService>();
// AppTray icon services
// Theme switcher tray icon
// - Icon click switches theme dark/light mode
// - Icon shift+click switches to next theme
// - Icon ctrl+click switches previous theme
// - Icon right click shows theme select context menu
builder.Services.AddScoped<ThemeTrayIconService>();
// Battery tray icon service (simple battery state indicator)
// - Does not show if no state is found
//builder.Services.AddScoped<BatteryTrayIconService>();
// EBML Schema service
builder.Services.AddScoped<EBMLSchemaService>();
// Fullscreen switcher tray icon
// - Icon click to toggle fullscreen mode
builder.Services.AddScoped<FullscreenTrayIconService>();
// App primary service
builder.Services.AddScoped<AppService>();
// If in a window scope, add Blazor document elements
if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}
// Start Blazor (BlazorJSRunAsync is scope aware and supports auto-starting services)
await builder.Build().BlazorJSRunAsync();