using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SignRank
{
    class Program
    {
        static void Main(string[] args)
        {
            CPDailyAPI api = new CPDailyAPI();
            Dictionary<int, double> signRate = new Dictionary<int, double>();
            for (int i = 208200601; i < 208200641; i++)
            {
                var signlist = api.GetSignListOf(i);
                int total = signlist.Length;
                int signed = 0;
                foreach(CPDailyAPI.SignInfo info in signlist)
                {
                    if (info.handled) signed++;
                }
                double rate = (double)signed / (double)total;
                signRate.Add(i, rate);
                Console.WriteLine(i + "\t" + signed + "/" + total + "=\t" + rate * 100 + "%");
            }
            var ranked = signRate.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, o => o.Value);
            int ii = 0;
            Console.WriteLine("===============================================");
            foreach (KeyValuePair<int, double> item in ranked)
            {
                ii++;
                Console.WriteLine(ii + ".\t" + item.Key + "\t签到率" + item.Value * 100 + "%");
            }
            while (true)Thread.Sleep(int.MaxValue);
        }
    }
}
