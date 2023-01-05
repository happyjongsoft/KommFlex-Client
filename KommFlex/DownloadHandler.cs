using System;

// This is handler to download file from CEF Browser
namespace CefSharp.Example.Handlers
{
    public class DownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;

        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            
            var handler = OnBeforeDownloadFired;

            if (handler != null)
            {
                handler(this, downloadItem);
            }

            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    string fn = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                @"\Downloads\" +
                                downloadItem.SuggestedFileName;

                    callback.Continue(fn, 
                            showDialog: false);
                }
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            var handler = OnDownloadUpdatedFired;
            if (handler != null)
            {
                handler(this, downloadItem);
            }
        }
    }
}