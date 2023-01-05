using System.Collections.Generic;
using System.IO;
using System;

// This is handler to upload file automatically on CEF Browser
namespace CefSharp.Example.Handlers
{
    public class TempFileDialogHandler : IDialogHandler
    {
        public string filename;
        public TempFileDialogHandler(string fn)
        {
            filename = fn;
        }
        public bool OnFileDialog(IWebBrowser chromiumWebBrowser, IBrowser browser, CefFileDialogMode mode, CefFileDialogFlags flags, string title, string defaultFilePath, List<string> acceptFilters, int selectedAcceptFilter, IFileDialogCallback callback)
        {
            if (filename.Length > 0)
            {
                callback.Continue(selectedAcceptFilter, new List<string> { filename });
                return true;
            }
            else
            {
                return true;
            }
        }
    }

}