using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;

namespace ComparitorTest
{
    public interface IDifferentiable<T>
    {
        List<PropertyInfo> GetDifferingProperties(T other, params string[] ignoreList);
    }

    public class Class1 : Base<Class1>
    {
        public int Property1 { get; set; }
        public int? Property2 { get; set; }
        public string Property3 { get; set; }
        public int Property4 { get; set; }
        public List<Class2> ChildColl1 { get; set; }
        public Class3 Var3 { get; set; }

        public Class1()
        {
            Var3 = new Class3() { C3Property1 = 6, C3Property2 = 3, C3Property3 = "Gey", C3Property4 = 5 };
            Property1 = 10;
            Property3 = "TestString";
            Property4 = 2;
            ChildColl1 = new List<Class2>()
            {
                new Class2(10, 3, "BeBah", 16),
                new Class2(3, 3, null, 11),
                new Class2(15, null, "BeBahttt", 1),
                new Class2(11121, 3, "Michail", 15)
            };
        }
    }

    public class Class2 : Base<Class2>
    {
        public int C2Property1 { get; set; }
        public int? C2Property2 { get; set; }
        public string C2Property3 { get; set; }
        public int C2Property4 { get; set; }
        public List<Class3> ChildColl2 { get; set; }
        public List<int> IntList { get; set; }

        public Class2(int p1, int? p2, string p3, int p4)
        {
            C2Property1 = p1;
            C2Property2 = p2;
            C2Property3 = p3;
            C2Property4 = p4;

            ChildColl2 = new List<Class3>()
            {
                new Class3(){ C3Property1 = 6, C3Property2 = 3, C3Property3 = "Gey", C3Property4 = 5 },
                new Class3(){ C3Property1 = 5, C3Property2 = 1, C3Property3 = "", C3Property4 = 4 },
                new Class3(){ C3Property1 = 3, C3Property2 = null, C3Property3 = null, C3Property4 = 5 }
            };

            IntList = new List<int>();
            IntList.Add(5);
            IntList.Add(5);
            IntList.Add(5);
            IntList.Add(7);
            IntList.Add(5);
            IntList.Add(8);
        }
    }

    public class Class3 : Base<Class3>
    {
        public int C3Property1 { get; set; }
        public int? C3Property2 { get; set; }
        public string C3Property3 { get; set; }
        public int C3Property4 { get; set; }
    }

    public abstract class Base<T> : IDifferentiable<T>
    {
        /// <summary>
        /// This function compares all the public properties of the current instance against another instance passed in as an argument.
        /// If a public property is a collection that implements ref="IDifferentiable<T>", an attempt to compare the collection is made (recursively).
        /// If a public property is a collection of primitives (aka ValueTypes) or strings then they will be compared.
        /// </summary>
        /// <param name="other">The other instance to compare against</param>
        /// <param name="ignoreList">A list of property names to ignore when comparing (NOTE: the ignore list is passed down the tree to each node when comparing nested collections)</param>
        /// <returns>A List<PropertyInfo> of public properties of this instance that have different values compared to the other instance passed in to the function.</returns>
        public List<PropertyInfo> GetDifferingProperties(T other, params string[] ignoreList)
        {
            Debug.WriteLine("Current Instance Type is a " + this.GetType().Name);
            List<PropertyInfo> diffs = new List<PropertyInfo>();

            foreach (var prop in this.GetType().GetProperties())
            {
                if (ignoreList.Any() && ignoreList.Contains(prop.Name))
                    continue;

                object thisValue = prop.GetValue(this, null);
                object otherValue = prop.GetValue(other, null);

                if (thisValue == null && otherValue == null)
                    continue;

                if((thisValue == null && otherValue != null) || (thisValue != null && otherValue == null))
                {
                    diffs.Add(prop);
                    continue;
                }

                //Check if Property is a generic collection
                if (prop.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType.GetGenericTypeDefinition()))
                {
                    IList thisRefList = (IList)thisValue;
                    IList otherRefList = (IList)otherValue;

                    // If the two collections have a different amount of elements - then that's good enough
                    // for us to assume 'something' has changed - and we can save ourselves the pain of checking ourselves
                    if(thisRefList.Count != otherRefList.Count)
                    {
                        diffs.Add(prop);
                        continue;
                    }

                    // Get the collections <T>
                    Type collectionGenericT = prop.PropertyType.GetGenericArguments()[0];

                    // Check if the generic T of the collection implments IDifferentiable<T>
                    if (collectionGenericT.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDifferentiable<>)))
                    {
                        // So here - the assumption is that each element of our collection implements IDifferentiable<T>. And they're in the same order.
                        // This should mean that we can call GetDifferingProperties(other) on each element and it should tell
                        // us the amount of differences it has- If the <T> itself has a (or many) properties that themselves are
                        // generic collections where T : IDifferentiable<Some other T> then we recursively call GetDifferingProperties(other)
                        // on those too. The ending leaf will be a IDifferentiable<T> : where T != : IDifferentiable<Some other T>.
                        // Of course we only need one thing to be different, so we break out once we find a single thing that has changed in the tree.
                        for (int i = 0; i < thisRefList.Count; i++)
                        {
                            try
                            {
                                if (((List<PropertyInfo>)collectionGenericT.GetMethod("GetDifferingProperties").Invoke(thisRefList[i], new object[] { otherRefList[i], ignoreList })).Count > 0)
                                {
                                    diffs.Add(prop);
                                    break;
                                }
                            }
                            catch (Exception e) { Debug.WriteLine(e.Message); }
                        }
                        continue;
                    }
                    else
                    {
                        // Here we have a collection<T>, but the T is not IDifferentiable<T>.
                        // If <T> is some primative or string then we can compare because ToString() should return the 
                        // same thing for a ValueType as an object - but if its a ref type we can't  - so move on.
                        if (collectionGenericT.IsValueType || collectionGenericT == typeof(String))
                        {
                            for (int i = 0; i < thisRefList.Count; i++)
                            {
                                if(thisRefList[i].ToString() != otherRefList[i].ToString())
                                {
                                    diffs.Add(prop);
                                    break;
                                }
                            }
                        }
                        continue;
                    }
                }

                Type t = prop.PropertyType;

                // Check for a ValueType or String
                if (t.IsValueType || t == typeof(String))
                {
                    if (!thisValue.Equals(otherValue))
                        diffs.Add(prop);

                    continue;
                }

                // Check for a IDifferentiable<T> (non-collection)
                if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDifferentiable<>)))
                {
                    try
                    {
                        if (((List<PropertyInfo>)t.GetMethod("GetDifferingProperties").Invoke(thisValue, new object[] { otherValue, ignoreList })).Count > 0)
                            diffs.Add(prop);
                    }
                    catch (Exception e) { Debug.WriteLine(e.Message); }
                }
            }
            return diffs;
        }
    }
}
