using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using XMLSigner.Library;
using XMLSigner.Model;

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
            /*
            XmlDocument tempDoc = new XmlDocument();
            tempDoc.LoadXml(xmlToSign.InnerText);
            List<Certificate> certificateList = XmlSign.GetAllSign(tempDoc);
            XmlDocument realDocument = XmlSign.GetRealXmlDocument(tempDoc);
            */
            XmlDataProvider dataProvider = this.FindResource("xmlDataProvider") as XmlDataProvider;
            dataProvider.Document = xmlToSign;
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
