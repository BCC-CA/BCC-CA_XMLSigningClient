using System;
using System.Windows;
using System.Xml;

namespace XMLSigner.Dialog.WysiwysDialog
{
    /// <summary>
    /// Interaction logic for WysiwysDialog.xaml
    /// </summary>
    public partial class WysiwysDialog : Window, IDisposable
    {
        public WysiwysDialog(XmlDocument xmlToSign)
        {
            InitializeComponent();
            var a = xmlToSign;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //txtAnswer.SelectAll();
            //txtAnswer.Focus();
        }

        public void Dispose()
        {
            //https://stackoverflow.com/a/568436/2193439
            //this?.Dispose();
            this.Close();
        }
    }
}
