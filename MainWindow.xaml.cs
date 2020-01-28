using Microsoft.Win32;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Xml;

namespace XMLSigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private static readonly String baseUrl = Properties.Resources.ApiUrl;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                //ServerFileName.Text = File.ReadAllText(openFileDialog.FileName);
                SelectedFileName.Text = openFileDialog.FileName;
            }
        }

        private void SignButtonClicked(object sender, RoutedEventArgs e)
        {
            //http://localhost:8080/file/download/831286aaff629434d9d4ddcbec679f8ecbe4afb0631c73e60d0968fbea2cccbea63aa8afb0a5bef528a3e2433c0d9994713da42fdadd62f5fab19d0365520e3cbig.xml
            String fileName = SelectedFileName.Text.Trim();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = false;/////////////////////////////////
            xmlDoc.Load(fileName);

            X509Certificate2 cert = XmlSignWithAspFunction.GetX509Certificate2();
            XmlSignWithAspFunction.GetSignedXMLDocument(xmlDoc, cert).Save(fileName + "_signed.xml");

            //Verify
            bool result = XmlSignWithAspFunction.VerifyXmlFileWithoutCertificateVerification(fileName + "_signed.xml", cert);
            if(result) {
                MessageBox.Show("Verified");
            } else {
                MessageBox.Show("Failed Verification");
            }
        }
    }
}
