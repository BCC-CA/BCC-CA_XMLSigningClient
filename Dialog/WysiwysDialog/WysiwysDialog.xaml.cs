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
        private readonly string receivedXmlText;
        private List<Certificate> certificateList;
        private XmlDocument realDocument;

        public WysiwysDialog(string xmlText)
        {
            InitializeComponent();
            receivedXmlText = xmlText;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            XmlDocument tempXml = new XmlDocument();
            tempXml.LoadXml(receivedXmlText);
            certificateList = XmlSign.GetAllSign(tempXml);
            realDocument = XmlSign.GetRealXmlDocument(tempXml);
            XmlDataProvider dataProvider = this.FindResource("xmlDataProvider") as XmlDataProvider;
            dataProvider.Document = realDocument;
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
