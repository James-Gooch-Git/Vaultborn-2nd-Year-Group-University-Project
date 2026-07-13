using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace AssetManager.Desktop
{
    /// <summary>
    /// Shared WebView2 initialization boilerplate: ensures the CoreWebView2 runtime is
    /// initialized and (optionally) attaches a NavigationStarting handler exactly once.
    /// Per-site concerns (URLs, navigation, other handlers) stay at the call sites.
    /// </summary>
    internal static class WebViewHelper
    {
        public static async Task InitializeAsync(WebView2 view, EventHandler<CoreWebView2NavigationStartingEventArgs> onNavStarting = null)
        {
            if (view.CoreWebView2 == null)
            {
                await view.EnsureCoreWebView2Async();
            }

            if (onNavStarting != null)
            {
                // Detach first so repeated calls never double-subscribe.
                view.CoreWebView2.NavigationStarting -= onNavStarting;
                view.CoreWebView2.NavigationStarting += onNavStarting;
            }
        }
    }
}
