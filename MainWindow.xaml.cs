using Microsoft.Win32;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Xml;
using XMLSigner.Library;

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
            //xmlDoc.PreserveWhitespace = false;/////////////////////////////////Should do it in both sign and verify
            xmlDoc.Load(fileName);

            X509Certificate2 cert = XmlSign.GetX509Certificate2FromDongle();   //Load Certificate
            XmlDocument signedDoc = XmlSign.GetSignedXMLDocument(xmlDoc, cert);
            if(signedDoc!=null) {
                signedDoc.Save(fileName + "_signed.xml");   //Sign a file
            } else {
                MessageBox.Show("File Tempered After Last Sign");
            }

            XmlDocument signedXmlDoc = new XmlDocument();
            //xmlDoc.PreserveWhitespace = false;/////////////////////////////////
            signedXmlDoc.Load(fileName + "_signed.xml");

            //Verify
            bool? ifSignVerified = XmlSign.VerifyAllSign(signedXmlDoc);
            if(ifSignVerified == true) {
                MessageBox.Show("Verified");
            } else if(ifSignVerified == false) {
                MessageBox.Show("Failed Verification");
            } else {
                MessageBox.Show("File Has No Sign");
            }
        }
    }
}
