using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;


namespace ClearFile
{
    class Program
    {
        static string PastaIn;
        static string PastaOut;
        static string formatoNome;


        static void Main(string[] args)
        {
            //Verifica o número de argumentos
            if (args.Length < 3)
            {
                Console.WriteLine("Insira a pasta de origem:");
                PastaIn = Console.ReadLine();
                Console.WriteLine("Insira a pasta de destino:");
                PastaOut = Console.ReadLine();
                Console.WriteLine("Insira o formato do nome do arquivo (1=Ano e Pais origem, 2=Legibilidade, 3=Nome do Livro)");
                formatoNome = Console.ReadLine();
            }
            else
            {
                //Recupera e confere as pastas
                PastaIn = args[0];
                PastaOut = args[1];
                formatoNome = args[2];
            }

            if (!Directory.Exists(PastaIn))
            {
                Console.WriteLine("Pasta de entrada não existe.");
                Console.ReadKey();
                return;
            }

            if (Directory.Exists(PastaOut))
            {
                Console.WriteLine("Pasta de saída já existe. Favor excluir antes de rodar o aplicativo.");
                Console.ReadKey();
                return;
            }




            //Cria a pasta de saída
            Directory.CreateDirectory(PastaOut);


            Console.WriteLine("Iniciando limpeza dos arquivos...");
            Console.WriteLine("Pasta de entrada: " + PastaIn);
            Console.WriteLine("Pasta de saída: " + PastaOut);

            //Percorre as pastas dos livros
            foreach (DirectoryInfo dir in new DirectoryInfo(PastaIn).GetDirectories())
            {
                int intOut;
                //Se o nome for numérico
                if (int.TryParse(dir.Name, out intOut))
                {
                    //Se tiver um diretório de livro filho deste, inicia o processamento
                    if (dir.GetDirectories().Length > 0)
                    {
                        DirectoryInfo dirLivro = dir.GetDirectories()[0];

                        Console.WriteLine("Processando " + dirLivro.Name + "...");

                        processarLivro(dirLivro);
                    }
                }
            }


            Console.WriteLine("Fim do processamento.");


            Console.ReadKey();

        }

        private static void processarLivro(DirectoryInfo pastaLivro)
        {
            string caminhoIndex = pastaLivro.FullName + "\\index.html";
            string caminhoLivro;
            string ano;
            string country;
            string legibilidade;
            string nomeLivro;
            string prefixocampo;
            string caminhoFixo;

            //Se existir o index do livro, abre ele e processa o livro
            if (File.Exists(caminhoIndex))
            {
                //Objeto para extração do ano e do país de origem
                HtmlDocument html = new HtmlDocument();
                html.Load(caminhoIndex);

                HtmlNode divLateral = html.GetElementbyId("column_secondary");

                //Leitura do ano e do país de origem
                ano = divLateral.ChildNodes[1].ChildNodes[1].ChildNodes[1].LastChild.InnerText.Trim();
                country = divLateral.ChildNodes[1].ChildNodes[1].ChildNodes[5].LastChild.InnerText.Trim();
                legibilidade = divLateral.ChildNodes[1].ChildNodes[3].ChildNodes[1].ChildNodes[3].ChildNodes[1].ChildNodes[1].InnerText.Trim();
                nomeLivro = pastaLivro.Name;


                if (country.Length == 0)
                    country = "Indeterminado";

                if (ano.Length == 0)
                    ano = "0000";

                if (legibilidade.Length == 0)
                    legibilidade = "0";

                legibilidade = legibilidade.Replace(".", "_");


                //Monta o nome do arquivo
                switch (formatoNome)
                {
                    case "1":
                        caminhoLivro = PastaOut + "\\" + ano + "-" + country + ".txt";
                        prefixocampo = ano + "|" + country + "|";
                        break;
                    case "2":
                        caminhoLivro = PastaOut + "\\" + legibilidade + ".txt";
                        prefixocampo = legibilidade + "|";
                        break;
                    default:
                        caminhoLivro = PastaOut + "\\" + nomeLivro + ".txt";
                        prefixocampo = nomeLivro + "|";
                        break;
                }


                caminhoFixo = PastaOut + "\\dataOut.txt";
                caminhoLivro = caminhoFixo;

                Console.WriteLine("Arquivo: " + caminhoLivro);

                //Se não existir o arquivo, cria ele e depois coloca os capitulos limpos
                if (!File.Exists(caminhoLivro))
                {
                    File.CreateText(caminhoLivro).Close();
                }


                //Percorre os capítulos
                foreach (var cap in pastaLivro.GetDirectories())
                {
                    string caminhoCapitulo = cap.GetDirectories()[0].FullName + "\\index.html";

                    //Verifica se o arquivo index do capitulo existe
                    if (!File.Exists(caminhoCapitulo))
                        continue;

                    HtmlDocument htmlCapitulo = new HtmlDocument();
                    htmlCapitulo.Load(caminhoCapitulo);
                    //var body = htmlCapitulo.DocumentNode.SelectNodes("//body");
                    var div_shrink_wrap = htmlCapitulo.GetElementbyId("shrink_wrap");

                    //Se o documento não possui body, então vai pro proximo capitulo
                    if (div_shrink_wrap == null)
                        continue;

                    var paragrafos = new List<string>();

                    foreach (var item in div_shrink_wrap.DescendantsAndSelf())
                    {
                        if (item.NodeType == HtmlNodeType.Text && item.ParentNode.Name == "p")
                        {
                            if (item.InnerText.Trim() != "")
                            {
                                paragrafos.Add(item.InnerText.Trim());
                            }
                        }
                    }

                    //Junta os parágrafos do texto, e retira os caracteres "Pipe", se tiver, pois serão utilizados como separador no pig
                    string capitulo = WebUtility.HtmlDecode(String.Join(" ", paragrafos)).Replace("|", " ");//.Replace(".", " ").Replace(",", " ").Replace("!", " ").Replace("?", " ").Replace("-", " ").Replace("—", " ").Replace("\"", "").Replace("“","").Replace("”", "");

                    capitulo = Regex.Replace(capitulo, "[^a-zA-Z\\s]+", "");

                    if (capitulo.Replace(" ", "").Length > 0)
                        File.AppendAllText(caminhoLivro, prefixocampo + capitulo + Environment.NewLine);

                }

            }
        }



    }
}
