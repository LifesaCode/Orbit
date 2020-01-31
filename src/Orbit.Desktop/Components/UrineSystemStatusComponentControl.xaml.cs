using System;
using System.Windows.Controls;

using NLog;

using Orbit.Models;
using Orbit.Util;

namespace Orbit.Desktop.Components
{
    /// <summary>
    /// Interaction logic for UrineSystemStatusComponentControl.xaml
    /// </summary>
    public partial class UrineSystemStatusComponentControl : UserControl
    {
        private static readonly Lazy<ILogger> _logger = new Lazy<ILogger>(LogManager.GetCurrentClassLogger);

        private static ILogger Logger => _logger.Value;

        public AlertViewModel UrineSystemStatusAlert {
            get => (AlertViewModel)DataContext;
            set => DataContext = value;
        }

        public UrineSystemStatusComponentControl()
        {
            this.InitializeComponent();
            // Subscribe to updates here
            EventMonitor.Instance.AlertReported += this.Instance_AlertReported;
        }

        private void Instance_AlertReported(object? sender, AlertEventArgs e)
        {
            if (e.Alert.PropertyName == nameof(UrineSystemData.Status) || e.Alert.PropertyName == nameof(UrineSystemData.UrineTankLevel))
            {
                // Handle level alert
                if (e.Report is UrineSystemData data)
                {
                    Logger.Info("Alert reported: {data}", new { data.Status, e.Alert.Message });
                    if (this.UrineSystemStatusAlert == null)
                    {
                        this.UrineSystemStatusAlert = new AlertViewModel(data, e.Alert);
                    }
                    else
                    {
                        this.UrineSystemStatusAlert.Model = data;
                        this.UrineSystemStatusAlert.Alert = e.Alert;
                    }
                }
            }
        }
    }
}
