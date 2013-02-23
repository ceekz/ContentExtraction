using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ContentExtraction
{
    class NonDispBrowser : WebBrowser
    {
        // Reference:
        //   http://www.atmarkit.co.jp/fdotnet/dotnettips/687nondispbrowser/nondispbrowser.html

        bool done;
        TimeSpan timeout = new TimeSpan(0, 0, 10);

        protected override void OnDocumentCompleted(WebBrowserDocumentCompletedEventArgs e)
        {
            if (e.Url == this.Url)
            {
                done = true;
            }
        }

        protected override void OnNewWindow(CancelEventArgs e)
        {
            e.Cancel = true;
        }

        public NonDispBrowser()
        {
            this.ScriptErrorsSuppressed = true;
        }

        public bool NavigateAndWait(string url)
        {
            base.Navigate(url);

            done = false;
            DateTime start = DateTime.Now;

            while (done == false)
            {
                if (DateTime.Now - start > timeout)
                {
                    return false;
                }
                Application.DoEvents();
            }
            return true;
        }
    }
}
