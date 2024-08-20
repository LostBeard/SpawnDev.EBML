using BlazorEBMLViewer.Components.AppTray;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;

namespace BlazorEBMLViewer.Services
{
    public class BatteryTrayIconService : IAsyncBackgroundService
    {
        public Task Ready => _Ready ??= InitAsync();
        private Task? _Ready = null;
        BlazorJSRuntime JS;
        AppTrayService TrayIconService;
        AppTrayIcon BatteryTrayIcon;
        BatteryManager? BatteryManager = null;
        public BatteryTrayIconService(BlazorJSRuntime js, AppTrayService trayIconService, DialogService dialogService)
        {
            JS = js;
            TrayIconService = trayIconService;
            if (JS.IsWindow)
            {
                // battery indicator
                BatteryTrayIcon = new AppTrayIcon
                {
                    Icon = "battery",
                    Visible = false,
                };
                TrayIconService.Add(BatteryTrayIcon);
            }
        }
        async Task InitAsync()
        {
            using var navigator = JS.Get<Navigator>("navigator");
            BatteryManager = await navigator.GetBattery();
            if (BatteryManager != null)
            {
                BatteryTrayIcon.Visible = true;
                BatteryManager.OnChargingChange += BatteryManager_OnChargingChange;
                BatteryManager.OnLevelChange += BatteryManager_OnLevelChange;
                BatteryManager.OnChargingTimeChange += BatteryManager_OnChargingTimeChange;
                BatteryManager.OnDischargingTimeChange += BatteryManager_OnDischargingTimeChange;
                UpdateBatteryIcon();
            }
        }
        void UpdateBatteryIcon()
        {
            if (BatteryManager == null) return;
            try
            {
                var level = BatteryManager.Level;
                var charging = BatteryManager.Charging;
                var chargingTime = BatteryManager.ChargingTime;
                var dischargingTime = BatteryManager.DischargingTime;
                BatteryTrayIcon.Visible = !(charging && level >= 1f);
                BatteryTrayIcon.IconStyle = charging ? IconStyle.Info : IconStyle.Warning;
                if (level >= 0.9f)
                {
                    BatteryTrayIcon.Icon = "battery_full";
                }
                else if (level >= 0.8f)
                {
                    BatteryTrayIcon.Icon = "battery_5_bar";
                }
                else if (level >= 0.6f)
                {
                    BatteryTrayIcon.Icon = "battery_4_bar";
                }
                else if (level >= 0.4f)
                {
                    BatteryTrayIcon.Icon = "battery_3_bar";
                }
                else if (level >= 0.2f)
                {
                    BatteryTrayIcon.Icon = "battery_2_bar";
                }
                else
                {
                    BatteryTrayIcon.Icon = "battery_1_bar";
                }
                TrayIconService.StateHasChanged();
                Console.WriteLine($"Battery: {level} {charging} {chargingTime} {dischargingTime}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Battery error: {ex.Message}");
            }
        }
        void BatteryManager_OnDischargingTimeChange()
        {
            UpdateBatteryIcon();
        }
        void BatteryManager_OnChargingTimeChange()
        {
            UpdateBatteryIcon();
        }
        void BatteryManager_OnLevelChange()
        {
            UpdateBatteryIcon();
        }
        void BatteryManager_OnChargingChange()
        {
            UpdateBatteryIcon();
        }
    }
}
