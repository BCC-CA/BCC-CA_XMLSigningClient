//using ModernMessageBoxLib;
//https://www.nuget.org/packages/ModernMessageBoxLib/
//https://github.com/hv0905/ModernMessageBoxLibForWPF#indeterminateprogresswindow-1
using System;

namespace XMLSigner.Dialog
{
    class LoadingDialog: IDisposable
    {
        //IndeterminateProgressWindow win;
        public LoadingDialog(string message)
        {
            //win = new IndeterminateProgressWindow(message);
            ///win.Show();
            //Do Some Staff
            //await Task.Delay(5000);
            //Change the message the 2nd time
            //win.Message = "Done!!!";
        }

        public void Dispose()
        {
            //win.Close();
        }
    }
}
