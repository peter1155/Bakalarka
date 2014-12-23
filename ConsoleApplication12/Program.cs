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
        public static void GenerateDiffGram(string originalFile, string finalFile,
                            XmlWriter diffGramWriter)
        {
            XmlDiff xmldiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder |
                                         XmlDiffOptions.IgnoreNamespaces |
                                         XmlDiffOptions.IgnorePrefixes
                                         );
            bool bIdentical = xmldiff.Compare(originalFile, finalFile, false, diffGramWriter);
            diffGramWriter.Close();
        }

        public static void createXMLDoc()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement element1 = doc.CreateElement(string.Empty, "actions", string.Empty);
            doc.AppendChild(element1);
            doc.Save("RecordedActions.xml");
        }

        static void Main(string[] args)
        {
            ABB.SrcML.Src2SrcMLRunner my_runner = new ABB.SrcML.Src2SrcMLRunner();
            
            //String my_source = my_runner.GenerateSrcMLFromString("#include <stdio.h> \nint main() {\n int i,j;\n printf(\"\");}", ABB.SrcML.Language.C);
            //String my_change = my_runner.GenerateSrcMLFromString("#include <stdio.h> \nint main() {\n int i,j;\n printf(\"dfs\");}", ABB.SrcML.Language.C);
            my_runner.GenerateSrcMLFromFile("Source_code1.c", 
                "source_data1.xml", ABB.SrcML.Language.C);
            my_runner.GenerateSrcMLFromFile("Source_code2.c",
                 "source_data2.xml", ABB.SrcML.Language.C);
            
            XDocument document = XDocument.Load("source_data1.xml");
            String source_str = document.ToString();
            source_str = source_str.Replace("pos:line", "line");
            source_str = source_str.Replace("pos:column", "column");
            document = XDocument.Parse(source_str);
            document.Save("source_data1.xml");


            XDocument document2 = XDocument.Load("source_data2.xml");
            String changed_str = document2.ToString();
            changed_str = changed_str.Replace("pos:line", "line");
            changed_str = changed_str.Replace("pos:column", "column");
            document2 = XDocument.Parse(changed_str);
            document2.Save("source_data2.xml");


            //////////////////////////////////////////// Indexing xml elements ///////////////////////////
            XmlDocument doc1 = new XmlDocument();
            XmlDocument doc2 = new XmlDocument();
            doc1.Load("source_data1.xml");
            doc2.Load("source_data2.xml");

            XmlNode node1 = doc1.DocumentElement;
            XmlNode node2 = doc2.DocumentElement;

            SimilarityBasedIndexing indexing = new SimilarityBasedIndexing();

            // Indexovanie 2. úrovne
            indexing.computeSimilarity(doc1, doc2, node1, node2);
            
            
            
            // Indexovanie 3. úrovne
            XmlNodeList list1 = node1.ChildNodes;
            XmlNodeList list2 = node2.ChildNodes;
            for (int i = 0; i < list1.Count; i++)
                for (int j = 0; j < list2.Count;j++)
                {
                    var idAtrib = list1.Item(i).Attributes.GetNamedItem("id");
                    var idAtrib2 = list2.Item(j).Attributes.GetNamedItem("id");
                    if(idAtrib != null && idAtrib2 != null && idAtrib.Value == idAtrib2.Value)
                    {
                        indexing.computeSimilarity(doc1, doc2, list1.Item(i), list2.Item(j));
                        
                        // Indexovanie 4. úrovne
                        XmlNodeList list3 = list1.Item(i).ChildNodes;
                        XmlNodeList list4 = list2.Item(j).ChildNodes;
                        foreach(XmlNode list3Node in list3)
                            foreach(XmlNode list4Node in list4)
                            {
                                if (list3Node.Attributes != null && list4Node.Attributes != null)
                                {
                                    idAtrib = list3Node.Attributes.GetNamedItem("id");
                                    idAtrib2 = list4Node.Attributes.GetNamedItem("id");
                                    if (idAtrib != null && idAtrib2 != null && idAtrib.Value == idAtrib2.Value)
                                    {
                                        indexing.computeSimilarity(doc1, doc2, list3Node, list4Node);
                                    }
                                }
                            }
                    }
                }
                //var similarAtrib = root2.Attributes.GetNamedItem("similarity");
            
            // Indexovanie 4. úrovne


            /*XmlNodeList list3;
            XmlNodeList list4;
            for (int i = 0; i < list1.Count;i++)
            {
                list3 = list1.Item(i).ChildNodes;
                
            }*/

            doc1.Save("source_data1.xml");
            doc2.Save("source_data2.xml");

            ///////////////////////////////////////// Indexing elements end ///////////////////////////////
            String  diffString = "tempDiff.xml";
            XmlWriter writer = XmlWriter.Create(diffString);

            //GenerateDiffGram("source_data1.xml", "source_data2.xml", writer);
            //System.Console.WriteLine(diffString.ToString());
            
            System.Console.ReadLine();

            //System.Console.WriteLine(source_str);
           
            //System.Console.ReadLine();

            /*foreach (var el in document.Descendants())
            {
                if(el.Name != "unit")
                {
                    el.RemoveAttributes();
                }
            }

            document.Save("source_data1.xml");

            document = XDocument.Load("source_data2.xml");

            //System.Console.ReadLine();

            foreach (var el in document.Descendants())
            {
                if (el.Name != "unit")
                {
                    el.RemoveAttributes();
                }
            }

            document.Save("source_data2.xml");

            System.Console.WriteLine(document);*/
            //System.Console.ReadLine();


            string filename = "libxmldiff_new\\xmldiff";
            // Ako separator pouzivam tildu ta by sa v kode nemala vyskytnut
            string parameteres = " diff --ids @id --ignore @line,@column,@similarity --sep ~ source_data1.xml source_data2.xml difference.xml";
            /*if (File.Exists("difference.xml"))
            {
                File.Delete("difference.xml");
            }*/

            Process p = new Process();
            p.StartInfo.FileName = filename;
            p.StartInfo.Arguments = parameteres;
            p.Start();
            p.WaitForExit();

            XmlDocument doc = new XmlDocument();
            //if (p.ExitCode == 0)   // Vracia exit code 4 nejaka divocina
            doc.Load("difference.xml");             // nacitanie vytvoreneho xml suboru
            /*else
            {
                System.Console.WriteLine("Nepodarilo sa vygenerovat diff subor");
                System.Console.ReadLine();
                return;
            }*/

            createXMLDoc();
            string xmlcontents = doc.InnerXml;
            xmlcontents = xmlcontents.Replace("line", "pos:line");
            xmlcontents = xmlcontents.Replace("column", "pos:column");
            
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
            // Vytvaram manager a navigator na parsovanie xml pomocou xpath

            OutputChangeActivity outputActivity = new OutputChangeActivity();
            outputActivity.findDifferenceInOutput(manager, navigator);

            InputChangeActivity inputActivity = new InputChangeActivity();
            //inputActivity.findDifferenceInOutput(manager, navigator);

            SpecialCaseActivity specialCaseActivity = new SpecialCaseActivity();
            //specialCaseActivity.findDifferenceInOutput(manager, navigator);


            
        }
    }
}
