﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.OData
{
    public delegate void WalkGraphAction(PropertyInfo property, object obj, Stack<object> parentObjects);

    public static class SortCollectionsHelper
    {
        private static Dictionary<Type, PropertyInfo[]> SortByPropertiesPerClass = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Use reflection to idenitfy all properties with a [Sortable] attribute, and sort the elements of those collections
        /// using the value of the [SortBy] property on the type of elements of the collection.
        /// </summary>
        /// <param name="source"></param>
        public static void SortObjectGraph(this object firstNode)
        {
            if (firstNode == null)
                return;

            WalkObjectGraph<SortableAttribute>(firstNode, false, SortMembersOfPropertyOfIList);
        }

        private static void SortMembersOfPropertyOfIList(PropertyInfo prop, object source, Stack<object> parentObjects)
        {
            if (prop == null) return;

            IList list = prop.GetValue(source) as IList;
            if (list != null)
            {
                Type type = prop.PropertyType;
                Type[] genericTypes = type.GenericTypeArguments;

                if (genericTypes.Contains(typeof(Parameter))) { Console.WriteLine("Sorting parameters..."); }
                string debug = genericTypes.FirstOrDefault()?.Name;
                list.SortMembersByProperties(GetSortByProperties(genericTypes.First()));
                foreach (object o in list)
                {
                    WalkObjectGraph<SortableAttribute>(o, false, SortMembersOfPropertyOfIList, parentObjects);
                }
            }
        }

        public static void RenameTypeInObjectGraph(this object firstNode, string oldTypeName, string newTypeName)
        {
            if (firstNode == null)
                return;

            WalkObjectGraph<Transformation.ContainsTypeAttribute>(firstNode, true, (prop, source, parentObjects) =>
            {
                if (prop == null) return;

                string value = (string)prop.GetValue(source);
                if (null != value)
                {
                    var dataType = value.ParseParameterDataType();

                    if (dataType.CustomTypeName != null && dataType.CustomTypeName == oldTypeName)
                    {
                        dataType.CustomTypeName = newTypeName;
                        prop.SetValue(source, dataType.ToString());
                    }
                    else if (dataType.CustomTypeName != null && dataType.CustomTypeName.EndsWith($".{oldTypeName}"))
                    {
                        dataType.CustomTypeName = dataType.CustomTypeName.Substring(0, dataType.CustomTypeName.Length - oldTypeName.Length) + newTypeName;
                        prop.SetValue(source, dataType.ToString());
                    }
                }
            });
        }

        public static void WalkObjectGraph(object currentNode, bool walkCollections, WalkGraphAction action, Stack<object> objectStack, Func<PropertyInfo, bool> performActionOnProperty, Func<Type, bool> interestingTypeFunction = null)
        {
            if (currentNode == null)
            {
                return;
            }

            if (null == objectStack)
            {
                // Maintain a stack of objects in the hierarchy so we don't get stuck processing loops in the graph
                objectStack = new Stack<object>();
            }
            else if (objectStack.Contains(currentNode))
            {
                // We've already walked this object, so we have a circular reference, let's not overflow the stack.
                return;
            }

            objectStack.Push(currentNode);

            interestingTypeFunction = interestingTypeFunction ?? (t => true);

            // Act on the current object
            if (interestingTypeFunction(currentNode.GetType()))
            {
                action(null, currentNode, objectStack);
            }

            // Walk down into the object graph for properties that have objects
            var objectProperties = currentNode.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in objectProperties)
            {
                // Skip read or write only properties
                if (!prop.CanRead || !prop.CanWrite) { continue; }

                // Check to see if we should perform the action() on this property
                if (performActionOnProperty(prop) && interestingTypeFunction(prop.GetType()))
                {
                    action(prop, currentNode, objectStack);
                }

                // Check to see if this property holds an object we can walk into 
                object value;
                try
                {
                    value = prop.GetValue(currentNode);
                }
                catch (Exception)
                {
                    continue;
                }

                if (null != value)
                {
                    if (walkCollections && value is IEnumerable && !(value is string))
                    {
                        foreach (var obj in (IEnumerable)value)
                        {
                            WalkObjectGraph(obj, walkCollections, action, objectStack, performActionOnProperty, interestingTypeFunction);
                        }
                    }
                    else if (!(value is IEnumerable))
                    {
                        WalkObjectGraph(value, walkCollections, action, objectStack, performActionOnProperty, interestingTypeFunction);
                    }
                }
            }
            objectStack.Pop();
        }

        internal static void WalkObjectGraph<TAttribute>(object source, bool intoCollections, WalkGraphAction action, Stack<object> parentObjectStack = null ) 
            where TAttribute : Attribute
        {
            WalkObjectGraph(source, intoCollections, action, parentObjectStack, p => p.GetCustomAttributes<TAttribute>(true).Any(), null);
        }

        private static void SortMembersByProperties(this IList list, PropertyInfo[] properties)
        {
            ArrayList.Adapter(list).Sort(new PropertyListComparer(properties));
        }

        class PropertyListComparer : IComparer
        {
            private PropertyInfo[] Properties { get; set; }

            public PropertyListComparer(PropertyInfo[] sortByProps)
            {
                System.Diagnostics.Debug.Assert(sortByProps.Any(), "Cannot sort without any properties in the sortByProps collection");
                Properties = sortByProps;
            }

            public int Compare(object x, object y)
            {
                foreach(var prop in Properties)
                {
                    IComparable x_value = (IComparable)prop.GetValue(x);
                    IComparable y_value = (IComparable)prop.GetValue(y);

                    if (x_value == null && y_value != null)
                    {
                        return 1;
                    }
                    else if (x_value != null && y_value == null)
                    {
                        return -1;
                    }
                    else if (x_value == null && y_value == null)
                    {
                        continue;
                    }
                    else
                    {
                        int result = x_value.CompareTo(y_value);
                        if (result != 0)
                        {
                            return result;
                        }
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// Return a collection of properties attributed with the SortByAttribute and ordered based on SortOrderIndex
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static PropertyInfo[] GetSortByProperties(Type t)
        {
            PropertyInfo[] output;
            if (SortByPropertiesPerClass.TryGetValue(t, out output))
            {
                return output;
            }

            var sortByProperties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetCustomAttributes<SortByAttribute>(true).Any()).ToArray();
            if (!sortByProperties.Any())
            {
                Console.WriteLine($"No sortBy properties were defined for {t.Name}.");
            }

            // Sort the sortable properties based on the sortOrderIndex,  so we consider them in the proper order.
            SortByPropertiesPerClass[t] = sortByProperties.OrderBy(x => x.GetCustomAttribute<SortByAttribute>(true).SortOrderIndex).ToArray();
            return sortByProperties;
        }
    }

    /// <summary>
    /// Apply this attribute to properties which contains collections or objects that contain collection
    /// to indicate that the sort logic should walk down into this object graph.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SortableAttribute : Attribute
    {

    }

    /// <summary>
    /// indicates that this field should be included in the set of properties to sort by
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SortByAttribute : Attribute
    {
        /// <summary>
        /// Setting SortOrderIndex allows you to control how multiple properties 
        /// tagged SortBy are considered when sorting a collection of objects.
        /// </summary>
        public int SortOrderIndex { get; set; }

        public SortByAttribute()
        {
            this.SortOrderIndex = Int32.MaxValue;
        }

        public SortByAttribute(int sortOrderIndex)
        {
            this.SortOrderIndex = sortOrderIndex;
        }
    }
}
