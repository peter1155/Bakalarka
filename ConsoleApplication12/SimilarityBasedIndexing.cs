using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ConsoleApplication12
{
    class SimilarityBaseIndexing
    {
        // Sluzi na indexovanie podobnych lementov
        static Int64 id = 0;

        // Aktualna hlbka  vnorenia v ramci rekurzie
        private int depth = 0;

        // Sluzi na priradenie pomocnych indexov vsetkym elementom
        private Int64 tempIndex = 0;

        // Udavaju velkost resultTable
        private Int64 tempId1 = 0;
        private Int64 tempId2 = 0;
        // Odpametavam si vysledky aby som zabranil opakovanym vypoctom
        private float[,] resultTable;

        // Vracia maximum dvoch cisel
        private int max(int a, int b)
        {
            return (a > b) ? a : b;
        }

        // Longest common subsequence
        private int LCS(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            {
                return 0;
            }

            int[,] table = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                table[i, 0] = 0;
            for (int i = 0; i <= s2.Length; i++)
                table[0, i] = 0;

            for (int i = 1; i <= s1.Length; i++)
                for (int j = 1; j <= s2.Length; j++)
                {
                    if (s1[i - 1] == s2[j - 1])
                        table[i, j] = table[i - 1, j - 1] + 1;
                    else
                    {
                        table[i, j] = max(table[i - 1, j], table[i, j - 1]);
                    }

                }
            return table[s1.Length, s2.Length];
        }

        // Vracia textovu podobnost dvoch stringov
        private float computeStringSimilarity(string str1, string str2)
        {
            int lcs = LCS(str1, str2);
            float avarage = (str1.Length + str2.Length) / 2;
            return lcs / avarage;
        }

        // Sluzi na greedy parovanie dcerskych elementov all to all
        private float greedyMatching(float[,] matrix, XmlNodeList list1, XmlNodeList list2,
            XmlDocument doc1, XmlDocument doc2)
        {
            float max = matrix[0, 0];
            int i1 = 0, i2 = 0;
            float sum = 0;
            int count;
            if (matrix.GetLength(0) > matrix.GetLength(1))
                count = matrix.GetLength(1);
            else
                count = matrix.GetLength(0);

            for (int k = 0; k < count; k++)
            {
                for (int i = 0; i < matrix.GetLength(0); i++)
                    for (int j = 0; j < matrix.GetLength(1); j++)
                    {
                        if (matrix[i, j] > max)
                        {
                            max = matrix[i, j];
                            i1 = i;
                            i2 = j;
                        }
                    }
                sum += max;
                for (int j = 0; j < matrix.GetLength(1); j++)
                    matrix[i1, j] = 0;
                for (int i = 0; i < matrix.GetLength(0); i++)
                    matrix[i, i2] = 0;
            }
            float avarage = (matrix.GetLength(0) + matrix.GetLength(1)) / 2;
            return sum / avarage;
        }

        // Sluzi na pridanie pomocnych indexov
        private void tempIndedxing(XmlDocument doc, XmlNodeList nodes)
        {
            foreach (XmlNode node in nodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlAttribute tempId = doc.CreateAttribute("temp_id");
                    tempId.Value = tempIndex.ToString();
                    node.Attributes.Append(tempId);
                    tempIndex++;
                }
                tempIndedxing(doc, node.ChildNodes);
            }
        }

        // Vracia podobnost subelementov daneho elementu
        private float subelementSimilarity(XmlDocument doc1, XmlDocument doc2, XmlNode root1, XmlNode root2)
        {
            XmlNodeList list1 = root1.ChildNodes;
            XmlNodeList list2 = root2.ChildNodes;
            float[,] matrix = new float[root1.ChildNodes.Count, root2.ChildNodes.Count];

            for (int i = 0; i < root1.ChildNodes.Count; i++)
                for (int j = 0; j < root2.ChildNodes.Count; j++)
                {
                    depth++;
                    matrix[i, j] = computeSimilarity(doc1, doc2, list1.Item(i), list2.Item(j));
                    depth--;
                }

            if (root1.ChildNodes.Count == 0 && root2.ChildNodes.Count == 0)
                return 1;
            if ((root1.ChildNodes.Count == 0 && root2.ChildNodes.Count != 0)
                || (root1.ChildNodes.Count != 0 && root2.ChildNodes.Count == 0))
                return 0;
            return greedyMatching(matrix, list1, list2, doc1, doc2);
        }

        // Vracia podobnost dvoch xml elementov
        public float computeSimilarity(XmlDocument doc1, XmlDocument doc2, XmlNode root1, XmlNode root2)
        {
            XmlNode tempIdAtrib;
            XmlNode tempIdAtrib2;
            int tempIdValue1 = -1;
            int tempIdValue2 = -1;
            float similarity = -1;

            if (root1.NodeType == XmlNodeType.Element && root2.NodeType == XmlNodeType.Element)
            {
                tempIdAtrib = root1.Attributes.GetNamedItem("temp_id");
                tempIdAtrib2 = root2.Attributes.GetNamedItem("temp_id");
                tempIdValue1 = Convert.ToInt32(tempIdAtrib.Value);
                tempIdValue2 = Convert.ToInt32(tempIdAtrib2.Value);
                if (resultTable[tempIdValue1, tempIdValue2] > 0 && depth > 0)
                    similarity = resultTable[tempIdValue1, tempIdValue2];
                else
                {
                    float textSimilarity = computeStringSimilarity(root1.InnerText, root2.InnerText);
                    float nameSimilarity = computeStringSimilarity(root1.Name, root2.Name);
                    float childSimilarity = subelementSimilarity(doc1, doc2, root1, root2);
                    //XmlNodeList list1 = root1.ChildNodes;
                    similarity = (textSimilarity + nameSimilarity + childSimilarity) / 3;

                    // Ulozim hodnotu podobnosti do resultTable
                    if (root1.NodeType == XmlNodeType.Element && root2.NodeType == XmlNodeType.Element)
                    {
                        resultTable[tempIdValue1, tempIdValue2] = similarity;
                    }
                }

            }
            else
            {
                float textSimilarity = computeStringSimilarity(root1.InnerText, root2.InnerText);
                float nameSimilarity = computeStringSimilarity(root1.Name, root2.Name);
                float childSimilarity = subelementSimilarity(doc1, doc2, root1, root2);
                //XmlNodeList list1 = root1.ChildNodes;
                similarity = (textSimilarity + nameSimilarity + childSimilarity) / 3;

                // Ulozim hodnotu podobnosti do resultTable
                //if (root1.Attributes != null && root2.Attributes != null)
                if (root1.NodeType == XmlNodeType.Element && root2.NodeType == XmlNodeType.Element)
                {
                    resultTable[tempIdValue1, tempIdValue2] = similarity;
                }
            }

            if (similarity > 0.7 && root1.Attributes != null && root2.Attributes != null
                && root1.ParentNode.Name == root2.ParentNode.Name && depth == 1)
            {
                float similar1 = -1;
                float similar2 = -1;
                if (root1.Attributes.GetNamedItem("similarity") != null)
                {
                    var atribNode = root1.Attributes.GetNamedItem("similarity");
                    string myString = atribNode.Value;
                    similar1 = Convert.ToSingle(myString);
                }
                if (root2.Attributes.GetNamedItem("similarity") != null)
                {
                    var atribNode = root2.Attributes.GetNamedItem("similarity");
                    string myString = atribNode.Value;
                    similar2 = Convert.ToSingle(myString);
                }
                if (similarity > similar1 && similarity > similar2)
                {
                    if (similar1 > 0 && similar2 < 0)
                    {
                        var similarAtrib = root1.Attributes.GetNamedItem("similarity");
                        similarAtrib.Value = similarity.ToString();
                        var idAtrib = root1.Attributes.GetNamedItem("id");
                        idAtrib.Value = id.ToString();

                        XmlAttribute similarityAtr2 = doc2.CreateAttribute("similarity");
                        similarityAtr2.Value = similarity.ToString();
                        XmlAttribute idAttr2 = doc2.CreateAttribute("id");
                        idAttr2.Value = id.ToString();
                        root2.Attributes.Append(idAttr2);
                        root2.Attributes.Append(similarityAtr2);
                    }
                    else if (similar1 < 0 && similar2 > 0)
                    {
                        var similarAtrib = root2.Attributes.GetNamedItem("similarity");
                        similarAtrib.Value = similarity.ToString();
                        var idAtrib = root2.Attributes.GetNamedItem("id");
                        idAtrib.Value = id.ToString();

                        XmlAttribute similarityAtr = doc1.CreateAttribute("similarity");
                        similarityAtr.Value = similarity.ToString();
                        XmlAttribute idAttr = doc1.CreateAttribute("id");
                        idAttr.Value = id.ToString();
                        root1.Attributes.Append(idAttr);
                        root1.Attributes.Append(similarityAtr);
                    }
                    else if (similar1 > 0 && similar2 > 0)
                    {
                        var similarAtrib = root1.Attributes.GetNamedItem("similarity");
                        similarAtrib.Value = similarity.ToString();
                        var idAtrib = root1.Attributes.GetNamedItem("id");
                        idAtrib.Value = id.ToString();

                        var similarAtrib2 = root1.Attributes.GetNamedItem("similarity");
                        similarAtrib2.Value = similarity.ToString();
                        var idAtrib2 = root1.Attributes.GetNamedItem("id");
                        idAtrib2.Value = id.ToString();
                    }

                    else if (similar1 < 0 && similar2 < 0)
                    {
                        XmlAttribute attr = doc1.CreateAttribute("id");
                        attr.Value = id.ToString();

                        XmlAttribute attr2 = doc2.CreateAttribute("id");
                        attr2.Value = id.ToString();

                        XmlAttribute similarityAtr = doc1.CreateAttribute("similarity");
                        similarityAtr.Value = similarity.ToString();

                        XmlAttribute similarityAtr2 = doc2.CreateAttribute("similarity");
                        similarityAtr2.Value = similarity.ToString();

                        root1.Attributes.Append(attr);
                        root1.Attributes.Append(similarityAtr);
                        root2.Attributes.Append(attr2);
                        root2.Attributes.Append(similarityAtr2);

                    }
                    id++;
                }
            }

            return similarity;
        }

        // Recurzivne vypocitava podobnost vsetkych xml elementov a sucasne ich indexuje
        private void recursiveSimilarity(XmlDocument doc1, XmlDocument doc2, XmlNodeList list1, XmlNodeList list2)
        {
            foreach (XmlNode listNode in list1)
                foreach (XmlNode listNode2 in list2)
                {
                    if (listNode.NodeType == XmlNodeType.Element && listNode2.NodeType == XmlNodeType.Element)
                    {
                        var idAtrib = listNode.Attributes.GetNamedItem("id");
                        var idAtrib2 = listNode2.Attributes.GetNamedItem("id");
                        if (idAtrib != null && idAtrib2 != null && String.Compare(idAtrib.Value, idAtrib2.Value) == 0)
                        {
                            computeSimilarity(doc1, doc2, listNode, listNode2);
                            if (listNode.HasChildNodes && listNode2.HasChildNodes)
                                recursiveSimilarity(doc1, doc2, listNode.ChildNodes, listNode2.ChildNodes);
                        }
                    }
                }
        }

        public void xmlIndexing(String fileName1, String fileName2)
        {
            XmlDocument doc1 = new XmlDocument();
            XmlDocument doc2 = new XmlDocument();
            doc1.Load(fileName1);
            doc2.Load(fileName2);
            XmlNode node1 = doc1.DocumentElement;
            XmlNode node2 = doc2.DocumentElement;

            tempIndedxing(doc1, doc1.ChildNodes);
            tempId1 = tempIndex;
            tempIndex = 0;
            tempIndedxing(doc2, doc2.ChildNodes);
            tempId2 = tempIndex;
            resultTable = new float[tempId1, tempId2];

            for (int i = 0; i < tempId1; i++)
                for (int j = 0; j < tempId2; j++)
                    resultTable[i, j] = -1;

            computeSimilarity(doc1, doc2, node1, node2);
            recursiveSimilarity(doc1, doc2, node1.ChildNodes, node2.ChildNodes);
            doc1.Save("source_data1.xml");
            doc2.Save("source_data2.xml");
        }
    }
}