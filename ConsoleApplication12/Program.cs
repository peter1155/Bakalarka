using Microsoft.XmlDiffPatch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
            String my_source = my_runner.GenerateSrcMLFromString("#include <stdio.h> \nint main() {printf(\"Hello peter\");}", ABB.SrcML.Language.C);
            String my_change = my_runner.GenerateSrcMLFromString("#include <stdio.h> \nint main() {printf(\"Hello world\");}", ABB.SrcML.Language.C);

            // Create the XmlDocument.
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(my_source);

            // Save the document to a file. White space is 
            // preserved (no white space).
            //doc.PreserveWhitespace = true;
            doc.Save("libxmldiff_new\\source_data1.xml");   // ulozi obsah vygenerovaneho xml pred zmenou
            doc.LoadXml(my_change);
            doc.Save("libxmldiff_new\\source_data2.xml");   // ulozi obsah vygenerovaneho xml po zmene

            string filename = "libxmldiff_new\\xmldiff";
            string parameteres = " diff source_data1.xml source_data2.xml difference.xml";
            Process.Start(filename,parameteres);

            doc.Load("difference.xml");             // nacitanie vytvoreneho xml suboru
            string xmlcontents = doc.InnerXml;

            System.Console.WriteLine(xmlcontents + "\n"); 
            System.Console.WriteLine(my_source + "\n");
            System.Console.WriteLine(my_change + "\n");
            
            
            //XmlDocument xml = new XmlDocument();
            //xml = doc;

            //XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            //nsmgr.AddNamespace("xd", "xmlns:xd");

            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document = new XPathDocument(reader);
            XPathNavigator navigator = document.CreateNavigator();
            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
            manager.AddNamespace("cpp", "http://www.sdml.info/srcML/cpp");
            manager.AddNamespace("lit", "http://www.sdml.info/srcML/literal");
            manager.AddNamespace("op", "http://www.sdml.info/srcML/operator");
            manager.AddNamespace("type", "http://www.sdml.info/srcML/modifier");
            manager.AddNamespace("pos", "http://www.sdml.info/srcML/position");
            manager.AddNamespace("diff", "http://www.via.ecp.fr/~remi/soft/xml/xmldiff");

            XPathNodeIterator nodes = navigator.Select("name",manager);
            
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
