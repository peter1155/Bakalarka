﻿using Microsoft.XmlDiffPatch;
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
    class CallFunctionChangeActivity
    {
        public List<String> findPosition(String elementName, XPathNavigator navigator)
        {
            while (navigator != null && String.Compare(navigator.Name, elementName) != 0)
            {
                navigator.MoveToParent();
            }

            // Ziskam pristup k detom elementu call
            XPathNodeIterator callChildren =
                navigator.SelectChildren(XPathNodeType.Element);

            // Ziskam pristup k name kde sa nachadza prislusny attribut
            while (callChildren.MoveNext() && callChildren.Current.Name != "name") ;

            XPathNavigator callNameNav = callChildren.Current;
            String line = callNameNav.GetAttribute("line", "http://www.sdml.info/srcML/position");
            String column = callNameNav.GetAttribute("column", "http://www.sdml.info/srcML/position");

            List<String> list = new List<string>();
            list.Add(line);
            list.Add(column);
            return list;
        }

        private String findInSource(String id, String fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            string xmlcontents = doc.InnerXml;
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

            String temp = "//base:call[@id='" + id + "']/base:argument_list/base:argument";
            XPathNodeIterator nodes = navigator.Select(temp, manager);
            int counter = 0;
            temp = "";
            while (nodes.MoveNext())
            {
                if (counter > 0)
                    temp += ",";
                temp += nodes.Current.Value;
                /*if (counter == 0)
                    temp += "~~";*/
                counter++;
            }
            return temp;
        }

        private XElement getFunctionNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "function") != 0)
            {
                navigator.MoveToParent();
            }

            if (navigator.Name == "unit")
                return new XElement("errorInSource");

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

        private XElement getFunctionCallNameElement(XPathNavigator navigator)
        {
            // Najde element function
            while (String.Compare(navigator.Name, "unit") != 0 && String.Compare(navigator.Name, "call") != 0)
            {
                navigator.MoveToParent();
            }

            if (navigator.Name == "unit")
                return new XElement("errorInSource");

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

        private List<String> manualParse(XPathNavigator navigator)
        {
            
            String parameters_before = null;
            String parameters_after = null;
            String[] beforeAfterValues = null;
            char[] delimiters = { '~' };
            // Select arguments from argument list
            XPathNodeIterator call_children = navigator.SelectChildren(XPathNodeType.Element);
            XPathNavigator call_child = null;

            while (call_children.MoveNext())
            {
                if (String.Compare(call_children.Current.Name, "argument_list") == 0)
                {
                    call_child = call_children.Current;
                    break;
                }
            }

            XPathNodeIterator arguments = call_child.SelectChildren(XPathNodeType.Element);

            // Prechadzame vsetkymi argumentmi funkcie 
            int in_after_counter = 0;
            int out_after_counter = 0;

            int in_before_counter = 0;
            int out_before_counter = 0;


            while (arguments.MoveNext())
            {
                XPathNodeIterator expresionIterator = arguments.Current.SelectChildren(XPathNodeType.Element);
                expresionIterator.MoveNext();
                XPathNodeIterator parameterIterator = expresionIterator.Current.SelectChildren(XPathNodeType.Element);

                in_after_counter = 0;
                in_before_counter = 0;

                while (parameterIterator.MoveNext())
                {
                    XPathNavigator parameter = parameterIterator.Current;
                    String temp = parameter.GetAttribute("status", "http://www.via.ecp.fr/~remi/soft/xml/xmldiff");
                    String tempValue = parameter.Value;

                    if (String.Compare(temp, "modified") == 0)
                    {
                        if (out_after_counter != 0 && in_after_counter == 0)
                            parameters_after += ",";
                        if (out_before_counter != 0 && in_before_counter == 0)
                            parameters_before += ",";
                        
                        out_after_counter++;
                        in_after_counter++;

                        out_before_counter++;
                        in_before_counter++;

                        beforeAfterValues = tempValue.Split(delimiters);
                        parameters_before += beforeAfterValues[0];
                        parameters_after += beforeAfterValues[1];
                    }
                    else if (String.Compare(temp, "added") == 0)
                    {
                        if (out_after_counter != 0 && in_after_counter == 0)
                            parameters_after += ",";
                        out_after_counter++;
                        in_after_counter++;
                        parameters_after += tempValue;
                    }
                    else if (String.Compare(temp, "removed") == 0)
                    {
                        if (out_before_counter != 0 && in_before_counter == 0)
                            parameters_before += ",";
                        out_before_counter++;
                        in_before_counter++;
                        parameters_before += tempValue;
                    }
                    else if (String.Compare(temp, "below") == 0)
                    {

                        // parmeterObject.type.Add("removed");
                    }
                    else
                    {
                        if (out_after_counter != 0 && in_after_counter == 0)
                            parameters_after += ",";
                        if (out_before_counter != 0 && in_before_counter == 0)
                            parameters_before += ",";

                        out_after_counter++;
                        in_after_counter++;

                        out_before_counter++;
                        in_before_counter++;

                        parameters_before += tempValue;
                        parameters_after += tempValue;
                    }
                }

            }
            
            List<String> list = new List<String>();
            list.Add(parameters_before);
            list.Add(parameters_after);
            return list;
        }

        public void writeActionCallModified(XPathNavigator navigator)
        {

            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zistujem poziciu 
            List<String> position = findPosition("call", navigator.Clone());
            String line = position.ElementAt(0);
            String column = position.ElementAt(1);

            // Zistujem meno volanej funkcie
            XElement functionCallElement = getFunctionCallNameElement(navigator.Clone());

            // Zistujem id elementu call 
            XPathNavigator tempNavigator2 = navigator.Clone();
            while (tempNavigator2 != null && String.Compare(tempNavigator2.Name, "call") != 0)
            {
                tempNavigator2.MoveToParent();
            }

            String id = tempNavigator2.GetAttribute("id", "");
            String parametersBefore = findInSource(id, "source_data1.xml");
            String parametersAfter = findInSource(id, "source_data2.xml");
            if(String.Compare(id,"") == 0)
            {
                var list = manualParse(tempNavigator2.Clone());
                parametersBefore = list.ElementAt(0);
                parametersAfter = list.ElementAt(1);
            }
            
            String modification_type = "";

            if (String.Compare(parametersBefore, parametersAfter) != 0)
                modification_type = "parameter";
            
            XElement function_parameters = new XElement("function_parameters");
            function_parameters.Add(new XElement("parameters_before", parametersBefore),
                new XElement("parameters_after", parametersAfter));

            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");
            
            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action",
                    new XElement("name", "function_call"),
                    new XElement("diff_type", "modified"),
                    new XElement("modification_type",modification_type),
                    functionElement,
                    new XElement("function_call",
                    functionCallElement,function_parameters),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void writeActionCallAdded(XPathNavigator navigator)
        {
            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zistujem poziciu 
            List<String> position = findPosition("call", navigator.Clone());
            String line = position.ElementAt(0);
            String column = position.ElementAt(1);

            // Zistujem meno volanej funkcie
            XElement functionCallElement = getFunctionCallNameElement(navigator.Clone());

            // Zistujem poziciu funkci
            XPathNavigator tempNavigator2 = navigator.Clone();
            while (tempNavigator2 != null && String.Compare(tempNavigator2.Name, "call") != 0)
            {
                tempNavigator2.MoveToParent();
            }

            String id = tempNavigator2.GetAttribute("id", "");
            String parametersAfter;
            var list = manualParse(tempNavigator2.Clone());
            parametersAfter = list.ElementAt(1);
            XElement function_parameters = new XElement("function_parameters",parametersAfter);
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action", new XAttribute("id", id),
                    new XElement("name", "function_call"),
                    new XElement("diff_type", "added"),
                    functionElement,
                    new XElement("function_call",
                    functionCallElement, function_parameters),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public void writeActionCallDeleted(XPathNavigator navigator)
        {
            // Zistujem v ktorej funkcii je to vnorene
            XElement functionElement = getFunctionNameElement(navigator.Clone());

            // Zistujem poziciu 
            List<String> position = findPosition("call", navigator.Clone());
            String line = position.ElementAt(0);
            String column = position.ElementAt(1);

            // Zistujem meno volanej funkcie
            XElement functionCallElement = getFunctionCallNameElement(navigator.Clone());

            // Zistujem poziciu funkcie scanf
            XPathNavigator tempNavigator2 = navigator.Clone();
            while (tempNavigator2 != null && String.Compare(tempNavigator2.Name, "call") != 0)
            {
                tempNavigator2.MoveToParent();
            }

            String id = tempNavigator2.GetAttribute("id", "");
            String parametersBefore;            
            var list = manualParse(tempNavigator2.Clone());
            parametersBefore = list.ElementAt(0);
           
            XElement function_parameters = new XElement("function_parameters", parametersBefore);

            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");

            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("action", new XAttribute("id", id),
                    new XElement("name", "function_call"),
                    new XElement("diff_type", "deleted"),
                    functionElement,
                    new XElement("function_call",
                    functionCallElement, function_parameters),
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        public List<String> getFunctionNames(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            List<String> functionList = new List<string>();
            XPathNodeIterator nodes = navigator.Select("//base:function/base:name", manager);
            while(nodes.MoveNext())
            {
                XPathNavigator nameNode = nodes.Current;
                String temp = nameNode.Value;
                
                // Ak doslo k modifikacii nazvu treba prehladavat nazov pred po modifikacii aj zluceny
                if (temp.Contains("~"))
                {
                    string[] delimiters = { "~" };
                    string[] beforeAfterValues = temp.Split(delimiters, StringSplitOptions.None); // Rozsekam zmenu na casti
                    functionList.Add(beforeAfterValues[0]);
                    functionList.Add(beforeAfterValues[1]);
                }
                else 
                    functionList.Add(nameNode.Value);
            }
            
            return functionList;
        }
        
        public void findChangedFunctionCalls(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            List<String> functionList = getFunctionNames(manager, navigator);
           
            // Najskor identifikujeme modifikovane volania funkcii
            
            XPathNodeIterator nodes = navigator.Select("//base:call[@diff:status='below']/base:name"
                + " | //base:call[@similarity!='1']/base:name", manager);
            List<XPathNavigator> modifiedCalls =  new List<XPathNavigator>();

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                for(int i=0; i< functionList.Count;i++)
                {
                    String temp  = currentNode.Value;
                    if(temp == functionList.ElementAt(i))
                    {
                        modifiedCalls.Add(currentNode.Clone());
                        break;
                    }
                }
            }

            for(int i=0;i<modifiedCalls.Count;i++)
            {
                writeActionCallModified(modifiedCalls.ElementAt(i));
            }

            // Teraz to iste pre pridane volania
            nodes = navigator.Select("//base:call[@diff:status='added']/base:name", manager);
            modifiedCalls = new List<XPathNavigator>();

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                for (int i = 0; i < functionList.Count; i++)
                {
                    String temp = currentNode.Value;
                    if (temp == functionList.ElementAt(i))
                    {
                        modifiedCalls.Add(currentNode.Clone());
                        break;
                    }
                }
            }

            for (int i = 0; i < modifiedCalls.Count; i++)
            {
                writeActionCallAdded(modifiedCalls.ElementAt(i));
            }

            // Teraz to iste pre vymazane volania
            nodes = navigator.Select("//base:call[@diff:status='removed']/base:name", manager);
            modifiedCalls = new List<XPathNavigator>();

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                for (int i = 0; i < functionList.Count; i++)
                {
                    String temp = currentNode.Value;
                    if (temp == functionList.ElementAt(i))
                    {
                        modifiedCalls.Add(currentNode.Clone());
                        break;
                    }
                }
            }

            for (int i = 0; i < modifiedCalls.Count; i++)
            {
                writeActionCallDeleted(modifiedCalls.ElementAt(i));
            }
        }
    }
}
