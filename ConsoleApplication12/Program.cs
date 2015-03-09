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
        // Vytvori xml subor na zapis zaznamenanych cinnosti
        public static void CreateXMLDoc()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement element1 = doc.CreateElement(string.Empty, "actions", string.Empty);
            doc.AppendChild(element1);
            doc.Save("RecordedActions.xml");
        }

        // Sluzi na vymazanie XML elementu, tak aby ostali zachovany jeho potomkovia
        private static void RemoveBlocks(XmlNode parent, XmlNode childToRemove)
        {
            while (childToRemove.HasChildNodes)
                parent.InsertBefore(childToRemove.ChildNodes[0], childToRemove);

            parent.RemoveChild(childToRemove);
        }

        // Rekurzivne prechadza cele XML a vymazava block elementy
        private static void RecursiveXmlPreProcessing(XmlNodeList list1, XmlDocument doc)
        {
            foreach (XmlNode listNode in list1)
            {
                if(listNode.Name == "block")
                {
                    XmlNode parent = listNode.ParentNode;
                    RemoveBlocks(parent, listNode);
                    if (parent.HasChildNodes)
                        RecursiveXmlPreProcessing(parent.ChildNodes,doc);
                }
                else if (listNode.HasChildNodes)
                    RecursiveXmlPreProcessing(listNode.ChildNodes,doc);
            }
        }

        // Vymaze block elementy z xml suboru s nazvom fileName
        // zaroven zmeni elementy fro, do, while na cycle a typ cyklu sa ulozi do atributu
        private static void XmlPreProcessing(String fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);
            RecursiveXmlPreProcessing(doc.ChildNodes,doc);
            doc.Save(fileName);
        }
        
        static void Main(string[] args)
        {
            // pocita spracovane kody
            int taskCounter = 0;
            
            // Time messurement
            DateTime allTaskStart = DateTime.Now;

            // Nacitanie konfiguracie aplikacie
            try
            {
                Options.LoadProgramConfiguration();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                return;
            }
            // Ziskava instanciu triedy Src2SrcMLRunner na preklad zdrojoveho kodu do xml
            ABB.SrcML.Src2SrcMLRunner my_runner = new ABB.SrcML.Src2SrcMLRunner();

            // Prehadza vsetkymi subormi pre danu ulohu a identifikuje vybrane cinnosti
            for (int student = Options.StartStudent; student < Options.EndStudent; student++)
            {
                for (int pokus = Options.StartAttempt; pokus < Options.EndAttempt; pokus++)
                {
                    Boolean fileExist = true;

                    // Vysklada meno suboru a overi ci dany subor existuje 
                    String fileName1 = Options.TaskPath + student.ToString("D4") + "_" + pokus.ToString("D2") + "_" + "wrong";
                    if (!File.Exists(fileName1 + ".c"))
                        fileExist = false;

                    // Ak subor neexituje prejde na dalsieho studenta
                    if (!fileExist)
                        break;

                    fileExist = true;

                    // Vysklada meno suboru a overi ci dany subor existuje 
                    String fileName2 = Options.TaskPath + student.ToString("D4") + "_" + (pokus + 1).ToString("D2") + "_" + "wrong";
                    if (!File.Exists(fileName2 + ".c"))
                        fileExist = false;

                    // Vysklada meno suboru a overi ci dany subor existuje 
                    if (!fileExist)
                        fileName2 = Options.TaskPath + student.ToString("D4") + "_" + (pokus + 1).ToString("D2") + "_" + "correct";

                    fileExist = true;
                    if (!File.Exists(fileName2 + ".c"))
                        fileExist = false;

                    // Ak neexistuje ani jeden zo suborov prejde na dalsieho studenta
                    if (!fileExist)
                        break;

                    // Time messurement
                    DateTime start = DateTime.Now;

                    FileInfo fileInfo1 = new FileInfo(fileName1 + ".c");
                    long size1 = fileInfo1.Length / 1000;

                    FileInfo fileInfo2 = new FileInfo(fileName2 + ".c");
                    long size2 = fileInfo2.Length / 1000;

                    // Ak je velkost niektoreho zo suborov viac ako 50 KB chod na dalsi
                    if (size1 > 50 || size2 > 50)
                        continue;
                    
                    //////////////////////////////////////////// SrcToSrcML preklad ////////////////////////////////////////////////////

                    // Prelozi subori so zdrojovym kodom do formatu xml 
                    my_runner.GenerateSrcMLFromFile(fileName1 + ".c",
                            "source_data1.xml", ABB.SrcML.Language.C);
                    my_runner.GenerateSrcMLFromFile(fileName2 + ".c",
                         "source_data2.xml", ABB.SrcML.Language.C);

                    //////////////////////////////////////////// SrcToSrcML preklad ////////////////////////////////////////////////////

                    XmlPreProcessing("source_data1.xml");
                    XmlPreProcessing("source_data2.xml");

                    // Pre potreby xml-diffu vymaze z atributov line a column prefix pos v subore source_data1.xml
                    XDocument document = XDocument.Load("source_data1.xml");
                    String source_str = document.ToString();
                    source_str = source_str.Replace("pos:line", "line");
                    source_str = source_str.Replace("pos:column", "column");
                    document = XDocument.Parse(source_str);
                    document.Save("source_data1.xml");

                    // Pre potreby xml-diffu vymaze z atributov line a column prefix pos v subore source_data2.xml
                    XDocument document2 = XDocument.Load("source_data2.xml");
                    String changed_str = document2.ToString();
                    changed_str = changed_str.Replace("pos:line", "line");
                    changed_str = changed_str.Replace("pos:column", "column");
                    document2 = XDocument.Parse(changed_str);
                    document2.Save("source_data2.xml");


                    //////////////////////////////////////////// Indexing xml elements ///////////////////////////


                    SimilarityBaseIndexing indexing = new SimilarityBaseIndexing();
                    indexing.XmlIndexing("source_data1.xml", "source_data2.xml");

                    ///////////////////////////////////////// Indexing elements end ///////////////////////////////


                    ///////////////////////////////////////// XML Differencing ////////////////////////////////////

                    // V zavislosti od vybranej metody sa veberie sposob diffovania
                    if (Options.Method == Options.Methods.Fast)
                    {
                        string filename = "libxmldiff_new\\xmldiff";

                        // Ako separator je pouzita tilda ta by sa v kode nemala vyskytnut resp. minimalne, diffuje sa podla ids vypocitanych
                        // v similarityBasedIndexing classe, ignoruju sa cisla riadkov a stlpcov a zaroven similarity atribut
                        string parameteres = " diff --ids @id --ignore @line,@column,@similarity,@temp_id --sep ~ source_data1.xml source_data2.xml difference.xml";

                        Process p = new Process();
                        p.StartInfo.FileName = filename;
                        p.StartInfo.Arguments = parameteres;
                        p.Start();
                        p.WaitForExit();
                    }
                    else
                    {
                        XMLDiff myDiffer = new XMLDiff();
                        myDiffer.DiffXmlFiles("source_data1.xml", "source_data2.xml");
                    }

                    ////////////////////////////////////////// XML Differencing /////////////////////////////////////

                    //xmlPostIndexingProcessing("difference.xml");

                    // Nacitanie vytvoreneho xml suboru
                    XmlDocument doc = new XmlDocument();
                    doc.Load("difference.xml");

                    // Vytvorenie suboru na zapis identifikovanych zmien
                    CreateXMLDoc();

                    // Spatne pridanie prefixov pre atributy line a column
                    string xmlcontents = doc.InnerXml;
                    xmlcontents = xmlcontents.Replace("line", "pos:line");
                    xmlcontents = xmlcontents.Replace("column", "pos:column");
                    doc.LoadXml(xmlcontents);
                    doc.Save("difference.xml");


                    // Vytvorenie xPathNavigatora pre dopytovanie diffDocumentu
                    XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
                    XPathDocument document_xpath = new XPathDocument(reader);
                    XPathNavigator navigator = document_xpath.CreateNavigator();

                    // Vytvorenie XmlNamespaceManagera aby bolo mozne dopytovat elementy s roznymi prefixami
                    XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
                    manager.AddNamespace("base", "http://www.sdml.info/srcML/src");
                    manager.AddNamespace("cpp", "http://www.sdml.info/srcML/cpp");
                    manager.AddNamespace("lit", "http://www.sdml.info/srcML/literal");
                    manager.AddNamespace("op", "http://www.sdml.info/srcML/operator");
                    manager.AddNamespace("type", "http://www.sdml.info/srcML/modifier");
                    manager.AddNamespace("pos", "http://www.sdml.info/srcML/position");
                    manager.AddNamespace("diff", "http://www.via.ecp.fr/~remi/soft/xml/xmldiff");

                    // Hlada zmeny v outpute
                    OutputChangeActivity outputActivity = new OutputChangeActivity();
                    outputActivity.FindDifferenceInOutput(manager, navigator);

                    // Hlada zmeny v inpute
                    InputChangeActivity inputActivity = new InputChangeActivity();
                    inputActivity.FindDifferenceInInput(manager, navigator);

                    // Hlada pridane funkcie
                    AddFunctionActivity addFunctionActivity = new AddFunctionActivity();
                    addFunctionActivity.FindAddedFunctions(manager, navigator);

                    // Hlada zmenene volania funkcie
                    CallFunctionChangeActivity callFunctionChangedActivity = new CallFunctionChangeActivity();
                    callFunctionChangedActivity.FindChangedFunctionCalls(manager, navigator);

                    // Hlada pridanie okrajovych pripadov
                    SpecialCaseActivity specialCaseActivity = new SpecialCaseActivity();
                    specialCaseActivity.FindSpecialCaseAdd(manager, navigator);

                    // Hladam zmenene podmienky
                    ConditionChangeActivity conditionChangeActivity = new ConditionChangeActivity();
                    conditionChangeActivity.FindConditionChange(manager, navigator);

                    // Hlada zmenu na podmienkach cyklu
                    LoopChangeActivity loopChangeActivity = new LoopChangeActivity();
                    loopChangeActivity.FindLoopChange(manager, navigator);

                    // Hlada zrusene premenne
                    VariableDeclarationActivity variableDeclarationActivity = new VariableDeclarationActivity();
                    variableDeclarationActivity.FindVariableRemoved(manager, navigator);

                    // Hlada zmenu v indexovani poli
                    ArrayIndexingActivity arrayIndexingActivity = new ArrayIndexingActivity();
                    arrayIndexingActivity.FindArrayIndexingModification(manager, navigator);

                    // Hlada zrusene pomocne vypisy
                    OutputCanceledActivity outputCanceledActivity = new OutputCanceledActivity();
                    outputCanceledActivity.FindCanaceledOutput(manager, navigator);

                    // Hlada zakomentovane a odkomentovane casti kodu
                    CommentActivity commentActivity = new CommentActivity(fileName1, fileName2);
                    commentActivity.FindCanaceledOutput(manager, navigator);

                    // Hlada refctoring kodu
                    RefactorActivity refactorActivity = new RefactorActivity();
                    refactorActivity.FindRefactoring();

                    // Pomocou postProcessingu hlada niektore presunute elementy
                    MoveDetection moveDetection = new MoveDetection();
                    moveDetection.detectMoves();

                    // Time messurement
                    TimeSpan timeItTook = DateTime.Now - start;

                    if (Options.Time == Options.ShowTime.Show)
                    {
                        // Vypis casu potrebneho na vypocet 
                        Console.WriteLine(timeItTook);
                    }

                    // Prekopirovanie suboru so zaznamenanymi cinnostami do prislusneho priecinka
                    File.Copy("RecordedActions.xml", fileName2 + ".xml",true);
                    Console.WriteLine("Subor: " + fileName2 + " spracovany");
                    taskCounter++;
                }
            }
            
            if(taskCounter == 0)
            {
                Console.WriteLine("Pravdepodobne bola zadana nespravna cesta: "+Options.TaskPath);
                Console.ReadKey();
                return;
            }

            // Time messurement
            if (Options.Time == Options.ShowTime.Show)
            {
                TimeSpan timeItTookAll = DateTime.Now - allTaskStart;
                Console.WriteLine("Celkovy pocet vytvorenych zaznamov: {0}",taskCounter);
                Console.WriteLine("Priemerny cas spracovania na ulohu: " + (float)timeItTookAll.TotalSeconds / (float)taskCounter);
            }
            else
            {
                Console.WriteLine("Process finished sucessfully");
            }
            Console.ReadKey();
        }
    }
}
