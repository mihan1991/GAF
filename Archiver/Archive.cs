using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Archiver
{
    public class Archive
    {
        //кодировка
        ASCIIEncoding utf8;

        //сигнатуры
        private const string HEADER = "GAF!";
        private const string TAB_IN = "<TAB";
        private const string TAB_OUT = "TAB>";
        //private int Key_Lenght;
        private int Sample_Length;
        private const string BORDER_KEY = "<()>";
        private const string BORDER_NORM = "<!!>";

        private string[] Files;         //список файлов
        private List<Dict> Tables;       //коллекция словарей
        private int TablesCount;        //кол-во таблиц
        private byte[] buf;             //буфер чтения
        private List<string> data;      //содержимое в виде строк

        private string root;            //корневая директория с файлами


        public Archive() { }

        
        public Archive(string r, int min_len, int max_len)
        {
            utf8 = new ASCIIEncoding();
            root = r;
            TablesCount = 0;
            Files = Directory.GetFiles(root);
            Tables = new List<Dict>();
            data = new List<string>();

            Console.WriteLine("Чтение файлов...\n");
            for (int i = 0; i < Files.Length; i++)
            {
                Console.WriteLine(string.Format("Чтение файла {0}",Files[i]));
                FileStream fstream = new FileStream(Files[i], FileMode.Open);
                buf = new byte[fstream.Length];
                fstream.Read(buf, 0, (int)fstream.Length);
                fstream.Close();

                data.Add(GetString(buf));
            }

            //заполняем таблицы
            Console.WriteLine("\nЗаполнение таблиц...\n");
            for(int sample_len = min_len; sample_len <= max_len; sample_len++)
            {
                if (data[0].Length % sample_len == 0)
                {
                    Console.WriteLine(string.Format("Заполнение таблицы, с длиной образца {0}", sample_len));
                    Tables.Add(new Dict(sample_len, data[0].Length));

                    //бекапим содержимое 1 файла
                    string new_data = data[0];

                    //загоняем образцы
                    while(new_data.Length > sample_len)
                    {
                        //формируем образец
                        string sample = new_data.Remove(sample_len);
                        //отсекаем его от остатков
                        new_data = new_data.Substring(sample_len);
                        //отправляем в словарь
                        Tables[TablesCount].SendSample(sample);
                    }
                    Tables[TablesCount].CleanDict();
                    Tables[TablesCount].GetInfo();
                    //break;
                    TablesCount++;
                }
            }
            Console.WriteLine("Заполнение таблиц завершено.");
            Compress(Tables[TabCompare()]);
        }

        private int TabCompare()
        {
            int max = 0, res=-1;
            for (int i = 0; i < TablesCount; i++)
            {
                if (Tables[i].GetWon() > max)
                {
                    max = Tables[i].GetWon();
                    res = i;
                }
            }
            return res;
        }
        private byte[] GetBytes(string str)
        {
            byte[] bytes = utf8.GetBytes(str);
            //System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private string GetString(byte[] bytes)
        {
            string s = utf8.GetString(bytes, 0, bytes.Length);
            return s;
        }

        private byte[] ConvertTobyteArray(List<Dict> obj)
        {
            Encoding encode = Encoding.ASCII;

            List<byte> listByte = new List<byte>();
            string[] ResultCollectionArray = obj.Select(i => i.ToString()).ToArray<string>();

            foreach (var item in ResultCollectionArray)
            {
                foreach (byte b in encode.GetBytes(item))
                    listByte.Add(b);
            }

            return listByte.ToArray();
        }

        // Convert an object to a byte array
        private byte[] VolumesToByteArray(Dict obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }

        //сериализация словаря
        private void Compress(Dict dict)
        {
            Console.WriteLine("Проход 2. Записываю архив.\n");
            byte[] b;
            //string tmp="";
            FileStream fs = new FileStream(root + "/" + "archive.bin", FileMode.OpenOrCreate);

            //пишем сигнатуры

            //заголовок
            b = GetBytes(HEADER);
            //tmp += HEADER;
            fs.Write(b, 0, b.Length);

            //заголовок таблицы
            b = GetBytes(TAB_IN);
            //tmp += TAB_IN;
            fs.Write(b, 0, b.Length);

            //пишем длину семпла
            Sample_Length = dict.GetSampleLength();
            b = BitConverter.GetBytes(Sample_Length);
            //tmp += Sample_Length;
            fs.Write(b, 0, b.Length);

            //пишет таблицу 
            Dictionary<int, string> d = dict.GetTable();
            for (int i = 0; i < d.Keys.Count; i++)
            {
                int q = d.First(a => a.Value == d[i]).Key;
                b = BitConverter.GetBytes(q);
                //tmp += q;
                fs.Write(b, 0, b.Length);

                b = GetBytes(d[i]);
                //tmp += d[i];
                fs.Write(b, 0, b.Length);

                if (i < d.Keys.Count - 1)
                {
                    b = GetBytes(BORDER_NORM);
                    //tmp += BORDER_NORM;
                    fs.Write(b, 0, b.Length);
                }
            }
            //пишем завершение таблицы
            b = GetBytes(TAB_OUT);
            //tmp += TAB_OUT;
            fs.Write(b, 0, b.Length);

            //бекапим содержимое 1 файла
            string new_data = data[0];

            //загоняем образцы
            while (new_data.Length > Sample_Length)
            {
                //формируем образец
                string sample = new_data.Remove(Sample_Length);

                //сверяем со словарем
                //если есть совпадение
                int comp = dict.CompareSample(sample);
                if (comp != -1)
                {
                    //пишем сигнатуру замены
                    b = GetBytes(BORDER_KEY);
                    //tmp += BORDER_KEY;
                    fs.Write(b, 0, b.Length);

                    //и ключ
                    b = BitConverter.GetBytes(comp);
                    //tmp += comp;
                    fs.Write(b, 0, b.Length);
                }

                //если же не нашли
                else
                {
                    //пишем сигнатуру обычного куска
                    b = GetBytes(BORDER_NORM);
                    //tmp += BORDER_NORM;
                    fs.Write(b, 0, b.Length);

                    //и сам кусок
                    b = GetBytes(sample);
                    //tmp += sample;
                    fs.Write(b, 0, b.Length);
                }

                //отсекаем его от остатков
                new_data = new_data.Substring(Sample_Length);
            }
            //b = GetBytes(tmp);
            //fs.Write(b, 0, b.Length);
            fs.Flush();
            fs.Close();
            Console.WriteLine("Запись завершена.");
        }

        public static string EncryptStr(string p)
        {
            string tmp = "";
            //char[] m = p.ToCharArray();
            //int[] mm = p.ToCharArray().ToArray<int>();
            for (int i = 0; i < p.Length; i++)
            {
                tmp += string.Format("{0:X4}", (int)p[i]);
            }
            return tmp;
        }

        public static string DecryptStr(string p)
        {
            UTF8Encoding utf = new UTF8Encoding();
            string tmp = "";
            char[] m = p.ToCharArray();
            for (int i = 0; i < m.Length; i += 4)
            {
                int q = 0;
                if (Char.IsDigit(m[i])) q += m[i] - '0';
                else q += m[i] - '0' - 7;
                q *= 16;
                if (Char.IsDigit(m[i + 1])) q += m[i + 1] - '0';
                else q += m[i + 1] - '0' - 7;
                q *= 16;
                if (Char.IsDigit(m[i + 2])) q += m[i + 2] - '0';
                else q += m[i + 2] - '0' - 7;
                q *= 16;
                if (Char.IsDigit(m[i + 3])) q += m[i + 3] - '0';
                else q += m[i + 3] - '0' - 7;
                tmp += Convert.ToChar(q);
            }
            return tmp;
        }
    }
}
