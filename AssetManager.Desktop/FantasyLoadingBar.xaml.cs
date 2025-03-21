using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AssetManager.Desktop
{
    public partial class FantasyLoadingBar : UserControl
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(FantasyLoadingBar),
                new PropertyMetadata(0.0, OnProgressChanged));

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public double ProgressPercent
        {
            get { return Math.Round(Progress * 100); }
        }

        public FantasyLoadingBar()
        {
            InitializeComponent();
        }

        // Add this to your FantasyLoadingBar class
        public void UpdateProgressBar()
        {
            if (ProgressBarContainer != null && ProgressIndicator != null)
            {
                ProgressIndicator.Width = Progress * ProgressBarContainer.ActualWidth;
            }
        }

        // And modify the OnProgressChanged method to call it:
        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as FantasyLoadingBar;
            if (control != null)
            {
                // Clamp the value between 0 and 1
                control.Progress = Math.Max(0, Math.Min(1, (double)e.NewValue));

                // Update the UI
                if (control.ProgressText != null)
                {
                    control.ProgressText.Text = $"{control.ProgressPercent}%";
                }

                control.UpdateProgressBar();
            }
        }

        // Also handle size changes
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateProgressBar();
        }

        private void ProgressBarContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProgressBar();
        }
    }
}