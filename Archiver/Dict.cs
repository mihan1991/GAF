using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Archiver
{
    public class Dict
    {
        private int SampleLength;                   //длина образца
        private int FileLength;                     //размер файла
        private Dictionary<int, string> Temp;       //временная хэш-таблица
        private Dictionary<int, string> Table;      //основная хэш-таблица 
        private Dictionary<int, int> Repeats;       //кол-во повторений шаблона
        private int Count;                          //счетчик-ключ
        private int Sum;                            //сумма повторов
        private int Trash;                          //лишнее

        public Dict()
        {
            Count = SampleLength = 0;
            Temp = new Dictionary<int, string>();
            Repeats = new Dictionary<int, int>();
        }

        public Dict(int len, int fl)
        {
            Count = 0;
            SampleLength = len;
            FileLength = fl;
            Temp = new Dictionary<int, string>();
            Repeats = new Dictionary<int, int>();
        }

        public int GetSampleLength()
        {
            return SampleLength;
        }

        public void SetSampleLength(int len)
        {
            SampleLength = len;
        }

        public void SendSample(string str)
        {
            for (int i = 0; i < Temp.Values.Count; i++)
            {
                if (str == Temp[i])
                {
                    Repeats[i]++;
                    return;
                }
            }
            AddToDict(str);
        }

        //добавляем новую пару
        private void AddToDict(string str)
        {
            Temp.Add(Count, str);
            Repeats.Add(Count, 1);
            Count++;
        }

        public void CleanDict()
        {
            int cnt = Temp.Keys.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (Repeats[i] < 4)
                {
                    Temp.Remove(i);
                    Repeats.Remove(i);
                    //i --;
                }
            }
            Table = new Dictionary<int, string>();
            int j = 0;
            for (int i = 0; i < Count; i++)
                if (Temp.ContainsKey(i) ) 
                { 
                    Table.Add(j, Temp[i]); 
                    j++; 
                }
            Temp = null;
        }


        public void GetInfo()
        {
            Sum = 0;
            foreach (int x in Repeats.Values)
                Sum += x;
            Trash = (FileLength / SampleLength - Sum) * 4;
            Console.WriteLine(string.Format("Таблица с размером {0} содержит {1} записей и {2} повторов.\nМаксимум экономии: {3}. Лишней инфы: {4} Итого уменьшено на {5}\n", SampleLength, Table.Keys.Count, Sum, Sum * SampleLength, Trash, Sum * SampleLength - Trash));
            //GetInfo3();
        }

        public Dictionary<int, string> GetTable()
        {
            return Table;
        }

        //сравниваем строки
        public int CompareSample(string str)
        {
            for (int i = 0; i < Table.Keys.Count; i++)
            {
                if (str == Table[i])
                    return i;
            }

            //если семпла 
            return -1;
        }

        //вернуть сумму повторов
        public int GetWon()
        {
            return Sum * SampleLength - Trash;
        }
    }
}
