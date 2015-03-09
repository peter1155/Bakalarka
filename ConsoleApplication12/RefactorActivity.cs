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
using System.Xml.Schema;
using System.Xml.XPath;

namespace ConsoleApplication12
{
    class RefactorActivity
    {
        // Ziska element s pridanou funkciu
        private XElement GetFunctionAddedNode(XPathNavigator navigator)
        {
            XElement addFuncElement = new XElement("added_function");

            var temp = navigator.SelectChildren(XPathNodeType.Element);
            while(temp.MoveNext())
            {
                if(temp.Current.Name != "name")
                {
                    addFuncElement.Add(XElement.Parse(temp.Current.OuterXml));
                }
            }
            return addFuncElement;
        }

        // Ziska element s pridanym volanim funkcie
        private XElement GetFunctionCallNode(XPathNavigator navigator)
        {
            XElement callFuncElement = new XElement("call_function");

            var temp = navigator.SelectChildren(XPathNodeType.Element);
            while (temp.MoveNext())
            {
                if (temp.Current.Name != "name" && temp.Current.Name != "diff_type")
                {
                    callFuncElement.Add(XElement.Parse(temp.Current.OuterXml));
                }
            }
            return callFuncElement;
        }

        // Zapise identifikovany refactoring do vystupneho xml suboru
        public void WriteRefactorActivity(XPathNavigator callNavigator, XPathNavigator navigator)
        {
            XElement addFuncElement = GetFunctionAddedNode(navigator);
            XElement callFuncElement = GetFunctionCallNode(callNavigator);
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            XElement my_element = new XElement("action",
                    new XElement("name", "source_code_refactoring"),
                    addFuncElement,
                    callFuncElement);
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Nacita subor so zaznamenanymi aktivitami vracia XPathNavigator pre dany subor
        private XPathNavigator LoadIdentifiedActivities()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("RecordedActions.xml");
           
            string xmlcontents = doc.InnerXml;
           
            XmlReader reader = XmlReader.Create(new StringReader(xmlcontents));
            XPathDocument document_xpath = new XPathDocument(reader);
            XPathNavigator navigator = document_xpath.CreateNavigator();
            return navigator;
            
        }

        // Najde refactoring a zapise danu zmenu do vystupneho xml suboru
        public void FindRefactoring()
        {
            XPathNavigator navigator = LoadIdentifiedActivities();

            // Najde vsetky pridane funkcie
            XPathNodeIterator nodes = navigator.Select("//action[name='function_added']");
           
            while (nodes.MoveNext())
            {
                XPathNavigator nodesNavigator = nodes.Current;

                XPathNavigator nameNavigator = nodesNavigator.SelectSingleNode("function_name");

                // Hlada aktivitu pridanie volania pridanej funkcie
                XPathNavigator funcCall = navigator.SelectSingleNode("//action[name='function_call' and diff_type='added' and "
                + "(function_call/function_name/before='" + nameNavigator.Value + "' or function_call/function_name/after='" + nameNavigator.Value + "')]");
                
                // Ak sa tam vyskytuje volanie pridanej funkcie zapis aktivitu
                if(funcCall != null)
                {
                    WriteRefactorActivity(funcCall, nodesNavigator);
                }
            }

            
        }
    }
}
