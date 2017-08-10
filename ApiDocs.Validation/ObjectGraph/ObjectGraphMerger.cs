using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.Utility
{
    public class ObjectGraphMerger<T> where T : new()
    {
        public T Result { get; set; }

        public T[] Sources { get; private set; }

        public ObjectGraphMerger(params T[] objectGraphs)
        {
            this.Sources = objectGraphs;
            this.Result = new T();
        }

        /// <summary>
        /// Merge the frameworks in the Sources collection together into the Result
        /// </summary>
        public T Merge()
        {
            this.Result = (T)MergeNodes(this.Sources.Cast<object>());
            return this.Result;
            }

        private object MergeNodes(IEnumerable<object> nodesToMerge)
        {
            if (!nodesToMerge.Any())
                return null;

            Type type = nodesToMerge.First().GetType();
            VerifyNodesAreOfType(nodesToMerge, type);

            var result = CreateInstanceOfType(type);

            var propertyMap = GetMergerPropertyMap(type);
            foreach(var mapping in propertyMap)
            {
                var values = (from node in nodesToMerge
                              where node != null
                              select mapping.Property.GetValue(node));

                if (mapping.IsCollection)
                {
                    // We need to find and merge values within the collection, dedupe them by their CollectionIdentifier
                    // and then merge objects with the same identifier
                    var collectionMembers = EnumerableAllCollectionMembers(values);
                    var resultingMembers = DedupeMembersInCollection(mapping, collectionMembers);
                    SetCollectionProperty(result, mapping.Property, resultingMembers.ToArray());
                }
                else if (mapping.IsSimpleType)
                {
                    object mergedCollection = CalculateMergedValueForMapping(mapping, values);
                    mapping.Property.SetValue(result, mergedCollection);
                }
                else
                {
                    // We have complex objects at this level of hierarchy that we can't resolve, so we need to merge those nodes
                    object mergedValue = MergeNodes(values.Where(x => x != null));
                    mapping.Property.SetValue(result, mergedValue);
                }
            }
            return result;
        }

        private void SetCollectionProperty(object obj, PropertyInfo collectionProperty, object[] valueToSet)
        {
            var type = collectionProperty.PropertyType;
            if (type.IsArray)
            {
                Type innerType = type.GetElementType();
                var array = Array.CreateInstance(innerType, valueToSet.Length);
                for (int i = 0; i <valueToSet.Length; i++)
                {
                    array.SetValue(valueToSet[i], i);
                }
                collectionProperty.SetValue(obj, array);
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                
                IList list = CreateInstanceOfType(type) as IList;
                for(int i=0; i<valueToSet.Length; i++)
                {
                    list.Add(valueToSet[i]);
                }
                collectionProperty.SetValue(obj, list);
            }
            else
            {
                throw new NotSupportedException($"Unable to set collection property of type {type.Name}.");
            }
        }

        private IEnumerable<object> DedupeMembersInCollection(MergerConditions mapping, IEnumerable<object> members)
        {
            Dictionary<object, object> equivelentKeyValues = new Dictionary<object, object>();
            var policyAttributeOnIdentifier = mapping?.CollectionIdentifierProperty?.GetCustomAttribute<MergePolicyAttribute>();
            if (null != policyAttributeOnIdentifier)
            {
                equivelentKeyValues = ParseEquivalentValues(policyAttributeOnIdentifier.EquivalentValues);
            }

            // Dedupe the objects based on their collection identifier value
            Dictionary<string, List<object>> uniqueMembers = new Dictionary<string, List<object>>();
            foreach(var obj in members)
            {
                if (null == mapping.CollectionIdentifierProperty)
                {
                    throw new ObjectGraphMergerException($"Missing a collection identifier for class {mapping.CollectionInnerType.Name} referenced from {mapping.Property.DeclaringType.Name}.{mapping.Property.Name}.");
                }
                string key = (string)mapping.CollectionIdentifierProperty.GetValue(obj);

                // Check to see if there is a value we should use instead of this one
                object replacementKey = null;
                if (equivelentKeyValues.TryGetValue(key, out replacementKey))
                {
                    key = (string)replacementKey;
                }

                List<object> knownMembersWithKey;
                if (!uniqueMembers.TryGetValue(key, out knownMembersWithKey))
                {
                    knownMembersWithKey = new List<object>();
                    uniqueMembers.Add(key, knownMembersWithKey);
                }
                knownMembersWithKey.Add(obj);
            }
            
            // Merge any elements where a duplicate key exists
            foreach(var key in uniqueMembers)
            {
                if (key.Value.Count > 1)
                {
                    var mergedNode = MergeNodes(key.Value.ToArray());
                    key.Value.Clear();
                    key.Value.Add(mergedNode);
                }
            }

            // Return the single value from each key member
            return (from m in uniqueMembers select m.Value.Single());
        }

        private static bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<object> EnumerableAllCollectionMembers(IEnumerable<object> collections)
        {
            foreach(IEnumerable<object> collection in collections)
            {
                if (collection != null)
                {
                    foreach (var obj in collection)
                    {
                        yield return obj;
                    }
                }
            }
        }

        private object CalculateMergedValueForMapping(MergerConditions mapping, IEnumerable<object> values)
        {
            if (!mapping.IsSimpleType)
            {
                throw new ObjectGraphMergerException("Attempted to calculate merged value for a non-value type map.");
            }

            switch (mapping.Policy)
            {
                case MergePolicy.EqualOrNull:
                case MergePolicy.Default:
                    {
                        // See if we need to do any mapping of values to their replacements.                        
                        if (mapping.EquivalentValues.Any())
                        {
                            object[] knownValues = values.ToArray();
                            for(int i=0; i<knownValues.Length; i++)
                            {
                                object replacementValue;
                                if (mapping.EquivalentValues.TryGetValue(knownValues[i], out replacementValue))
                                {
                                    knownValues[i] = replacementValue;
                                }
                            }
                            
                            // Replace the enumerable collection with our modified version
                            values = knownValues;
                        }

                        object knownValue = values.FirstOrDefault(x => x != null);
                        if (values.Any(x => x != null && !x.Equals(knownValue)))
                        {
                            throw new ObjectGraphMergerException($"Unable to merge values for {mapping.Property.DeclaringType.Name}.{mapping.Property.Name} because values are not equal or null. Values: {values}.");
                        }
                        return knownValue;
                    }
                case MergePolicy.PreferGreaterValue:
                    return values.OrderByDescending(x => x).FirstOrDefault();

                case MergePolicy.PreferLesserValue:
                    return values.OrderBy(x => x).FirstOrDefault();
                default:
                    throw new NotImplementedException($"Unsupported merge policy value: {mapping.Policy}.");
            }
        }

        private static Dictionary<Type, MergerConditions[]> knownMergerPropertyMaps = new Dictionary<Type, MergerConditions[]>();
        private static MergerConditions[] GetMergerPropertyMap(Type type)
        {
            if (type.GetCustomAttribute(typeof(MergableAttribute), true) == null)
            {
                throw new ObjectGraphMergerException($"Object type {type.Name} is not marked with the Mergable attribute.");
            }

            MergerConditions[] result = null;
            if (!knownMergerPropertyMaps.TryGetValue(type, out result))
            {
                // Generate the map for this type and cache it
                List<MergerConditions> conditions = new List<MergerConditions>();
                var mergableProperties = type.GetProperties().Where(p => p.CanRead && p.CanWrite);
                foreach(var prop in mergableProperties)
                {
                    var policyAttrib = prop.GetCustomAttribute<MergePolicyAttribute>(true);
                    var policy = policyAttrib?.Policy ?? MergePolicy.Default;
                    if (policy != MergePolicy.Ignore)
                    {
                        var condition = new MergerConditions { Policy = policy };
                        condition.EquivalentValues = ParseEquivalentValues(policyAttrib?.EquivalentValues);
                        if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                        {
                            condition.IsCollection = true;
                            // Determine the class type contained by this collection
                            var collectionType = prop.PropertyType;
                            if (collectionType.IsGenericType)
                            {
                                condition.CollectionInnerType = collectionType.GenericTypeArguments.First();
                            }
                            else
                            {
                                condition.CollectionInnerType = collectionType.GetElementType();
                            }
                            condition.CollectionIdentifierPropertyName = condition.CollectionInnerType?.GetCustomAttribute<MergableAttribute>(true)?.CollectionIdentifier;
                            if (!string.IsNullOrEmpty(condition.CollectionIdentifierPropertyName))
                            {
                                condition.CollectionIdentifierProperty = condition.CollectionInnerType?.GetProperty(condition.CollectionIdentifierPropertyName);
                            }
                        }
                        condition.IsSimpleType = IsSimple(prop.PropertyType);
                        condition.Property = prop;
                        conditions.Add(condition);
                    }
                }
                result = conditions.ToArray();
                knownMergerPropertyMaps.Add(type, result);
            }
            return result;
        }

        private static Dictionary<object, object> ParseEquivalentValues(string equivalentValues)
        {
            var output = new Dictionary<object, object>();
            if (equivalentValues != null)
            {
                var values = System.Web.HttpUtility.ParseQueryString(equivalentValues);
                foreach (var key in values.AllKeys)
                {
                    output[key] = values[key];
                }
            }
            return output;
        }

        private class MergerConditions
        {
            public PropertyInfo Property { get; set; }
            public MergePolicy Policy { get; set; }
            public Dictionary<object, object> EquivalentValues { get; set; }
            public bool IsCollection { get; set; }
            public Type CollectionInnerType { get; set; }
            public bool IsSimpleType { get; set; }
            public string CollectionIdentifierPropertyName { get; internal set; }
            public PropertyInfo CollectionIdentifierProperty { get; internal set; }
        }

        private static object CreateInstanceOfType(Type type)
        {
            object result = null;
            ConstructorInfo constructor = type.GetConstructor(new Type[] { });
            if (null == constructor)
            {
                throw new ObjectGraphMergerException($"Types being merged must have a parameterless public constructor: {type.Name}");
            }

            try
            {
                result = constructor.Invoke(new object[] { });
            }
            catch (Exception ex)
            {
                throw new ObjectGraphMergerException($"Unable to create a new instance of type {type.Name}.", ex);
            }

            return result;
        }

        private static void VerifyNodesAreOfType(IEnumerable<object> nodesToMerge, Type type)
        {
            if (nodesToMerge.Any(x => x != null && x.GetType() != type))
            {
                throw new ObjectGraphMergerException("Nodes to merge must all be the same class type.");
            }
        }



        //private void RecordFrameworkObject(PropertyInfo prop, object obj, Stack<object> parentObjects)
        //{
        //    if (prop != null) return;

        //    var sourceItem = obj as ITransformable;
        //    if (sourceItem != null)
        //    {
        //        var identifier = GenerateUniqueIdentifier(sourceItem, parentObjects);


        //        Type knownIdentiiferType = null;
        //        if (this.UniqueObjectIdentifiers.TryGetValue(identifier, out knownIdentiiferType))
        //        {
        //            if (knownIdentiiferType != sourceItem.GetType())
        //            {
        //                Console.WriteLine("Unique identifier found that references two different object types. That must be a bug.");
        //            }
        //        }
        //        else
        //        {
        //            this.UniqueObjectIdentifiers.Add(identifier, sourceItem.GetType());
        //        }
        //    }
        //}

        //private string GenerateUniqueIdentifier(ITransformable obj, Stack<object> parentObjects)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (var parent in parentObjects)
        //    {
        //        string identifier = (parent as ITransformable)?.ElementIdentifier;
        //        identifier = identifier ?? parent.GetType().Name;
        //        sb.Insert(0, identifier);
        //        sb.Insert(0, ".");
        //    }
        //    var output = sb.ToString().Substring(1);
        //    if (obj?.ElementIdentifier != null && !output.EndsWith(obj.ElementIdentifier))
        //    {
        //        output = output + $".{obj.ElementIdentifier}";
        //    }
        //    return output;
        //}
    }

    public class ObjectGraphMergerException : Exception
    {
        public ObjectGraphMergerException(string message) : base(message)
        {

        }

        public ObjectGraphMergerException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class MergableAttribute : Attribute
    {
        public string CollectionIdentifier { get; set; }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MergePolicyAttribute : Attribute
    {
        public MergePolicy Policy { get; set; }

        public string EquivalentValues { get; set; }
        
        public MergePolicyAttribute()
        {
            this.Policy = MergePolicy.Default;
        }

        public MergePolicyAttribute(MergePolicy policy)
        {
            this.Policy = policy;
        }
    }

    public enum MergePolicy
    {
        Default = 0,
        
        /// <summary>
        /// Indiciates that values in the objects being merged must be equal to each other or be null to have a successful merge.
        /// </summary>
        EqualOrNull = 1,

        /// <summary>
        /// The value in the objects being merged is ignored and not merged.
        /// </summary>
        Ignore = 2,

        PreferGreaterValue = 3,
        PreferTrueValue = 3,


        PreferLesserValue = 4,
        PreferFalseValue = 4,

        MustBeNull = 5,
    }
}
