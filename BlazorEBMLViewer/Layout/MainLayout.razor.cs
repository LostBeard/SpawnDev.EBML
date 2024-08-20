﻿using Microsoft.AspNetCore.Components;
using Radzen;
using SpawnDev.BlazorJS;
using BlazorEBMLViewer.Components.AppTray;
using BlazorEBMLViewer.Services;

namespace BlazorEBMLViewer.Layout
{
    public partial class MainLayout
    {
        [Inject]
        BlazorJSRuntime JS { get; set; }
        [Inject]
        NotificationService NotificationService { get; set; }
        [Inject]
        DialogService DialogService { get; set; }
        [Inject]
        AppTrayService TrayIconService { get; set; }
        [Inject]
        protected NavigationManager NavigationManager { get; set; }
        [Inject]
        MainLayoutService MainLayoutService { get; set; }
        [Inject]
        ThemeService ThemeService { get; set; }
        [Inject]
        EBMLSchemaService EBMLSchemaService { get; set; }

        string Title => MainLayoutService.Title;
        bool leftSidebarExpanded = false;
        bool rightSidebarExpanded = false;
        public Type? PageType { get; private set; }
        public string PageTypeName => PageType?.Name ?? "";
        public string Location { get; private set; } = "";
        public string? HistoryEntryState { get; private set; }
        public DateTime LocationUpdated { get; private set; } = DateTime.MinValue;
        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            MainLayoutService.OnTitleChanged += MainLayoutService_OnTitleChanged;
        }
        private void MainLayoutService_OnTitleChanged()
        {
            StateHasChanged();
        }
        private void NavigationManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            AfterLocationChanged(e.HistoryEntryState);
        }
        protected override void OnAfterRender(bool firstRender)
        {
            MainLayoutService.TriggerOnAfterRender(this, firstRender);
            if (firstRender)
            {
                AfterLocationChanged();
            }
        }
        void AfterLocationChanged(string? historyEntryState = null)
        {
            var pageType = Body != null && Body.Target != null && Body.Target is RouteView routeView ? routeView.RouteData.PageType : null;
            var location = NavigationManager.Uri;
            if (PageType == pageType && Location == location)
            {
                Console.WriteLine($"SendLocationChanged: false");
                return;
            }
            LocationUpdated = DateTime.Now;
            PageType = pageType;
            Location = location;
            HistoryEntryState = historyEntryState;
            Console.WriteLine($"LocationChanged: {PageTypeName} [{HistoryEntryState ?? ""}] {Location}");
        }
    }
}
