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
            // Ziskava instanciu triedy Src2SrcMLRunner na preklad zdrojoveho kodu do xml
            ABB.SrcML.Src2SrcMLRunner my_runner = new ABB.SrcML.Src2SrcMLRunner();

            // Prehadza vsetkymi subormi pre danu ulohu a identifikuje vybrane cinnosti
            for (int student = 2; student < 3; student++)
            {
                for (int pokus = 6; pokus < 7; pokus++)
                {
                    Boolean fileExist = true;
                    
                    // Vysklada meno suboru a overi ci dany subor existuje 
                    String fileName1 = "C:\\Users\\peto\\Desktop\\per_task_all\\Uloha_2-1\\"+student.ToString("D4") + "_" + pokus.ToString("D2") + "_" + "wrong";
                    if (!File.Exists(fileName1+".c"))
                        fileExist = false;

                    // Ak subor neexituje prejde na dalsieho studenta
                    if (!fileExist)
                        break;
                    
                    fileExist = true;

                    // Vysklada meno suboru a overi ci dany subor existuje 
                    String fileName2 = "C:\\Users\\peto\\Desktop\\per_task_all\\Uloha_2-1\\" + student.ToString("D4") + "_" + (pokus+1).ToString("D2") + "_" + "wrong";
                    if (!File.Exists(fileName2 + ".c"))
                        fileExist = false;

                    // Vysklada meno suboru a overi ci dany subor existuje 
                    if (!fileExist)
                        fileName2 = "C:\\Users\\peto\\Desktop\\per_task_all\\Uloha_2-1\\" + student.ToString("D4") + "_" + (pokus+1).ToString("D2") + "_" + "correct";

                    fileExist = true;
                    if (!File.Exists(fileName2 + ".c"))
                        fileExist = false;

                    // Ak neexistuje ani jeden zo suborov prejde na dalsieho studenta
                    if (!fileExist)
                        break;
                    
                    // Time messurement
                    DateTime start = DateTime.Now;

                    //////////////////////////////////////////// SrcToSrcML preklad ////////////////////////////////////////////////////

                    // Prelozi subori so zdrojovym kodom do formatu xml 
                    my_runner.GenerateSrcMLFromFile(fileName1+".c",
                            "source_data1.xml", ABB.SrcML.Language.C);
                    my_runner.GenerateSrcMLFromFile(fileName2+".c",
                         "source_data2.xml", ABB.SrcML.Language.C);

                    //////////////////////////////////////////// SrcToSrcML preklad ////////////////////////////////////////////////////

                    /*
                    my_runner.GenerateSrcMLFromFile("source_code1.c",
                            "source_data1.xml", ABB.SrcML.Language.C);
                    my_runner.GenerateSrcMLFromFile("source_code2.c",
                         "source_data2.xml", ABB.SrcML.Language.C);*/

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
                    indexing.xmlIndexing("source_data1.xml", "source_data2.xml");

                    ///////////////////////////////////////// Indexing elements end ///////////////////////////////
                  

                    ///////////////////////////////////////// XML Differencing ////////////////////////////////////

                    string filename = "libxmldiff_new\\xmldiff";

                    // Ako separator je pouzita tilda ta by sa v kode nemala vyskytnut resp. minimalne, diffuje sa podla ids vypocitanych
                    // v similarityBasedIndexing classe, ignoruju sa cisla riadkov a stlpcov a zaroven similarity atribut
                    string parameteres = " diff --ids @id --ignore @line,@column,@similarity,@temp_id --sep ~ source_data1.xml source_data2.xml difference.xml";
                    
                    Process p = new Process();
                    p.StartInfo.FileName = filename;
                    p.StartInfo.Arguments = parameteres;
                    p.Start();
                    p.WaitForExit();

                    ////////////////////////////////////////// XML Differencing /////////////////////////////////////

                    // Nacitanie vytvoreneho xml suboru
                    XmlDocument doc = new XmlDocument();
                    doc.Load("difference.xml");             
                   
                    // Vytvorenie suboru na zapis identifikovanych zmien
                    createXMLDoc();

                    // Spatne pridanie prefixov pre atributy line a column
                    string xmlcontents = doc.InnerXml;
                    xmlcontents = xmlcontents.Replace("line", "pos:line");
                    xmlcontents = xmlcontents.Replace("column", "pos:column");
                    
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
                    outputActivity.findDifferenceInOutput(manager, navigator);

                    // Hlada zmeny v inpute
                    InputChangeActivity inputActivity = new InputChangeActivity();
                    inputActivity.findDifferenceInInput(manager, navigator);

                    // Hlada pridane funkcie
                    AddFunctionActivity addFunctionActivity = new AddFunctionActivity();
                    addFunctionActivity.findAddedFunctions(manager, navigator);

                    // Hlada zmenene volania funkcie
                    CallFunctionChangeActivity callFunctionChangedActivity = new CallFunctionChangeActivity();
                    callFunctionChangedActivity.findChangedFunctionCalls(manager, navigator);

                    // Hlada pridanie okrajovych pripadov
                    SpecialCaseActivity specialCaseActivity = new SpecialCaseActivity();
                    specialCaseActivity.findSpecialCaseAdd(manager, navigator);

                    // Hladam zmenene podmienky
                    ConditionChangeActivity conditionChangeActivity = new ConditionChangeActivity();
                    conditionChangeActivity.findConditionChange(manager, navigator);

                    // Hlada zmenu na podmienkach cyklu
                    LoopChangeActivity loopChangeActivity = new LoopChangeActivity();
                    loopChangeActivity.findLoopChange(manager, navigator);

                    // Hlada zrusene premenne
                    VariableDeclarationActivity variableDeclarationActivity = new VariableDeclarationActivity();
                    variableDeclarationActivity.findVariableRemoved(manager, navigator);

                    // Hlada zmenu v indexovani poli
                    ArrayIndexingActivity arrayIndexingActivity = new ArrayIndexingActivity();
                    arrayIndexingActivity.findArrayIndexingModification(manager, navigator);

                    // Hlada zrusene pomocne vypisy
                    OutputCanceledActivity outputCanceledActivity = new OutputCanceledActivity();
                    outputCanceledActivity.findCanaceledOutput(manager, navigator);

                    // Hlada zakomentovane a odkomentovane casti kodu
                    CommentActivity commentActivity = new CommentActivity(fileName1,fileName2);
                    commentActivity.findCanaceledOutput(manager, navigator);

                    // Hlada refctoring kodu
                    RefactorActivity refactorActivity = new RefactorActivity();
                    refactorActivity.findRefactoring();

                    // Pomocou postProcessingu hlada niektore presunute elementy
                    MoveDetection moveDetection = new MoveDetection();
                    moveDetection.detectMoves();

                    // Time messurement
                    TimeSpan timeItTook = DateTime.Now - start;

                    // Vypis casu potrebneho na vypocet 
                    Console.WriteLine(timeItTook);

                    // Prekopirovanie suboru so zaznamenanymi cinnostami do prislusneho priecinka
                    File.Copy("RecordedActions.xml", fileName2+".xml");
                    Console.WriteLine("Subor: " + fileName2 + " spracovany");

                }
            }
            Console.ReadKey();
        }
    }
}
