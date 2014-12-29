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
    class AddFunctionActivity
    {
        
        public void writeActionScan(XPathNavigator navigator)
        {
            String line = null;
            String column = null;
            String type = null;
            String name = null;
            XElement parameter_list2 = new XElement("parameter_list");

            XPathNodeIterator function_children = navigator.SelectChildren(XPathNodeType.Element);
            while(function_children.MoveNext())
            {
                XPathNavigator child = function_children.Current;
                if(String.Compare(child.Name,"type") == 0)
                {
                    type = child.Value;
                }
                else if(String.Compare(child.Name,"name") == 0)
                {
                    name = child.Value;
                    line = child.GetAttribute("line", "http://www.sdml.info/srcML/position");
                    column = child.GetAttribute("column", "http://www.sdml.info/srcML/position");
                }
                else if(String.Compare(child.Name,"parameter_list") == 0)
                {
                    XPathNodeIterator param_list = child.SelectChildren(XPathNodeType.Element);
                    // Prechadzam jednotlivymi parametrami 
                    while (param_list.MoveNext())
                    {
                        XPathNavigator param = param_list.Current;

                        // Ziskam pristup k detom param teda declaretion
                        XPathNodeIterator param_children = param.SelectChildren(XPathNodeType.Element);
                        param_children.MoveNext();
                        XPathNavigator param_child = param_children.Current;

                        // Nakoniec ziskam pristup k detom declaration 
                        XElement parameter = new XElement("parameter");
                        XPathNodeIterator decl_children = param_child.SelectChildren(XPathNodeType.Element);
                       
                        while (decl_children.MoveNext())
                        {
                            XPathNavigator decl_child = decl_children.Current;
                            if (String.Compare(decl_child.Name, "type") == 0)
                                parameter.Add(new XElement("type", decl_child.Value));
                            if (String.Compare(decl_child.Name, "name") == 0)
                            {
                                // Meno moze obsahovat indexy preto treba prechadzat postupne
                                XPathNodeIterator name_children = decl_child.SelectChildren(XPathNodeType.Element);
                                XElement nameElement = new XElement("name");
                                while(name_children.MoveNext())
                                {
                                    XPathNavigator name_child = name_children.Current;
                                    if (String.Compare(name_child.Name, "name") == 0)
                                    {
                                        nameElement.Add(new XElement("name", name_child.Value));
                                    }
                                    if (String.Compare(name_child.Name, "index") == 0)
                                    {
                                        nameElement.Add(new XElement("index", name_child.Value));
                                    }
                                }
                                parameter.Add(nameElement);
                            }
                        }
                        parameter_list2.Add(parameter);
                    }
                }
            }
            
            // Zapisem akciu do xml suboru
            XDocument xdoc = XDocument.Load("RecordedActions.xml");
            
            // Pridana funkcia meno,typ,riadok,stlpec,parameter list
            XElement my_element = new XElement("acion",
                    new XElement("name", "function_added"),
                //new XElement("diffType", diffType),
                    new XElement("type", type),
                    new XElement("name", name),
                    parameter_list2,
                    new XElement("line", line),
                    new XElement("column", column)
                    );
            xdoc.Root.Add(my_element);
            xdoc.Save("RecordedActions.xml");
        }

        // Urobim dopyt nad difference XML dokumentom a vyhladam volania funkcie printf, kde nastala nejaka zmena
        public void findAddedFunctions(XmlNamespaceManager manager, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select("//base:function[@diff:status='added']", manager);

            while (nodes.MoveNext())
            {
                XPathNavigator currentNode = nodes.Current.Clone();
                writeActionScan(currentNode);
            }
        }
    }
}
