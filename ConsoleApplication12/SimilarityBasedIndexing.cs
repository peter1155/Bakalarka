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
        // Sluzi na indexovanie podobnych elementov
        static Int64 _id = 0;

        // Vracia maximum dvoch cisel
        private static int Max(int a, int b)
        {
            return (a > b) ? a : b;
        }

        // Longest common subsequence vracia najdlhsiu spolocnu postupnost dvoch stringov
        private static int Lcs(string s1, string s2)
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
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    if (s1[i - 1] == s2[j - 1])
                        table[i, j] = table[i - 1, j - 1] + 1;
                    else
                    {
                        table[i, j] = Max(table[i - 1, j], table[i, j - 1]);
                    }

                }
            }
            return table[s1.Length, s2.Length];
        }

        // Vracia textovu podobnost dvoch stringov ako podiel ich LCS a priemernej dlzky
        private static float ComputeStringSimilarity(string str1, string str2)
        {
            if (str1 == "" && str2 == "")
                return 1;
            int lcs = Lcs(str1, str2);
            float avarage = (str1.Length + str2.Length) / (float)2;
            return lcs / avarage;
        }

        // Vracia index na ktorom sa nachadza node v XmlNodeList list
        private static int GetIndex(XmlNodeList list, XmlNode node)
        {
            for (int i = 0; i < list.Count; i++)
                if (list.Item(i).Equals(node))
                    return i;
            return -1;
        }

        // Sluzi na hladanie najlepsieho parovania nazvov subelementov
        // na zaklade vstupnej matice vyrata najlepsie parovanie nazvov
        // subelementov vracia sucet podobnosti nazvov vsetkych subelementov
        // podeleny priemernym poctom subelementov
        private float GreedyMatching(float[,] matrix)
        {
            float max = matrix[0, 0];
            int i1 = 0, i2 = 0;
            float sum = 0;
            int count;

            if (matrix.GetLength(0) > matrix.GetLength(1))
                count = matrix.GetLength(1);
            else
                count = matrix.GetLength(0);

            // Prechadza postupne celu maticu v kazdej iteracii najde maximalny prvok matice
            // nasledne prenuluje riadok aj stlpec na ktorom sa nachadza dany prvok a hodnotu
            // podobnosti pripocita do premennej sum, toto sa opakuje kym sa neprenuluje cela matica
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

            // Vyrata priemerny pocet subelementov
            float avarage = (matrix.GetLength(0) + matrix.GetLength(1)) / 2;

            // Vracia sumu podobnosti podelenu priemernym poctom subelementov
            return sum / avarage;
        }

        // Vracia podobnost dvoch xml elementov
        public float ComputeSimilarity(XmlDocument doc1, XmlDocument doc2, XmlNode root1, XmlNode root2)
        {
            float similarity = -1;

            // Zabezpeci aby komentar nebol namapovany s kodom ... 
            if ((root1.Name == "comment" && root2.Name != "comment")
                || (root1.Name != "comment" && root2.Name == "comment"))
                return 0;

            // Zabezpeci aby sa volania kniznicnych funkcii printf, scanf, putchar mapovali vzdy iba na seba
            if (root1.NodeType == XmlNodeType.Element && root2.NodeType == XmlNodeType.Element
                && root1.Name == "call" && root2.Name == "call"
                && ((root1.ChildNodes[0].InnerText == "printf" && root2.ChildNodes[0].InnerText != "printf")
                || (root1.ChildNodes[0].InnerText == "scanf" && root2.ChildNodes[0].InnerText != "scanf")
                || (root1.ChildNodes[0].InnerText == "putchar" && root2.ChildNodes[0].InnerText != "putchar")
                || (root1.ChildNodes[0].InnerText != "printf" && root2.ChildNodes[0].InnerText == "printf")
                || (root1.ChildNodes[0].InnerText != "scanf" && root2.ChildNodes[0].InnerText == "scanf")
                || (root1.ChildNodes[0].InnerText != "putchar" && root2.ChildNodes[0].InnerText == "putchar")))
                return 0;

            // Zabezpeci aby sa main vzdy mapoval na main
            if (root1.NodeType == XmlNodeType.Element && root2.NodeType == XmlNodeType.Element
                && root1.Name == "function" && root2.Name == "function" && root1.ChildNodes[1].InnerText == "main"
                && root2.ChildNodes[1].InnerText == "main")
            {
                similarity = 1;
            }
            else if (root1.NodeType == XmlNodeType.Element && root2.NodeType == XmlNodeType.Element)
            {
                // Ziska podobnost nazvov subelementov
                float nameSimilarity = ComputeStringSimilarity(root1.Name, root2.Name);

                // Ziska podobnst textoveho obsahu danych elementov
                float textSimilarity = ComputeStringSimilarity(root1.InnerText, root2.InnerText);

                // Ziska podobnost nazvov subelementov - takymto sposobom sa ziskava
                // podobnost struktury pretoze nazvy premennych sa mozu zmenit ale struktura 
                // ostava zachovana
                float structureSimilarity = SubelementNameSimilarity(root1, root2);

                // Pri komentaroch treaba pouzit tento vypocet lebo podobnost nazvov aj subelementov je vzdy 1
                if (root1.Name == "comment" && root2.Name == "comment")
                    similarity = (textSimilarity * 5 + nameSimilarity + structureSimilarity) / 7;
                else
                    similarity = (textSimilarity * 3 + nameSimilarity + structureSimilarity) / 5;

            }

            // Ak bude indexovanie pracovat divne treba odkomentovat
            /*else
            {
                float textSimilarity = computeStringSimilarity(root1.InnerText, root2.InnerText);
                float nameSimilarity = computeStringSimilarity(root1.Name, root2.Name);

                similarity = (textSimilarity * 4 + nameSimilarity) / 5;
            }*/

            // Ak je podobnost vecsia ako nastavena prahova hodnota a jedna sa o Element  potom nastav podobnost  a idecka
            // zaroven musi platit ze podobnost je vecsia ako uz pred tym nastavena ak bola nastavena
            if (similarity > 0.7 && root1.NodeType == XmlNodeType.Element && root2.NodeType == XmlNodeType.Element
                && root1.ParentNode.Name == root2.ParentNode.Name)
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
                        idAtrib.Value = _id.ToString();

                        XmlAttribute similarityAtr2 = doc2.CreateAttribute("similarity");
                        similarityAtr2.Value = similarity.ToString();
                        XmlAttribute idAttr2 = doc2.CreateAttribute("id");
                        idAttr2.Value = _id.ToString();
                        root2.Attributes.Append(idAttr2);
                        root2.Attributes.Append(similarityAtr2);
                    }
                    else if (similar1 < 0 && similar2 > 0)
                    {
                        var similarAtrib = root2.Attributes.GetNamedItem("similarity");
                        similarAtrib.Value = similarity.ToString();
                        var idAtrib = root2.Attributes.GetNamedItem("id");
                        idAtrib.Value = _id.ToString();

                        XmlAttribute similarityAtr = doc1.CreateAttribute("similarity");
                        similarityAtr.Value = similarity.ToString();
                        XmlAttribute idAttr = doc1.CreateAttribute("id");
                        idAttr.Value = _id.ToString();
                        root1.Attributes.Append(idAttr);
                        root1.Attributes.Append(similarityAtr);
                    }
                    else if (similar1 > 0 && similar2 > 0)
                    {
                        var similarAtrib = root1.Attributes.GetNamedItem("similarity");
                        similarAtrib.Value = similarity.ToString();
                        var idAtrib = root1.Attributes.GetNamedItem("id");
                        idAtrib.Value = _id.ToString();

                        var similarAtrib2 = root1.Attributes.GetNamedItem("similarity");
                        similarAtrib2.Value = similarity.ToString();
                        var idAtrib2 = root1.Attributes.GetNamedItem("id");
                        idAtrib2.Value = _id.ToString();
                    }

                    else if (similar1 < 0 && similar2 < 0)
                    {
                        XmlAttribute attr = doc1.CreateAttribute("id");
                        attr.Value = _id.ToString();

                        XmlAttribute attr2 = doc2.CreateAttribute("id");
                        attr2.Value = _id.ToString();

                        XmlAttribute similarityAtr = doc1.CreateAttribute("similarity");
                        similarityAtr.Value = similarity.ToString();

                        XmlAttribute similarityAtr2 = doc2.CreateAttribute("similarity");
                        similarityAtr2.Value = similarity.ToString();

                        root1.Attributes.Append(attr);
                        root1.Attributes.Append(similarityAtr);
                        root2.Attributes.Append(attr2);
                        root2.Attributes.Append(similarityAtr2);

                    }
                    _id++;
                }
            }

            return similarity;
        }

        // Tu sa vypočítava all to all matching subelementov bud sa nájde hashovaním alebo sa volá
        // compute similarity
        private void SubelementSimilarity(XmlDocument doc1, XmlDocument doc2, XmlNode root1, XmlNode root2)
        {
            XmlNodeList list1 = root1.ChildNodes;
            XmlNodeList list2 = root2.ChildNodes;

            // Do matice sa ukladaju hodnoty podobnosti subelementov
            float[,] matrix = new float[root1.ChildNodes.Count, root2.ChildNodes.Count];

            // Hesovanim najdeme v rychlom case elementy, ktore su zhodne
            Dictionary<int, XmlNode> map = new Dictionary<int, XmlNode>();
            bool hashing = true;

            foreach (XmlNode node in list1)
            {
                XmlNode tempNode;

                if (!map.TryGetValue((node.Name + node.InnerText).GetHashCode(), out tempNode))
                {
                    map.Add((node.Name + node.InnerText).GetHashCode(), node);
                }
                else
                {
                    // V pripade ze sa tam nachadza dva-krat ten isty element - nemozme hashovat
                    // napr. pole[i] = 5; pole[i] = 5+7; - pole[i] je tam dva krat a pre druhy vyskyt
                    // by bolo priradene nespravne id ... 
                    hashing = false;
                    break;
                }
            }

            // S pouzitim hashovania
            if (hashing)
            {
                for (int j = 0; j < list2.Count; j++)
                {
                    XmlNode tempNode;

                    if (map.TryGetValue((list2.Item(j).Name + list2.Item(j).InnerText).GetHashCode(), out tempNode))
                    {
                        int i = GetIndex(list1, tempNode);

                        // Ak sa prvok nachadza v HashTabulke

                        if (i != -1)
                        {

                            // Ak dana pozicia matice nebola zatial nastavena - nebola nastavena na 1 a
                            // zaroven nebola nastavena na -1 lebo v prislusnom riadku/stlpci uz bol priradeny index

                            if (matrix[i, j] == 0)
                            {

                                // Ak sa v hashTabulke nachadza dany element prirad IDecka

                                matrix[i, j] = 1.0f;

                                if (list1.Item(i).NodeType == XmlNodeType.Element && list2.Item(j).NodeType == XmlNodeType.Element)
                                {
                                    String similarity = "1";

                                    XmlAttribute attr = doc1.CreateAttribute("id");
                                    attr.Value = _id.ToString();

                                    XmlAttribute attr2 = doc2.CreateAttribute("id");
                                    attr2.Value = _id.ToString();

                                    XmlAttribute similarityAtr = doc1.CreateAttribute("similarity");
                                    similarityAtr.Value = similarity;

                                    XmlAttribute similarityAtr2 = doc2.CreateAttribute("similarity");
                                    similarityAtr2.Value = similarity;

                                    list1.Item(i).Attributes.Append(attr);
                                    list1.Item(i).Attributes.Append(similarityAtr);
                                    list2.Item(j).Attributes.Append(attr2);
                                    list2.Item(j).Attributes.Append(similarityAtr2);
                                    _id++;
                                }
                            }
                            else

                                // Ak uz v danom riadku/stlpci je priradeny index prejdi na computeSimilarity
                                // pretoze sa opat nemoze hashovat
                                break;

                            // Nastavim ostatne pozicie matice (v riadku a stlpci) na -1 aby som ich nemusel znova prechadzat

                            for (int k = 0; k < root2.ChildNodes.Count; k++)
                            {
                                if (k != j)
                                    matrix[i, k] = -1;
                            }
                            for (int k = 0; k < root1.ChildNodes.Count; k++)
                            {
                                if (k != i)
                                    matrix[k, j] = -1;
                            }
                        }
                    }
                }
            }

            // Pre kazdy zatial nepriradeny prvok matice pocitaj podobnost pomocou funkcie ComputeSimilarity
            for (int i = 0; i < root1.ChildNodes.Count; i++)
                for (int j = 0; j < root2.ChildNodes.Count; j++)
                {
                    if (matrix[i, j] == 0)
                    {
                        matrix[i, j] = ComputeSimilarity(doc1, doc2, list1.Item(i), list2.Item(j));
                    }
                }
        }

        // Tu vypocitavam all to all podobnost nazvov subelementov takto sa sleduje podobnost struktury 
        private float SubelementNameSimilarity(XmlNode root1, XmlNode root2)
        {
            XmlNodeList list1 = root1.ChildNodes;
            XmlNodeList list2 = root2.ChildNodes;

            // Do matice sa ukladaju hodnoty pravdepodobnosti subelementov
            float[,] matrix = new float[root1.ChildNodes.Count, root2.ChildNodes.Count];

            // Hesovanim najdeme v rychlom case elementy, ktore su zhodne
            Dictionary<int, XmlNode> map = new Dictionary<int, XmlNode>();
            bool hashing = true;

            foreach (XmlNode node in list1)
            {
                XmlNode tempNode;

                if (!map.TryGetValue((node.Name).GetHashCode(), out tempNode))
                {
                    map.Add((node.Name).GetHashCode(), node);
                }
                else
                {
                    // V pripade ze sa tam nachadza dva-krat ten isty element - nemozme hashovat
                    // napr. pole[i] = 5; pole[i] = 5+7; - pole[i] je tam dva krat a pre druhy vyskyt
                    // by bolo priradene nespravne id ... 
                    hashing = false;
                    break;
                }
            }

            if (hashing)
            {
                for (int j = 0; j < list2.Count; j++)
                {
                    XmlNode tempNode;

                    if (map.TryGetValue((list2.Item(j).Name).GetHashCode(), out tempNode))
                    {
                        int i = GetIndex(list1, tempNode);

                        // Ak sa prvok nachadza v HashTabulke

                        if (i != -1)
                        {

                            // Ak dana pozicia matice nebola zatial nastavena - nebola nastavena na 0 a
                            // zaroven nebola nastavena na -1 lebo v prislusnom riadku/stlpci uz bol priradeny index

                            if (matrix[i, j] == 0)
                            {
                                // Ak sa v hashTabulke nachadza dany element prirad IDecka
                                matrix[i, j] = 1.0f;
                            }
                            else
                                // Ak uz v danom riadku/stlpci je priradeny index prejdi na computeSimilarity
                                // pretoze sa opat nemoze hashovat
                                break;

                            // Nastavim ostatne pozicie matice (v riadku a stlpci) na -1 aby som ich nemusel znova prechadzat

                            for (int k = 0; k < root2.ChildNodes.Count; k++)
                            {
                                if (k != j)
                                    matrix[i, k] = -1;
                            }
                            for (int k = 0; k < root1.ChildNodes.Count; k++)
                            {
                                if (k != i)
                                    matrix[k, j] = -1;
                            }
                        }
                    }
                }
            }

            // Dopocitam zvysne nenastavene prvky matice
            for (int i = 0; i < root1.ChildNodes.Count; i++)
                for (int j = 0; j < root2.ChildNodes.Count; j++)
                {
                    if (matrix[i, j] == 0)
                    {
                        matrix[i, j] = ComputeStringSimilarity(list1.Item(i).Name, list2.Item(j).Name);
                    }
                }

            // Ak ma jeden uzol 0 subelementov a druhy viac vrat 0 ak maju oba dva 0 subelementov vrat 1
            // v ostatnych pripadoch rataj podobnst cez greedyMatching
            if (root1.ChildNodes.Count == 0 && root2.ChildNodes.Count == 0)
                return 1;
            if ((root1.ChildNodes.Count == 0 && root2.ChildNodes.Count != 0)
                || (root1.ChildNodes.Count != 0 && root2.ChildNodes.Count == 0))
                return 0;
            return GreedyMatching(matrix);
        }

        // Rekurzivne vypocitava podobnost vsetkych xml elementov a sucasne ich indexuje
        private void RecursiveSimilarity(XmlDocument doc1, XmlDocument doc2, XmlNodeList list1, XmlNodeList list2)
        {
            foreach (XmlNode listNode in list1)
                foreach (XmlNode listNode2 in list2)
                {
                    if (listNode.NodeType == XmlNodeType.Element && listNode2.NodeType == XmlNodeType.Element)
                    {
                        var idAtrib = listNode.Attributes.GetNamedItem("id");
                        var idAtrib2 = listNode2.Attributes.GetNamedItem("id");

                        // Ak maju oba elementy rovnake id rataj podobnost ich child elementov
                        // pokracuj rekurzivne na nizsie urovne
                        if (idAtrib != null && idAtrib2 != null && String.Compare(idAtrib.Value, idAtrib2.Value) == 0)
                        {
                            SubelementSimilarity(doc1, doc2, listNode, listNode2);
                            if (listNode.HasChildNodes && listNode2.HasChildNodes)
                                RecursiveSimilarity(doc1, doc2, listNode.ChildNodes, listNode2.ChildNodes);
                            break;
                        }
                    }
                }
        }

        // try find nonindexed elements
        private void PostIndexing(XmlDocument doc1, XmlDocument doc2, XmlNodeList list1, XmlNodeList list2)
        {
            Dictionary<int, XmlNode> map = new Dictionary<int, XmlNode>();
            Hashing(doc1, list1, map);
            FindSameElements(doc2, doc1, list2, map);
        }

        // Hash all nonindexed elements
        private void Hashing(XmlDocument doc1, XmlNodeList list1, Dictionary<int, XmlNode> map)
        {
            foreach (XmlNode listNode in list1)
                if (listNode.NodeType == XmlNodeType.Element)
                {
                    var idAtrib = listNode.Attributes.GetNamedItem("id");

                    // Ak danemu elemntu zatial nebolo priradene id tak ho pridaj do HashMapy
                    if (idAtrib == null)
                    {
                        XmlNode tempNode;

                        if (!map.TryGetValue((listNode.Name + listNode.InnerText).GetHashCode(), out tempNode))
                        {
                            map.Add((listNode.Name + listNode.InnerText).GetHashCode(), listNode);
                        }
                    }
                    if (listNode.HasChildNodes)
                        Hashing(doc1, listNode.ChildNodes, map);
                }
        }

        // Najde rovnake elementy pomocou hashovania a priradi im rovnake id
        private void FindSameElements(XmlDocument doc1, XmlDocument doc2, XmlNodeList list1, Dictionary<int, XmlNode> map)
        {
            foreach (XmlNode listNode in list1)
                if (listNode.NodeType == XmlNodeType.Element)
                {
                    var idAtrib = listNode.Attributes.GetNamedItem("id");
                    if (idAtrib == null)
                    {
                        XmlNode tempNode;

                        // Ak sa element s rovnakym hashom nachadza v mape potom prirad elementom rovnake id
                        if (map.TryGetValue((listNode.Name + listNode.InnerText).GetHashCode(), out tempNode))
                        {
                            XmlAttribute idAttr = doc1.CreateAttribute("id");
                            idAttr.Value = _id.ToString();
                            listNode.Attributes.Append(idAttr);

                            XmlAttribute idAttr2 = doc2.CreateAttribute("id");
                            idAttr2.Value = _id.ToString();
                            tempNode.Attributes.Append(idAttr2);

                            _id++;
                        }
                    }
                    // Ak ma element deti pokracuj rekurzivne na tieto deti
                    if (listNode.HasChildNodes)
                        FindSameElements(doc1, doc2, listNode.ChildNodes, map);
                }
        }

        // Priradi idecka elementom pre ktore sa nenaslo parovanie
        private void NotMatchedIndexing(XmlDocument doc1, XmlNodeList list1)
        {
            foreach (XmlNode listNode in list1)
                if (listNode.NodeType == XmlNodeType.Element)
                {
                    var idAtrib = listNode.Attributes.GetNamedItem("id");

                    // Ak dany element nema id prirad mu ho
                    if (idAtrib == null)
                    {
                        XmlAttribute idAttr = doc1.CreateAttribute("id");
                        idAttr.Value = _id.ToString();
                        listNode.Attributes.Append(idAttr);
                        _id++;
                    }

                    // Pokracuj rekurzivne na deti elementu pokial nejake ma
                    if (listNode.HasChildNodes)
                        NotMatchedIndexing(doc1, listNode.ChildNodes);
                }
        }

        // Vyselektuje vsetky funkcie pre tie ktore sa na seba namapovali vykona all to all podobnost subelementov ktore 
        // doposial nemaju index
        private void AllNotIndexedSimilarity(XmlDocument doc1, XmlDocument doc2)
        {
            // Vytvara manager pre selektovanie funkcii
            XmlTextReader reader = new XmlTextReader("source_data1.xml");
            XmlNamespaceManager nsmanager1 = new XmlNamespaceManager(reader.NameTable);
            nsmanager1.AddNamespace("base", "http://www.sdml.info/srcML/src");

            XmlNodeList functionsList1 = doc1.SelectNodes("//base:function", nsmanager1);

            reader = new XmlTextReader("source_data2.xml");
            XmlNamespaceManager nsmanager2 = new XmlNamespaceManager(reader.NameTable);
            nsmanager2.AddNamespace("base", "http://www.sdml.info/srcML/src");

            XmlNodeList functionsList2 = doc2.SelectNodes("//base:function", nsmanager2);
            foreach (XmlNode list1Node in functionsList1)
                foreach (XmlNode list2Node in functionsList2)
                {
                    XmlNode id1 = list1Node.Attributes.GetNamedItem("id", "");
                    XmlNode id2 = list2Node.Attributes.GetNamedItem("id", "");
                    if (id1 != null && id2 != null && id1.Value == id2.Value)
                    {
                        List<XmlNode> list1 = new List<XmlNode>();
                        List<XmlNode> list2 = new List<XmlNode>();
                        GetNotIndexedNodes(list1Node.ChildNodes, list1);
                        GetNotIndexedNodes(list2Node.ChildNodes, list2);
                        AllToAllSimilarity(doc1, doc2, list1, list2);
                       
                    }
                }
        }

        // Priradi idecka elementom pre ktore sa nenaslo parovanie
        private void GetNotIndexedNodes(XmlNodeList list1, List<XmlNode> list2)
        {
            foreach (XmlNode listNode in list1)
                if (listNode.NodeType == XmlNodeType.Element)
                {
                    var idAtrib = listNode.Attributes.GetNamedItem("id");

                    // Ak dany element nema id prirad mu ho
                    if (idAtrib == null)
                    {
                        list2.Add(listNode);
                    }

                    // Pokracuj rekurzivne na deti elementu pokial nejake ma
                    if (listNode.HasChildNodes)
                        GetNotIndexedNodes(listNode.ChildNodes, list2);
                }
        }

        /*private void computeSimilarityForNotIndexed(XmlDocument doc1, XmlDocument doc2)
        {
            List<XmlNode> list1 = new List<XmlNode>();
            List<XmlNode> list2 = new List<XmlNode>();
            getNotIndexedNodes(doc1.ChildNodes, list1);
            getNotIndexedNodes(doc2.ChildNodes, list2);

            AllToAllSimilarity(doc1, doc2, list1, list2);

        }*/

        // Tu sa vypočítava all to all matching subelementov bud sa nájde hashovaním alebo sa volá
        // compute similarity
        private void AllToAllSimilarity(XmlDocument doc1, XmlDocument doc2, List<XmlNode> list1, List<XmlNode> list2)
        {
            //XmlNodeList list1 = root1.ChildNodes;
            //XmlNodeList list2 = root2.ChildNodes;

            // Do matice sa ukladaju hodnoty podobnosti subelementov
            float[,] matrix = new float[list1.Count, list2.Count];

            // Hesovanim najdeme v rychlom case elementy, ktore su zhodne
            Dictionary<int, XmlNode> map = new Dictionary<int, XmlNode>();
            bool hashing = true;

            foreach (XmlNode node in list1)
            {
                XmlNode tempNode;

                if (!map.TryGetValue((node.Name + node.InnerText).GetHashCode(), out tempNode))
                {
                    map.Add((node.Name + node.InnerText).GetHashCode(), node);
                }
                else
                {
                    // V pripade ze sa tam nachadza dva-krat ten isty element - nemozme hashovat
                    // napr. pole[i] = 5; pole[i] = 5+7; - pole[i] je tam dva krat a pre druhy vyskyt
                    // by bolo priradene nespravne id ... 
                    hashing = false;
                    break;
                }
            }

            // S pouzitim hashovania
            if (hashing)
            {
                for (int j = 0; j < list2.Count; j++)
                {
                    XmlNode tempNode;

                    if (map.TryGetValue((list2.ElementAt(j).Name + list2.ElementAt(j).InnerText).GetHashCode(), out tempNode))
                    {
                        int i = list1.FindIndex(a => a == tempNode);

                        // Ak sa prvok nachadza v HashTabulke

                        if (i != -1)
                        {

                            // Ak dana pozicia matice nebola zatial nastavena - nebola nastavena na 1 a
                            // zaroven nebola nastavena na -1 lebo v prislusnom riadku/stlpci uz bol priradeny index

                            if (matrix[i, j] == 0)
                            {

                                // Ak sa v hashTabulke nachadza dany element prirad IDecka

                                matrix[i, j] = 1.0f;

                                if (list1.ElementAt(i).NodeType == XmlNodeType.Element && list2.ElementAt(j).NodeType == XmlNodeType.Element)
                                {
                                    String similarity = "1";

                                    XmlAttribute attr = doc1.CreateAttribute("id");
                                    attr.Value = _id.ToString();

                                    XmlAttribute attr2 = doc2.CreateAttribute("id");
                                    attr2.Value = _id.ToString();

                                    XmlAttribute similarityAtr = doc1.CreateAttribute("similarity");
                                    similarityAtr.Value = similarity;

                                    XmlAttribute similarityAtr2 = doc2.CreateAttribute("similarity");
                                    similarityAtr2.Value = similarity;

                                    list1.ElementAt(i).Attributes.Append(attr);
                                    list1.ElementAt(i).Attributes.Append(similarityAtr);
                                    list2.ElementAt(j).Attributes.Append(attr2);
                                    list2.ElementAt(j).Attributes.Append(similarityAtr2);
                                    _id++;
                                }
                            }
                            else

                                // Ak uz v danom riadku/stlpci je priradeny index prejdi na computeSimilarity
                                // pretoze sa opat nemoze hashovat
                                break;

                            // Nastavim ostatne pozicie matice (v riadku a stlpci) na -1 aby som ich nemusel znova prechadzat

                            for (int k = 0; k < list2.Count; k++)
                            {
                                if (k != j)
                                    matrix[i, k] = -1;
                            }
                            for (int k = 0; k < list1.Count; k++)
                            {
                                if (k != i)
                                    matrix[k, j] = -1;
                            }
                        }
                    }
                }
            }

            // Pre kazdy zatial nepriradeny prvok matice pocitaj podobnost pomocou funkcie ComputeSimilarity
            for (int i = 0; i < list1.Count; i++)
                for (int j = 0; j < list2.Count; j++)
                {
                    if (matrix[i, j] == 0)
                    {
                        matrix[i, j] = ComputeSimilarity(doc1, doc2, list1.ElementAt(i), list2.ElementAt(j));
                    }
                }
        }

        // Priradi indexy podobnym elementom medzi dvoma verziami xml suborov
        public void XmlIndexing(String fileName1, String fileName2)
        {
            // Nacita obsah suborov fileName1/2 do objektu typu XmlDocument
            XmlDocument doc1 = new XmlDocument();
            XmlDocument doc2 = new XmlDocument();
            doc1.Load(fileName1);
            doc2.Load(fileName2);

            // Ziska z dokumentov korenove elementy
            XmlNode node1 = doc1.DocumentElement;
            XmlNode node2 = doc2.DocumentElement;

            // Priradi rovnake idecka korenovym elementom
            XmlAttribute attr = doc1.CreateAttribute("id");
            attr.Value = _id.ToString();

            XmlAttribute attr2 = doc2.CreateAttribute("id");
            attr2.Value = _id.ToString();

            node1.Attributes.Append(attr);
            node2.Attributes.Append(attr2);

            // Zvysi hodnotu pre id counter
            _id++;

            // Rekurzivne priradzuje id podobnym elementom
            RecursiveSimilarity(doc1, doc2, doc1.ChildNodes, doc2.ChildNodes);

            // Ak sa vyuziva komplexna metoda vykona sa all to all
            // porovnanie vsetkych elementov patriacich do rovnakych funkcii
            if (Options.Method == Options.Methods.Complex)
            {
                // Pre zvysne nezaindexovane uzly sa pokusa ratat all to all podobnost
                //computeSimilarityForNotIndexed(doc1, doc2);
                AllNotIndexedSimilarity(doc1, doc2);
            }

            // Najde rovnake elementy ktorym neboli priradene idecka
            //postIndexing(doc1, doc2, node1.ChildNodes, node2.ChildNodes);

            // Priradi id elemetom ktore sa na nic nemapuju
            NotMatchedIndexing(doc1, node1.ChildNodes);
            NotMatchedIndexing(doc2, node2.ChildNodes);

            // Ulozi xml s indexmi do suborov
            doc1.Save("source_data1.xml");
            doc2.Save("source_data2.xml");

            // Resetuje id counter
            _id = 0;

        }
    }
}