//#R System.dll
//#R System.Xml.dll

using System;
using MyMis.Scripting.Core.Contracts;
using System.Collections.Generic;
using System.Xml.Xsl;
using System.Xml;
using System.IO;
using System.Linq;




namespace myMIS
{
    public class Script
    {
        public string xsltString = @"
<xsl:stylesheet version=""2.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
  xmlns:msxsl=""urn:schemas-microsoft-com:xslt"">
<xsl:output method=""xml"" indent=""yes""  />
<xsl:template match=""Customer"">
<html>
<head></head>
<body>
<h1>Customer File</h1>
<h2><xsl:value-of select=""Name""/></h2>
<h3><xsl:value-of select=""Description""/></h3>
<p><b>Address: </b><xsl:value-of select=""Address""/></p>
<p><b>Contract validity: </b><xsl:value-of select=""ContractStart""/> - <xsl:value-of select=""ContractEnd""/></p>
<p><b>Financial ID: </b><xsl:value-of select=""FinancialID""/></p>
</body>
</html>
</xsl:template>
</xsl:stylesheet>
";
        public ScriptResponse Execute(ContextData context, Entity document, Dictionary<string,object> parameters)
        {
            //PREPARE XML FILE FROM DOCUMENT
            Customer customer = new Customer();
            customer.Code = document.Code;
            customer.Name = document.Name;
            customer.Description = document.Description;
            customer.Address = document.Attributes.Address;
            customer.ContractStart = document.Attributes.ContractStart.ToShortDateString();
            customer.ContractEnd = document.Attributes.ContractEnd.ToShortDateString();
            customer.FinancialID = document.Attributes.FinancialID;

            var externalSystem = context.ExternalSystems.FirstOrDefault().Value;

            string path = externalSystem.Parameters["FilePath"].ToString();

            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(customer.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                //OUTPUT HTML FROM XML
                x.Serialize(textWriter, customer);

                var html = TransformXMLToHTML(textWriter.ToString(), xsltString);
                File.WriteAllText(path + $"{document.Code}.html", html);
                Console.WriteLine("HTML at: " + path + $"{document.Code}.html");
            }

            var message = "Successful!";

            return new ScriptResponse
            {
                Message = message
            };
        }

        public static string TransformXMLToHTML(string inputXml, string xsltString)
        {
            XslCompiledTransform transform = new XslCompiledTransform();
            using (XmlReader reader = XmlReader.Create(new StringReader(xsltString)))
            {
                transform.Load(reader);
            }
            StringWriter results = new StringWriter();
            using (XmlReader reader = XmlReader.Create(new StringReader(inputXml)))
            {
                transform.Transform(reader, null, results);
            }
            return results.ToString();
        }

    }

    public class Customer
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string ContractStart { get; set; }
        public string ContractEnd { get; set; }
        public string FinancialID { get; set; }
    }
}