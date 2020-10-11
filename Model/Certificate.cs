using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using XMLSigner.Library;

namespace XMLSigner.Model
{
    public class Certificate
    {
        public enum ValidityStatus
        {
            [Display(Name = "Invalid Signature")]
            Invalid = 0,
            /*[Display(Name = "Application Forwarded")]
            Forwarded,*/
            [Display(Name = "Valid Signature")]
            Valid
        }

        public Certificate(X509Certificate2 certificate, string timeString)
        {
            ValidFrom = certificate.NotBefore;
            ValidTo = certificate.NotAfter;
            Issuer = certificate.Issuer;
            Subject = certificate.Subject;
            DateTime? siginigTime = Adapter.Base64DecodTime(timeString);
            if(siginigTime.HasValue)
            {
                SigningTime = (DateTime)siginigTime;
                IssuedBy = GetCN(certificate.Issuer);
                IssuedTo = GetCN(certificate.Subject);
                SerialNumber = certificate.SerialNumber;
                Validity = CheckVaildity();
            }
            else
            {
                Validity = ValidityStatus.Invalid;
            }
        }

        private ValidityStatus CheckVaildity()
        {
            if (SigningTime >= ValidFrom && SigningTime <= ValidTo)
                return ValidityStatus.Valid;
            else
                return ValidityStatus.Invalid;
        }

        private string GetCN(string data)
        {
            //.StartsWith("abc")
            foreach (string item in data.Split(","))
            {
                //Console.WriteLine("amount is {0}, and type is {1}", item.amount, item.type);
                string trimedData = item.Trim();
                if (trimedData.StartsWith("CN"))
                {
                    return trimedData.Substring(3, trimedData.Length - 3);
                }
            }
            return null;
        }

        public DateTime ValidFrom { get; }
        public DateTime ValidTo { get; }
        public string IssuedBy { get; private set; }
        public string IssuedTo { get; private set; }
        public string Issuer { get; }
        public string Subject { get; }
        //[DataType(DataType.DateTime)]
        //[DisplayFormat(ApplyFormatInEditMode = false, DataFormatString = "{0:dddd, dd MMMM yyyy HH:mm:ss.ffK tt}")]
        public DateTime SigningTime { get; }
        public string SerialNumber { get; }
        public ValidityStatus Validity { get; private set; }
    }
}
