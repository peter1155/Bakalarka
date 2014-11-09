using Microsoft.XmlDiffPatch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConsoleApplication12
{
    class Program
    {
        /*public static void GenerateDiffGram(string originalSource, string changedSource,
                                    XmlWriter diffGramWriter)
        {
            XmlDiff xmldiff = new XmlDiff(XmlDiffOptions.IgnoreNamespaces | XmlDiffOptions.IgnorePrefixes);
            XmlReader sourceReader = XmlReader.Create(new StringReader(originalSource));
            XmlReader changedReader = XmlReader.Create(new StringReader(changedSource));

            bool bIdentical = xmldiff.Compare(sourceReader, changedReader, diffGramWriter);
            diffGramWriter.Close();
        }*/

        static void Main(string[] args)
        {
            ABB.SrcML.Src2SrcMLRunner my_runner = new ABB.SrcML.Src2SrcMLRunner();
           
            String my_source = my_runner.GenerateSrcMLFromString("#include <stdio.h> \nint main() {\n int i;\n printf(\"Hello peter crow %d\",i);}", ABB.SrcML.Language.C);
            String my_change = my_runner.GenerateSrcMLFromString("#include <stdio.h> \nint main() { int i;\n printf(\"Hello woooo %d\",i);}", ABB.SrcML.Language.C);

           
            XDocument document = XDocument.Parse(my_source);
            
           
            //System.Console.ReadLine();

            foreach (var el in document.Descendants())
            {
                if(el.Name != "unit")
                {
                    el.RemoveAttributes();
                }
            }

            document.Save("source_data1.xml");



            document = XDocument.Parse(my_change);


            //System.Console.ReadLine();

            foreach (var el in document.Descendants())
            {
                if (el.Name != "unit")
                {
                    el.RemoveAttributes();
                }
            }

            document.Save("source_data2.xml");

            System.Console.WriteLine(document);
            //System.Console.ReadLine();

            
            string filename = "libxmldiff_new\\xmldiff";
            string parameteres = " diff source_data1.xml source_data2.xml difference.xml";
            Process.Start(filename, parameteres);
            
            
            XmlDocument doc = new XmlDocument();
            doc.Load("difference.xml");             // nacitanie vytvoreneho xml suboru
            string xmlcontents = doc.InnerXml;

            
            //XmlDocument xml = new XmlDocument();
            //xml = doc;

            //XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            //nsmgr.AddNamespace("xd", "xmlns:xd");

            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();
            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
            manager.AddNamespace("base", "http://www.sdml.info/srcML/src");
            manager.AddNamespace("cpp", "http://www.sdml.info/srcML/cpp");
            manager.AddNamespace("lit", "http://www.sdml.info/srcML/literal");
            manager.AddNamespace("op", "http://www.sdml.info/srcML/operator");
            manager.AddNamespace("type", "http://www.sdml.info/srcML/modifier");
            manager.AddNamespace("pos", "http://www.sdml.info/srcML/position");
            manager.AddNamespace("diff", "http://www.via.ecp.fr/~remi/soft/xml/xmldiff");

            XPathNodeIterator nodes = navigator.Select("//base:call/base:name='printf'",manager);
            
            if (nodes.MoveNext())
            {
                // now nodes.Current points to the first selected node
                XPathNavigator nodesNavigator = nodes.Current;

                //select all the descendants of the current price node
               
                XPathNodeIterator nodesText = 
                   nodesNavigator.SelectDescendants(XPathNodeType.Element, false);
                
                while (nodesText.MoveNext())
                {
                    if(nodesText.Current.Value.Contains("for"))
                    {
                        Console.WriteLine("Editoval for");
                    }
                    Console.WriteLine("\n" + nodesText.Current.Value);
                }
            }
            System.Console.ReadLine();
        }
    }
}
