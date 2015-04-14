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

namespace ConsoleApplication12.Actions
{
    class ArrayIndexingActivity
    {
        // objekt obsahuje obsah prveho zdrojoveho xml
        private XmlDocument _doc1Global;

        // objekt obsahuje obsah druheho zdrojoveho xml
        private XmlDocument _doc2Global;
        
        // xPathDoc sluzi na dopytovanie prveho zdrojoveho xml
        private XPathDocument _xPathDoc1Global;

        // xPathDoc sluzi na dopytovanie druheho zdrojoveho xml
        private XPathDocument _xPathDoc2Global;

        // Sluzi na ziskanie elementu nazvu nadradenej funkcie
        private XElement GetFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function

            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
            }

            // Ak sa dostane az k elementu unit - jedna sa o globalnu premennu pretoze 
            // unit je root element

            if (navigator.Name == "unit")
            {
                return new XElement("global_variable");
            }

            // Ziska deti elementu function

            XPathNodeIterator function_childeren = navigator.SelectChildren(XPathNodeType.Element);

            // Najde elemnt name - medzi detmi elementu function v nom sa nachadza nazov funkcie

            while (function_childeren.MoveNext() && function_childeren.Current.Name != "name") ;

            XPathNavigator functionNameNavigator = function_childeren.Current;
            XElement functionElement = null;
            String funcName = functionNameNavigator.Value;

            // Treba osetrit pripad ze nazov funkcie sa rapidne zmenil a je povazovany za zmazany a pridany

            List<String> funcNames = new List<string>();
            funcNames.Add(funcName);
            while (function_childeren.MoveNext() && function_childeren.Current.Name == "name")
            {
                funcNames.Add(function_childeren.Current.Value);
            }

            // Ak doslo k modifikacii nazvu funkcie treba to osetrit ~ je delimiter
            // ktory sa pri diffe pouziva na oddelenie povodnej a novej hodnoty

            if (funcName.Contains("~"))
            {
                char[] del = { '~' };
                string[] beforeAfterValues = funcName.Split(del);
                functionElement = new XElement("function_name",
                    new XElement("before", beforeAfterValues[0]),
                    new XElement("after", beforeAfterValues[1]));
            }

            // Ak je vo funcNames viac ako jeden element znamena to ze element nazov sa 
            // rapidne zmenil a je povazovany za pridany a zmazany 

            else if (funcNames.Count > 1)
            {
                functionElement = new XElement("function_name",
                    new XElement("before", funcNames.ElementAt(1)),
                    new XElement("after", funcNames.ElementAt(0)));
            }
            else
            // Ak neplati ani jedna z predch. podmienok nazov funkcie sa medzi verziami nezmenil

            {
                functionElement = new XElement("function_name",
                    new XElement("before", funcName),
                    new XElement("after", funcName));
            }
            return functionElement;
        }

        // Hlada cislo riadka a stlpca daneho elementu
        public List<String> FindPosition(XPathNavigator navigator)
        {
            
            // Pohne sa smerom k vnorenemu name, ktore ma dane atributy
            navigator.MoveToChild(XPathNodeType.Element);

            // Ziska cislo riadka z atributu line
            String line = navigator.GetAttribute("line", "http://www.sdml.info/srcML/position");

            // Ziska cislo stlpca z atributu column
            String column = navigator.GetAttribute("column", "http://www.sdml.info/srcML/position");
            
            List<String> list = new List<string>();
            list.Add(line);
            list.Add(column);
            
            // Vracia list ktory obsahuje ako prvu polozku cislo riadka a ako druhu cislo stlpca v Stringu
            return list;
        }

        // Hlada typ danej premennej v zdrojovych xml suboroch
        private List<String> GetType(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Hlada rodicovsky element daneho pola bud je pole zadeclarovane ako nova premenna - decl_stmt
            // alebo je pole vstupnym parametrom nejakej funkcie - param

            while (navigator != null && String.Compare(navigator.Name, "decl_stmt") != 0
                && String.Compare(navigator.Name, "param") != 0)
            {
                navigator.MoveToParent();
            }

            // Ziska id z rodicovskeho elementu 
            String id = navigator.GetAttribute("id", "");
            
            // Vytara list typov (typ sa mohol zmenit medzi verziami preto before/after)
            List<String> types = new List<string>();
            String typeBefore;
            String typeAfter;

            // Hlada typy v zdrojovych xml suboroch
            if (navigator.Name == "decl_stmt")
            {
                typeBefore = FindTypeInSourceDeclStmt("source_data1.xml", id, manager);
                typeAfter = FindTypeInSourceDeclStmt("source_data2.xml", id, manager);
            }
            else // Name = param
            {
                typeBefore = FindTypeInSourceParam("source_data1.xml", id, manager);
                typeAfter = FindTypeInSourceParam("source_data2.xml", id, manager);
            }
            types.Add(typeBefore);
            types.Add(typeAfter);

            // Vracia typy ako list stringov prva polozka je typ pred zmenou druha list po zmene
            return types;
        }

        // Sluzi nanajdenie nazvov danej premennej - pola pred zmenou a po zmene na zaklade poskytnuteho id
        private List<String> GetName(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Ziska id z daneho elementu
            String id = navigator.GetAttribute("id", "");
            
            List<String> names = new List<string>();
            
            // Najde nazov premennej v prvej verzii XML 
            String nameBefore = FindNameInSource("source_data1.xml", id, manager);
            
            // Najde nazov premennej v druhej verzii XML
            String nameAfter = FindNameInSource("source_data2.xml", id, manager);
            
            // Prida nazvy do string listu a vracia dany string list
            names.Add(nameBefore);
            names.Add(nameAfter);
            return names;
        }
         
        // Sluzi na najdenie nazvu premennej - pola pri zmene indexovania pri pouziti v programe
        private List<String> GetNameExpresion(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Ziska id daneho elementu
            String id = navigator.GetAttribute("id", "");

            // Najde mena premennej v oboch verziach xml suboru na zaklade id
            List<String> names = new List<string>();
            String nameBefore = FindNameInSourceExpresion("source_data1.xml", id, manager);
            String nameAfter = FindNameInSourceExpresion("source_data2.xml", id, manager);
            
            // Ak sa nepodarilo jedno z mien najst 
            if (nameBefore == null || nameAfter == null)
                return null;
            names.Add(nameBefore);
            names.Add(nameAfter);

            // Vracia mena premennej vo forme string listu prva polozka meno z prvej verzie druha polozka meno z druhej verzie
            return names;
        }

        // Sluzi na najdenie typu premennej - pola pri zmene indexovania pri pouziti v programe
        private List<String> GetTypeExpression(XmlNamespaceManager manager, List<String> names, XElement funcElement)
        {
            //navigator.MoveToChild(XPathNodeType.Element);

            String nameBefore = names.ElementAt(0);
            nameBefore = nameBefore.Substring(0, nameBefore.IndexOf('['));
            String nameAfter = names.ElementAt(1);
            nameAfter = nameAfter.Substring(0, nameAfter.IndexOf('['));

             
            if (funcElement.Value == "")
            {
                String before = FindTypeInSourceExpresion2("source_data1.xml", nameBefore, manager);
                String after = FindTypeInSourceExpresion2("source_data2.xml", nameAfter, manager);
                List<String> list = new List<string>();
                list.Add(before);
                list.Add(after);
                return list;
            }
            else
            {
                var temp = funcElement.Descendants();
                String before = FindTypeInSourceExpresion1("source_data1.xml", nameBefore, manager, temp.ElementAt(0).Value);
                String after = FindTypeInSourceExpresion1("source_data2.xml", nameAfter, manager, temp.ElementAt(1).Value);
                if(before==null || after==null )
                {
                    before = FindTypeInSourceExpresion2("source_data1.xml", nameBefore, manager);
                    after = FindTypeInSourceExpresion2("source_data2.xml", nameAfter, manager);
                }
                List<String> list = new List<string>();
                list.Add(before);
                list.Add(after);
                return list;
            }
        }

        // Sluzi na najdenie typu pola v zdrojovom xml na zaklade id (ked je pole deklarovane ako premenna)
        private String FindTypeInSourceDeclStmt(String fileName, String id, XmlNamespaceManager manager)
        {
            // Nacita obsah zdrojoveho xml suboru do objektu typu xmlDocument
            /*XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();*/

            // Ziskanie prislusneho navigatora podla fileName
            XPathNavigator navigator = GetNavigator(fileName);
            
            XPathNodeIterator nodes = navigator.Select("//base:decl_stmt[@id='" + id + "']/base:decl[1]/base:type", manager);
            nodes.MoveNext();

            // Vracia hodnotu prveho dietata elementu decl_stmt - typ daneho pola
            return nodes.Current.Value;
        }

        // Sluzi na najdenie typu pola v zdrojovom xml na zaklade id (ked je pole parametrom funkcie)
        private String FindTypeInSourceParam(String fileName, String id, XmlNamespaceManager manager)
        {
            // Nacita obsah zdrojoveho xml suboru do objektu typu xmlDocument
            /*XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();*/

            // Ziskanie prislusneho navigatora podla fileName
            XPathNavigator navigator = GetNavigator(fileName);

            XPathNodeIterator nodes = navigator.Select("//base:param[@id='" + id + "']/base:decl[1]/base:type", manager);
            nodes.MoveNext();

            // Vracia typ pola ako parametra funkcie
            return nodes.Current.Value;
        }

        // Sluzi na najdenie nazvu premennej pri zmene indexacie pri deklaracii
        private String FindNameInSource(String fileName, String id, XmlNamespaceManager manager)
        {
            // Nacita zdrojove xml do objektu typu XmlDocument
            /*XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();*/

            // Ziskanie prislusneho navigatora podla fileName
            XPathNavigator navigator = GetNavigator(fileName);

            // Najde deklaraciu pola kde atribut id mena je zhodny s danym id
            XPathNodeIterator nodes = navigator.Select("//base:decl/base:name[@id='" + id + "']", manager);
            nodes.MoveNext();

            // Vrati nazov daneho pola
            return nodes.Current.Value;
        }

        // Sluzi na najdenie nazvu pola pri zmene indexacie pri pouziti v programe
        private String FindNameInSourceExpresion(String fileName, String id, XmlNamespaceManager manager)
        {
            // Nacita obsah zdrojoveho xml do objektu XmlDocument
            /*XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();*/

            // Ziskanie prislusneho navigatora podla fileName
            XPathNavigator navigator = GetNavigator(fileName);

            // Najde nazov pola na zaklade id
            XPathNodeIterator nodes = navigator.Select("//base:expr/base:name[@id='" + id + "']", manager);
            nodes.MoveNext();
            if (nodes.Count > 0)
                // Vracia nazov daneho pola 
                return nodes.Current.Value;
            else
                return null;
        }

        // Sluzi na najdenie typu lokalnej premennej pri pouziti v programe
        private String FindTypeInSourceExpresion1(String fileName, String name, XmlNamespaceManager manager, String func)
        {
            /*// Nacita obsah zdrojoveho xml do objektu XmlDocument
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();*/

            // Ziskanie prislusneho navigatora podla fileName
            XPathNavigator navigator = GetNavigator(fileName);

            // Najde typ premennej na zaklade jej mena a mena nadradenej funkcie
            XPathNodeIterator nodes = navigator.Select("//base:function[base:name='"+func+"']//base:decl_stmt/base:decl[base:name/base:name='" + name + "']/base:type", manager);
            nodes.MoveNext();
            if (nodes.Count == 0)
                return null;

            // Vracia typ premennej 
            return nodes.Current.Value;
        }

        // Sluzi na najdenie typu globalnej premennej pri pouziti v programe
        private String FindTypeInSourceExpresion2(String fileName, String name, XmlNamespaceManager manager)
        {
            // Nacita obsah zdrojoveho xml do objektu XmlDocument
            /*XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();*/

            // Ziskanie prislusneho navigatora podla fileName
            XPathNavigator navigator = GetNavigator(fileName);

            // Najde typ premennej na zaklade jej mena
            XPathNodeIterator nodes = navigator.Select("//base:decl_stmt/base:decl[base:name/base:name='" + name + "']/base:type", manager);
            nodes.MoveNext();
            if (nodes.Count == 0)
                return null;

            // Vracia typ premennej 
            return nodes.Current.Value;
        }

        // Sluzi na zapisanie identifikovanej zmeny v indexovani pola pri jeho deklaracii do vystupneho xml
        public void WriteActionArrayDeclModification(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu 
            List<String> list = FindPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Nazov premennej
            List<String> names = GetName(manager, navigator.Clone());
            List<String> types = GetType(manager, navigator.Clone());

            if (!names[0].Contains('[') || !names[1].Contains('['))
                return;

            // Zistujem v ktorej funkcii je to vnorene
            XElement FunctionElement = GetFunctionNameElement(navigator.Clone());

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Vytvori novy XElement a zapise ho do vystupneho xml
            XElement my_element = new XElement("action",
                    new XElement("name", "array_indexing"),
                    new XElement("type", "declaration"),
                    FunctionElement,
                    new XElement("variable",
                        new XElement("name",
                            new XElement("before", names.ElementAt(0)),
                            new XElement("after", names.ElementAt(1))),
                        new XElement("type",
                            new XElement("before", types.ElementAt(0)),
                            new XElement("after", types.ElementAt(1)))),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Sluzi na zapis identifikovanej zmeny v indexovani pola do vystupneho xml
        public void writeActionArrayIndexModification(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistujem poziciu
            List<String> list = FindPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Nazov premennej
            List<String> names = GetNameExpresion(manager, navigator.Clone());

            if (names == null || !names[0].Contains('[') || !names[1].Contains('['))
                return;

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = GetFunctionNameElement(navigator.Clone());
            
            // Typ pola
            List<String> types = GetTypeExpression(manager, names, functionElement);

            // Ak v skutocnosti nedoslo k zmene indexovania tak konci
            if (names.ElementAt(0) == names.ElementAt(1))
                 return;

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");
         
            // Vytvori novy XElement a zapise ho do vystupneho xml
            XElement my_element = new XElement("action",
                    new XElement("name", "array_indexing"),
                    new XElement("type", "expression"),
                    functionElement,
                    new XElement("variable",
                        new XElement("name",
                            new XElement("before", names.ElementAt(0)),
                            new XElement("after", names.ElementAt(1))),
                        new XElement("type",
                            new XElement("before", types.ElementAt(0)),
                            new XElement("after", types.ElementAt(1)))),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Sluzi na inicializaciu dokumntov 1. a 2. verzie xml suborov pre dopytovanie
        private void InitGlobalDocuments()
        {
            // Nacita obsah 1. zdrojoveho xml do objektu XmlDocument
            _doc1Global = new XmlDocument();
            _doc1Global.Load("source_data1.xml");

            // Inicializuje xPathDoc1Global pre dopytovanie nad 1. xml
            string xmlcontents = _doc1Global.InnerXml;
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            _xPathDoc1Global = new XPathDocument(reader);

            // Nacita obsah 1. zdrojoveho xml do objektu XmlDocument
            _doc2Global = new XmlDocument();
            _doc2Global.Load("source_data2.xml");

            // Inicializuje xPathDoc1Global pre dopytovanie nad 1. xml
            xmlcontents = _doc2Global.InnerXml;
            reader = XmlReader.Create(new StringReader(xmlcontents));
            _xPathDoc2Global = new XPathDocument(reader);
        }

        // Vytvara navigator pre dopytovanie nad zdrojovymi xml subormi
        private XPathNavigator GetNavigator(String fileName)
        {
            if (fileName == "source_data1.xml")
                return _xPathDoc1Global.CreateNavigator();
            else if (fileName == "source_data2.xml")
                return _xPathDoc2Global.CreateNavigator();
            else
                return null;
        }

        // Hlada zmenu indexovania v poliach (v deklaraciach aj pri pouziti v programe)
        public void FindArrayIndexingModification(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Nacita obsah zdrojovych xml suborov do premennych triedy (doc1Global,doc2Global,xPathDoc1Global,xPathDoc2Global)
            InitGlobalDocuments();

            // Najde elementy poli v ktorych nastala zmena v indexovani pri deklaracii
            XPathNodeIterator nodes = navigator.Select("//base:decl_stmt[not(@diff:status='removed') and not(@diff:status='added')]/base:decl[@diff:status]/base:name[@diff:status='below' and base:index/@diff:status]"/*and (base:index/@diff:status='modified'" 
                +" or base:index/@diff:status='below')]"*/, manager);

            // Prechadza vsetky najdene elementy a zapisuje najdene zmeny do vystupneho xml suboru
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                WriteActionArrayDeclModification(manager, nodesNavigator);
            }


            // Najde elementy poli v ktorych nastala zmena v indexovani pri pouziti v programe
            nodes = navigator.Select("//base:expr[@diff:status]/base:name[@diff:status='below' and base:index/@diff:status]"/* and (base:index/@diff:status='modified'"
                + " or base:index/@diff:status='below')]"*/, manager);

            // Prechadza vsetky najdene elementy a zapisuje najdene zmeny do vystupneho xml suboru
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;
                writeActionArrayIndexModification(manager, nodesNavigator);
            }
            
        }
    }
}
