using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using ProtocolSimulator.Services;
using ProtocolSimulator.Utils;
using ProtocolSimulator.ViewModels;
using ProtocolSimulator.Views;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using Zenner.Communication.Avalonia.ViewModels;
using Zenner.Communication.Client;

namespace ProtocolSimulator
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public static IServiceProvider Services { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            ConfigurationNlog();

            var service = new ServiceCollection();

            service.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddNLog();
            });

            service.AddSingleton<MainWindowViewModel>();

            service.AddSingleton<SettingWindowViewModel>();

            service.AddTransient<SettingWindow>();
            service.AddSingleton<CommunicationController>();
            service.AddSingleton<CommunicationPanelViewModel>();

            _serviceProvider = service.BuildServiceProvider();

            Services = _serviceProvider;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }


        private static void ConfigurationNlog()
        {
            var nlogConfiguration = new LoggingConfiguration();

            var uiLogTarget = new NlogRuleTarget
            {
                Name = "UI",
                Layout = "${DateTime}:{message}"
            };

            nlogConfiguration.AddTarget(uiLogTarget);

            nlogConfiguration.AddRule(NLog.LogLevel.Info,NLog.LogLevel.Fatal,uiLogTarget);

            NLog.LogManager.Configuration = nlogConfiguration;
        }
    }
}