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
using System.Text.RegularExpressions;

namespace ConsoleApplication12
{
    class CommentActivity
    {
        // Nazov suboru so zdrojovym kodom
        private String fileName1;

        // Nazov suboru so zdrojovym kodom
        private String fileName2;
        
        // V konstruktore sa do atributov triedy predaju nazvy suborov so zdrojovym kodom
        public CommentActivity(String file1, String file2)
        {
            fileName1 = file1+".c";
            fileName2 = file2+".c";
        }

        // Ziska element s nazvom funkcie v ktorej je dany komentar vnoreny alebo
        // zisti ze sa jedna o komentar na globalnej urovni
        private XElement getFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
            }

            // Ak sa nenasiel element function jedna sa o element na globalnej urovni
            if (navigator.Name == "unit")
            {
                return new XElement("global_comment");
            }

            // Ziska deti elementu function
            XPathNodeIterator function_childeren = navigator.SelectChildren(XPathNodeType.Element);

            // Najde elemnt name elementu function 
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

            // Ak doslo k modifikacii nazvu funkcie treba to osetrit
            if (funcName.Contains("~"))
            {
                char[] del = { '~' };
                string[] beforeAfterValues = funcName.Split(del);
                functionElement = new XElement("function_name",
                    new XElement("before", beforeAfterValues[0]),
                    new XElement("after", beforeAfterValues[1]));
            }
            else if (funcNames.Count > 1)
            {
                functionElement = new XElement("function_name",
                    new XElement("before", funcNames.ElementAt(1)),
                    new XElement("after", funcNames.ElementAt(0)));
            }
            else
            {
                functionElement = new XElement("function_name",
                    new XElement("before", funcName),
                    new XElement("after", funcName));
            }
            return functionElement;
        }

        // Ziska poziciu elementu vo formate riadok, stlpec reprezentovanu string listom  pre 
        // aktualny XPathNavigator
        public List<String> findPosition(XPathNavigator navigator)
        {
            String line = navigator.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = navigator.GetAttribute("column", "http://www.sdml.info/srcML/position");
            List<String> list = new List<string>();
            list.Add(line);
            list.Add(column);
            return list;
        }

        // Zapise identifikovane pridanie komentara do vystupneho xml suboru
        public void writeActionCommentAdded(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistuje poziciu 
            List<String> list = findPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistuje v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            String tempSource = navigator.Value;
            tempSource = tempSource.Replace("\n", "");
            tempSource = tempSource.Replace("\r", "");
            tempSource = tempSource.Replace(" ", "");
            tempSource = tempSource.Replace("//", "");
            tempSource = tempSource.Replace("/*", "");
            tempSource = tempSource.Replace("*/", "");
               
            if (tempSource == "")
                return;

            // Zapise akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
                    new XElement("name", "comment"),
                    new XElement("type", "added"),
                    functionElement,
                    new XElement("commented_source", navigator.Value),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Zapise identifikovane zmazanie komentara do vystupneho xml suboru
        public void writeActionCommentRemoved(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Zistuje poziciu 
            List<String> list = findPosition(navigator.Clone());
            String line = list.ElementAt(0);
            String column = list.ElementAt(1);

            // Zistuje v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            String tempSource = navigator.Value;
            tempSource = tempSource.Replace("\n", "");
            tempSource = tempSource.Replace("\r", "");
            tempSource = tempSource.Replace(" ", "");

            if (tempSource == "")
                return;

            // Zapise akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
                    new XElement("name", "comment"),
                    new XElement("type", "deleted"),
                    functionElement,
                    new XElement("commented_source", navigator.Value),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }
        
        /*
        private String prcessXmlFile(String fileName)
        {
            XmlDocument doc1 = new XmlDocument();
            doc1.Load(fileName);
            attributeDelete(doc1.ChildNodes);
            return doc1.InnerXml;
        }

        private void attributeDelete(XmlNodeList list1)
        {
            foreach (XmlNode listNode in list1)
                if (listNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement node = (XmlElement)listNode;
                    
                    node.RemoveAllAttributes();
                    
                    if (listNode.HasChildNodes)
                        attributeDelete(listNode.ChildNodes);
                }
        }

        private String removeNamespaces(String temp)
        {
            temp = temp.Replace("xmlns=\"http://www.sdml.info/srcML/src\"", "");
            temp = temp.Replace("xmlns:cpp=\"http://www.sdml.info/srcML/cpp\"", "");
            temp = temp.Replace("xmlns:lit=\"http://www.sdml.info/srcML/literal\"", "");
            temp = temp.Replace("xmlns:op=\"http://www.sdml.info/srcML/operator\"", "");
            temp = temp.Replace("xmlns:type=\"http://www.sdml.info/srcML/modifier\"", "");
            temp = temp.Replace("xmlns:pos=\"http://www.sdml.info/srcML/position\"", "");
            temp = temp.Replace("xmlns:diff=\"http://www.via.ecp.fr/~remi/soft/xml/xmldiff\"", "");
            temp = Regex.Replace(temp, @"\s+", "");
            return temp;
        }*/

        // Cita subor po riadkocha vracia jeho obsah ako string
        private string readFile(string fileName)
        {
            string fileContent = "";
            string line;

            // Read the file and display it line by line.
            System.IO.StreamReader file =
               new System.IO.StreamReader(fileName);
            while ((line = file.ReadLine()) != null)
            {
                fileContent += line;
            }
            file.Close();
            return fileContent;
        }

        // vracia textovy obsah bez znaciek z xml elementu
        private string getTextFromNode(XPathNavigator navigator)
        {
            return navigator.TypedValue.ToString();
        }
        

        // Najde pridane a vymazane komentare 
        public void findCanaceledOutput(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            // Sluzi na ulozenie vsetkoho zmazaneho kodu 
            List<string> removedSourceList = new List<string>();
            
            // Sluzi na ulozenie v+setkeho pridaneho kodu 
            List<string> addedSourceList = new List<string>();

            // Najde zmazany kod
            XPathNodeIterator deletedSource = navigator.Select("//*[@diff:status='removed' and not(descendant-or-self::*[local-name() = 'comment'])]", manager);

            // Najde zmazany kod
            XPathNodeIterator addedSource = navigator.Select("//*[@diff:status='added' and not(descendant-or-self::*[local-name() = 'comment'])]", manager);

            // Prechadza vsetkymi zmazanymi elementmi a ziskava z nich len textovy obsah bez xml znaciek
            while (deletedSource.MoveNext())
            {
                XPathNavigator deletedElement = deletedSource.Current;
                String str = getTextFromNode(deletedElement);
                str = str.Replace("\t", "");
                str = str.Replace("\n", "");
                str = str.Replace(" ", "");
                removedSourceList.Add(str);
            }

            // Prechadza vsetkymi pridanymi elementmi a ziskava z nich len textovy obsah bez xml znaciek
            while (addedSource.MoveNext())
            {
                XPathNavigator addedElement = addedSource.Current;
                String str = getTextFromNode(addedElement);
                str = str.Replace("\t", "");
                str = str.Replace("\n", "");
                str = str.Replace(" ", "");
                addedSourceList.Add(str);
            }

            // Najde vsetky pridane komentare
            XPathNodeIterator nodesCommentAdded = navigator.Select("//base:comment[@diff:status='added']", manager);

            // Najde vsetky odobrane komentare
            XPathNodeIterator nodesCommentRemoved = navigator.Select("//base:comment[@diff:status='removed']", manager);
            
            // Prejdi cez vsetky pridane komentare
            while (nodesCommentAdded.MoveNext())
            {
                XPathNavigator nodesNavigator = nodesCommentAdded.Current;

                String temp = nodesNavigator.Value;
                temp = temp.Replace("//","");
                temp = temp.Replace("/*", "");
                temp = temp.Replace("*/", "");
                // Vymaz whitespaces
                temp = temp.Replace("\t", "");
                temp = temp.Replace("\n", "");
                temp = temp.Replace(" ", "");

                // Ak sa v zmazanom kode nachadza obsah komentara potom identifikuj zakomentovanie casti kodu
                foreach(String sourceString in removedSourceList)
                {
                    //if (sourceString == temp)
                    if(sourceString.Contains(temp))
                    {
                        writeActionCommentAdded(manager, nodesNavigator);
                        break;
                    }
                }
            }

            // Prejdi cez vsetky vymazane komentare
            while (nodesCommentRemoved.MoveNext())
            {
                XPathNavigator nodesNavigator = nodesCommentRemoved.Current;

                String temp = nodesNavigator.Value;
                temp = temp.Replace("//", "");
                temp = temp.Replace("/*", "");
                temp = temp.Replace("*/", "");

                // Vymaz whitespaces
                temp = temp.Replace("\t", "");
                temp = temp.Replace("\n", "");
                temp = temp.Replace(" ", "");

                // Ak sa v pridanom kode nachadza obsah komentara potom identifikuj odkomentovanie casti kodu
                foreach (String sourceString in addedSourceList)
                {
                    //if (sourceString == temp)
                    if(sourceString.Contains(temp))
                    {
                        writeActionCommentRemoved(manager, nodesNavigator);
                        break;
                    }
                }
            }

        }
    }
}
