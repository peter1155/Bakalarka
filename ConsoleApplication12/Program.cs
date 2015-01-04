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
            DateTime start = DateTime.Now;
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
           

            SimilarityBaseIndexing indexing = new SimilarityBaseIndexing();
            indexing.xmlIndexing("source_data1.xml","source_data2.xml");
         

            ///////////////////////////////////////// Indexing elements end ///////////////////////////////
            String  diffString = "tempDiff.xml";
            XmlWriter writer = XmlWriter.Create(diffString);

            //GenerateDiffGram("source_data1.xml", "source_data2.xml", writer);
            //System.Console.WriteLine(diffString.ToString());
            
            

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
            string parameteres = " diff --ids @id --ignore @line,@column,@similarity,@temp_id --sep ~ source_data1.xml source_data2.xml difference.xml";
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

            // Hladam zmeny v outpute
            OutputChangeActivity outputActivity = new OutputChangeActivity();
            outputActivity.findDifferenceInOutput(manager, navigator);

            // Hladam zmeny v inpute
            InputChangeActivity inputActivity = new InputChangeActivity();
            inputActivity.findDifferenceInInput(manager, navigator);

            // Hladam pridane funkcie
            AddFunctionActivity addFunctionActivity = new AddFunctionActivity();
            addFunctionActivity.findAddedFunctions(manager, navigator);

            // Hladam zmenene volania funkcie
            CallFunctionChangeActivity callFunctionChangedActivity = new CallFunctionChangeActivity();
            callFunctionChangedActivity.findChangedFunctionCalls(manager,navigator);
            
            // Hladam pridanie okrajovych pripadov
            SpecialCaseActivity specialCaseActivity = new SpecialCaseActivity();
            specialCaseActivity.findSpecialCaseAdd(manager, navigator);

            // Hladam zmenene podmienky
            ConditionChangeActivity conditionChangeActivity = new ConditionChangeActivity();
            conditionChangeActivity.findConditionChange(manager, navigator);
            
            // Hladam zmenu na podmienkach cyklu
            LoopChangeActivity loopChangeActivity = new LoopChangeActivity();
            loopChangeActivity.findLoopChange(manager,navigator);

            // Hladam zrusene premenne
            VariableDeclarationActivity variableDeclarationActivity = new VariableDeclarationActivity();
            variableDeclarationActivity.findVariableRemoved(manager, navigator);

            // Hladam zmenu v indexovani poli
            ArrayIndexingActivity arrayIndexingActivity = new ArrayIndexingActivity();
            arrayIndexingActivity.findArrayIndexingModification(manager, navigator);

            // Hladam zrusene pomocne vypisy
            OutputCanceledActivity outputCanceledActivity = new OutputCanceledActivity();
            outputCanceledActivity.findCanaceledOutput(manager,navigator);

            TimeSpan timeItTook = DateTime.Now - start;
            Console.WriteLine(timeItTook);
            Console.ReadKey();
        }
    }
}
