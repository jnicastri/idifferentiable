using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ComparitorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Class1 v1 = new Class1();
            Class1 v2 = new Class1();
            // + 1
            v1.Property1 = 250;

            List<PropertyInfo> d = v1.GetDifferingProperties(v2);

            Console.WriteLine("Expected 1: Count = " + d.Count);
            PrintColl(d);
            // - 1
            v1.Property1 = v2.Property1;
            // + 2
            v2.Property4 = 9999;
            v1.Property3 = "Shiskbab";

            d = v1.GetDifferingProperties(v2);

            Console.WriteLine("Expected 2: Count = " + d.Count);
            PrintColl(d);
             // + 1
            v2.ChildColl1[1].C2Property2 = 2500;

            d = v1.GetDifferingProperties(v2);

            Console.WriteLine("Expected 3: Count = " + d.Count);
            PrintColl(d);

            // - 1
            v2.ChildColl1[1].C2Property2 = v1.ChildColl1[1].C2Property2;

            // + 1
            v1.ChildColl1[3].ChildColl2.RemoveAt(0);

            d = v1.GetDifferingProperties(v2);

            Console.WriteLine("Expected 3: Count = " + d.Count);
            PrintColl(d);

            // + 1
            v2.ChildColl1[0].ChildColl2.Clear();
            v2.Property4 = v1.Property4;

            d = v1.GetDifferingProperties(v2);

            Console.WriteLine("Expected 2: Count = " + d.Count);
            PrintColl(d);

           

        }

        public static void PrintColl(List<PropertyInfo> l)
        {
            foreach (var i in l)
                Console.WriteLine(i.Name);


            Console.WriteLine();
            Console.WriteLine();
        }

    }
}
