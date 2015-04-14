using ConsoleApplication12.AppOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace ConsoleApplication12.Actions
{
    class MoveDetection
    {
        private void DetectCallFunctionMoves(XmlDocument doc)
        { 
            // Najdi vsetky elementy action ktorych meno je function_call a diff_type je added
            XmlNodeList nodesAdded = doc.SelectNodes("//action[name='function_call' and diff_type='added']");
            
            // Najdi vsetky elementy action ktorych meno je function_call a diff_type je deleted
            XmlNodeList nodesDeleted = doc.SelectNodes("//action[name='function_call' and diff_type='deleted']");
           
            List<String> idList1 = new List<String>();
            List<String> idList2 = new List<String>();

            foreach(XmlNode nodeAdded in nodesAdded)
                foreach(XmlNode nodeDeleted in nodesDeleted)
                {
                    var id1 =  nodeAdded.Attributes.GetNamedItem("id"); 
                    var id2 =  nodeDeleted.Attributes.GetNamedItem("id"); 

                    // Ak sa indexy elementov rovnaju jedna sa o move preto je potrebne jeden s elementov vymazat
                    // a druhemu zmenit diff_type
                    if(id1.Value == id2.Value)
                    {
                        var parent = nodeDeleted.ParentNode;
                        parent.RemoveChild(nodeDeleted);
                        
                        // Pre kazdy identifikovany element zmen diff_type na hodnotu move
                        foreach(XmlNode node in nodeAdded.ChildNodes)
                        {
                            if(node.Name == "diff_type")
                            {
                                node.InnerText = "moved";
                            }
                        }
                    }
                }

            // Vymaz idecka z vysledneho suboru pretoze su dost metuce
            foreach(XmlNode nodeAdded in nodesAdded)
                    nodeAdded.Attributes.RemoveAll();
            foreach(XmlNode nodeDeleted in nodesDeleted)
                    nodeDeleted.Attributes.RemoveAll();
                
        }

        private void DetectCallFunctionMovesFast(XmlDocument doc)
        {
             // Najdi vsetky elementy action ktorych meno je function_call a diff_type je added
            XmlNodeList nodesAdded = doc.SelectNodes("//action[name='function_call' and diff_type='added']");
            
            // Najdi vsetky elementy action ktorych meno je function_call a diff_type je deleted
            XmlNodeList nodesDeleted = doc.SelectNodes("//action[name='function_call' and diff_type='deleted']");
            
            // Najdi vsetky elementy action ktorych meno je function_call a diff_type je deleted
            XmlNode parentNode = doc.SelectSingleNode("//actions");

            List<String> idList1 = new List<String>();
            List<String> idList2 = new List<String>();
            List<int> toDeleteAdded = new List<int>();
            List<int> toDeleteRemoved = new List<int>();


            for (int i = 0; i < nodesAdded.Count; i++)
                for (int j = 0; j < nodesDeleted.Count;j++ )
                {
                    XmlNode funcNameNode1 = nodesAdded[i].SelectSingleNode("function_name");
                    XmlNode funcNameNode2 = nodesDeleted[j].SelectSingleNode("function_name");

                    XmlNode funcCallNode1 = nodesAdded[i].SelectSingleNode("function_call");
                    XmlNode funcCallNode2 = nodesDeleted[j].SelectSingleNode("function_call");

                    // Detekuje zmazanie a pridanie a nasledne vymaze uzly...
                    if (funcCallNode1.InnerText == funcCallNode2.InnerText
                        && funcNameNode1.InnerText == funcNameNode2.InnerText)
                    {
                        toDeleteAdded.Add(i);
                        toDeleteRemoved.Add(j);
                    }
                }

            if (toDeleteAdded.Count > 0)
            {
                toDeleteRemoved = toDeleteRemoved.GroupBy(delete => delete)
                    .Select(delete => delete.First())
                    .OrderByDescending(delete => delete)
                    .ToList();

                toDeleteAdded = toDeleteAdded.GroupBy(added => added)
                    .Select(added => added.First())
                    .OrderByDescending(added => added)
                    .ToList();

                for (int i = 0; i < toDeleteAdded.Count; i++)
                {
                    var deleteNode = nodesAdded[toDeleteAdded[i]];
                    parentNode.RemoveChild(deleteNode);
                }

                for (int i = 0; i < toDeleteRemoved.Count; i++)
                {
                    var deleteNode = nodesDeleted[toDeleteRemoved[i]];
                    parentNode.RemoveChild(deleteNode);
                }

            }

            /*foreach(XmlNode nodeAdded in nodesAdded)
                foreach (XmlNode nodeDeleted in nodesDeleted)
                {
                    XmlNode funcNameNode1 = nodeAdded.SelectSingleNode("//function_name");
                    XmlNode funcNameNode2 = nodeDeleted.SelectSingleNode("//function_name");

                    XmlNode funcCallNode1 = nodeAdded.SelectSingleNode("//function_call");
                    XmlNode funcCallNode2 = nodeDeleted.SelectSingleNode("//function_call");
                    
                    // Detekuje zmazanie a pridanie a nasledne vymaze uzly...
                    if (funcCallNode1.InnerText == funcCallNode2.InnerText
                        && funcNameNode1.InnerText == funcNameNode2.InnerText)
                    {
                        parentNode.RemoveChild(nodeDeleted);
                        parentNode.RemoveChild(nodeAdded);
                    }
                }*/
        }

        // Detekuje presunute elementy postProcessingom suboru so zaznamenanymi cinnostami
        public void detectMoves()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("RecordedActions.xml");
            
            // Detekujem presuny elementov call function
            if(Options.Method == Options.Methods.Complex)
                DetectCallFunctionMoves(doc);
            else
                DetectCallFunctionMovesFast(doc);

            doc.Save("RecordedActions.xml");
            

        }

    }
}
