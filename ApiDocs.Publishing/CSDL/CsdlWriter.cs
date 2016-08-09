/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Publishing.CSDL
{
    using ApiDocs.Validation;
    using ApiDocs.Validation.Writers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDocs.Validation.OData;
    using System.Diagnostics;
    public class CsdlWriter : DocumentPublisher
    {
        private readonly string[] validNamespaces;
        private readonly string baseUrl;
        private readonly CsdlWriterSettings settings;

        public CsdlWriter(DocSet docs, string[] namespacesToExport, string baseUrl)
            : base(docs)
        {
            this.validNamespaces = namespacesToExport;
            this.baseUrl = baseUrl;

            var config = DocSet.TryLoadConfigurationFiles<CsdlWriterConfigFile>(docs.SourceFolderPath).SingleOrDefault();
            if (null != config)
            {
                settings = config.CsdlWriterSettings;
            }
            else
            {
                settings = new CsdlWriterSettings();
            }
        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            // Step 1: Generate an EntityFramework OM from the documentation
            EntityFramework framework = CreateEntityFrameworkFromDocs(this.baseUrl);

            // Step 2: Generate XML representation of EDMX
            var xmlData = ODataParser.GenerateEdmx(framework, !settings.IncludeXmlDeclaration, settings.IndentXml);

            // Step 3: Write the XML to disk
            var outputDir = new System.IO.DirectoryInfo(outputFolder);
            outputDir.Create();

            var outputFilename = System.IO.Path.Combine(outputFolder, settings.OutputFilename);
            using (var writer = System.IO.File.CreateText(outputFilename))
            {
                await writer.WriteAsync(xmlData);
                await writer.FlushAsync();
                writer.Close();
            }
        }

        private EntityFramework CreateEntityFrameworkFromDocs(string baseUrlToRemove)
        {
            var edmx = new EntityFramework();
            
            // Add resources
            foreach (var resource in Documents.Resources)
            {
                var targetSchema = FindOrCreateSchemaForNamespace(resource.Name.NamespaceOnly(), edmx);
                if (targetSchema != null)
                {
                    AddResourceToSchema(targetSchema, resource, edmx);
                }
            }

            // Figure out the EntityCollection
            this.BuildEntityCollection(edmx, baseUrlToRemove);

            // Add actions to the collection
            this.ProcessRestRequestPaths(edmx, baseUrlToRemove);

            if (settings.ExcludedNamespaces.Any())
            {
                edmx.DataServices.Schemas.RemoveAll(x => settings.ExcludedNamespaces.Contains(x.Namespace));
            }
            return edmx;
        }

        /// <summary>
        /// Scan the MethodDefintions in the documentation and create actions and functions in the 
        /// EntityFramework for matching call patterns.
        /// </summary>
        /// <param name="edmx"></param>
        private void ProcessRestRequestPaths(EntityFramework edmx, string baseUrlToRemove)
        {
            Dictionary<string, MethodCollection> uniqueRequestPaths = GetUniqueRequestPaths(baseUrlToRemove);


            foreach (var path in uniqueRequestPaths.Keys)
            {
                var methodCollection = uniqueRequestPaths[path];

                ODataTargetInfo requestTarget = null;
                try
                {
                    requestTarget = ParseRequestTargetType(path, methodCollection, edmx);
                    if (requestTarget.Classification == ODataTargetClassification.Unknown &&
                        !string.IsNullOrEmpty(requestTarget.Name) &&
                        requestTarget.QualifiedType != null)
                    {
                        CreateNewActionOrFunction(edmx, methodCollection, requestTarget);
                    }
                    else if (requestTarget.Classification == ODataTargetClassification.EntityType || 
                        requestTarget.Classification == ODataTargetClassification.EntitySet)
                    {
                        // We've learned more about this entity type, let's add that information to the state
                        AppendToEntityType(edmx, requestTarget, methodCollection);
                    }
                    else if (requestTarget.Classification == ODataTargetClassification.NavigationProperty)
                    {
                        // TODO: Record somewhere the operations that are available on this NavigationProperty
                        AppendToNavigationProperty(edmx, requestTarget, methodCollection);
                    }
                    else
                    {
                        // TODO: Are there interesting things to learn here?
                        Console.WriteLine("Found type {0}: {1}", requestTarget.Classification, path);
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Log this out better than this.
                    Console.WriteLine("Couldn't serialize request for path {0} into EDMX: {1}", path, ex.Message);
                    continue;
                }
            }
        }

        private void AppendToNavigationProperty(EntityFramework edmx, ODataTargetInfo navigationProperty, MethodCollection methods)
        {
            NavigationProperty matchingProperty = null;

            //if (navigationProperty is ODataCollection)
            //{
            //    var collection = navigationProperty.

            //}
            //else
            //{
                EntityType parentType = edmx.ResourceWithIdentifier<EntityType>(navigationProperty.QualifiedType);
                matchingProperty = parentType.NavigationProperties.FirstOrDefault(np => np.Name == navigationProperty.Name);
            //}
                
            if (null != matchingProperty)
            {
                // Add the InsertRestrctions annotation
                AddAnnotationTermAndRecord(matchingProperty, Term.InsertRestrictionsTerm, PropertyValue.InsertableProperty, methods.PostAllowed);

                // Add the UpdateRestrictions annotation
                AddAnnotationTermAndRecord(matchingProperty, Term.UpdateRestrictionsTerm, PropertyValue.UpdatableProperty, methods.PutAllowed);

                // Add the DeleteRestrictions annotation
                AddAnnotationTermAndRecord(matchingProperty, Term.DeleteRestrictionsTerm, PropertyValue.DeletableProperty, methods.DeleteAllowed);

                // Add ExpandRestrictions annotation
                //AddAnnotationTermAndRecord(matchingProperty, Term.ExpandRestrictionsTerm, PropertyValue.ExpandableProperty, matchingProperty.Expandable);

                // Add FilterStrictions annotation
                //AddAnnotationTermAndRecord(matchingProperty, Term.FilterRestrictionsTerm, PropertyValue.FilterableProperty, matchingProperty.Filterable);
            }
            else
            {
                Console.WriteLine(
                    "EntityType '{0}' doesn't have a matching navigationProperty '{1}' but a request exists for this. Sounds like a documentation error.",
                    navigationProperty.QualifiedType,
                    navigationProperty.Name);
            }
        }

        private void AddAnnotationTermAndRecord(NavigationProperty target, string term, string propertyName, bool value)
        {
            if (null == target.Annotation)
            {
                target.Annotation = new List<Annotation>();
            }

            var annotation = target.Annotation.Where(x => x.Term == term).FirstOrDefault();
            if (null == annotation)
            {
                annotation = new Annotation { Term = term, Records = new List<Record>() };
                target.Annotation.Add(annotation);
            }


            // Check to see if this record already exists before we add it
            var record = annotation.Records.Where(x => x.PropertyValue.Property == propertyName).FirstOrDefault();
            if (null != record)
            {
                // We OR the values together, because if someone thought we could do this somewhere, we can do it anywhere
                record.PropertyValue.Bool = record.PropertyValue.Bool | value;
                record.PropertyValue.BoolSpecified = true;
            }
            else
            {
                annotation.Records.Add(new Record { PropertyValue = new PropertyValue { Property = propertyName, Bool = value, BoolSpecified = true } });
            }
        }

        /// <summary>
        /// This method should be used for TopSupported, SkipSupported
        /// </summary>
        /// <param name="target"></param>
        /// <param name="term"></param>
        /// <param name="value"></param>
        private void AddAnnotationTerm(NavigationProperty target, string term, bool value)
        {
            Debug.Assert(term.Equals(Term.TopSupportedTerm) || term.Equals(Term.SkipSupportedTerm), $"Unsupported term value specified: {term}");

            // Check to see if this annotation already exists
            var annotation = target.Annotation.Where(x => x.Term == term).FirstOrDefault();
            if (null != annotation)
            {
                annotation.Bool = annotation.Bool | value;
                annotation.BoolSpecified = true;
            }
            else
            {
                annotation = new Annotation { Term = term, Bool = value, BoolSpecified = true };

                if (target.Annotation == null)
                {
                    target.Annotation = new List<Annotation>();
                }
                target.Annotation.Add(annotation);
            }
        }

        /// <summary>
        /// Use the properties of methodCollection to augment what we know about this entity type
        /// </summary>
        /// <param name="requestTarget"></param>
        /// <param name="methodCollection"></param>
        private void AppendToEntityType(EntityFramework edmx, ODataTargetInfo requestTarget, MethodCollection methodCollection)
        {
            StringBuilder sb = new StringBuilder();
            const string seperator = ", ";
            sb.AppendWithCondition(methodCollection.GetAllowed, "GET", seperator);
            sb.AppendWithCondition(methodCollection.PostAllowed, "POST", seperator);
            sb.AppendWithCondition(methodCollection.PutAllowed, "PUT", seperator);
            sb.AppendWithCondition(methodCollection.DeleteAllowed, "DELETE", seperator);

            var singleton = requestTarget.Target as Singleton;
            if (null != singleton)
            {
                // TODO: Figure out what attributes we want to write to a singelton
            }

            var entitySet = requestTarget.Target as EntitySet;
            if (null != entitySet)
            {
                // TODO: figure out what attributes we want to write to an entitySet
                if (requestTarget.Name == "{var}")
                {
                    if (methodCollection.GetAllowed)
                    {
                        // queryable for OneDrive SDK
                        if (entitySet.Annotation == null) entitySet.Annotation = new List<Annotation>();
                        entitySet.Annotation.Add(new Annotation { Term = "Com.Microsoft.Graph.Queryable", Bool = true, BoolSpecified =true });
                    }
                    else if (methodCollection.PostAllowed)
                    {
                        // writable for OneDrive SDK
                        if (entitySet.Annotation == null) entitySet.Annotation = new List<Annotation>();
                        entitySet.Annotation.Add(new Annotation { Term = "Com.Microsoft.Graph.Writable", Bool = true, BoolSpecified = true });
                    }
                    else if (methodCollection.DeleteAllowed)
                    {
                        // deletable for OneDrive SDK
                        if (entitySet.Annotation == null) entitySet.Annotation = new List<Annotation>();
                        entitySet.Annotation.Add(new Annotation { Term = "Com.Microsoft.Graph.Deletable", Bool = true, BoolSpecified = true });
                    }
                }
                else
                {
                    if (methodCollection.GetAllowed)
                    {
                        // enumerable for OneDrive SDK
                        if (entitySet.Annotation == null) entitySet.Annotation = new List<Annotation>();
                        entitySet.Annotation.Add(new Annotation { Term = "Com.Microsoft.Graph.Enumerable", Bool = true, BoolSpecified = true });
                    }
                }
            }

            Console.WriteLine("EntityType '{0}' supports: ({1})", requestTarget.QualifiedType, sb.ToString());
        }

        private void CreateNewActionOrFunction(EntityFramework edmx, MethodCollection methodCollection, ODataTargetInfo requestTarget)
        {
            // Create a new action (not idempotent) / function (idempotent) based on this request method!
            ActionOrFunctionBase target = null;
            if (methodCollection.AllMethodsIdempotent)
            {
                target = new Validation.OData.Function();
            }
            else
            {
                target = new Validation.OData.Action();
            }

            string schemaName = requestTarget.Name.NamespaceOnly();
            target.Name = requestTarget.Name.TypeOnly();

            if (!string.IsNullOrEmpty(settings.FlattenActionsToNamespace))
            {
                target.Name = $"{schemaName}.{target.Name}";
                schemaName = settings.FlattenActionsToNamespace;
            }

            target.IsBound = true;
            target.Parameters.Add(new Parameter { Name = "bindingParameter", Type = requestTarget.QualifiedType, Nullable = false });
            foreach (var param in methodCollection.RequestBodyParameters)
            {
                var newParameter = new Parameter
                {
                    Name = param.Name,
                    Type = param.Type.ODataResourceName(edmx)
                };

                if (param.Required.HasValue && param.Required.Value)
                {
                    newParameter.Nullable = false;
                }
                target.Parameters.Add(newParameter);
            }

            foreach(var param in methodCollection.Parameters)
            {
                if (param.Location != ParameterLocation.QueryString)
                    continue;

                var newParameter = new Parameter
                {
                    Name = param.Name,
                    Type = param.Type.ODataResourceName(edmx)
                };

                if (param.Required.HasValue && param.Required.Value)
                {
                    newParameter.Nullable = false;
                }
                target.Parameters.Add(newParameter);
            }

            if (methodCollection.ResponseType != null)
            {
                target.ReturnType = new ReturnType { Type = methodCollection.ResponseType.ODataResourceName(), Nullable = false };
            }

            var schema = FindOrCreateSchemaForNamespace(schemaName, edmx, true);
            if (target is Function)
                schema.Functions.Add((Function)target);
            else
                schema.Actions.Add((Validation.OData.Action)target);
        }

        /// <summary>
        /// Walks the requestPath through the resources / entities defined in the edmx and resolves
        /// the type of request represented by the path
        /// </summary>
        /// <param name="requestPath"></param>
        /// <param name="requestMethod"></param>
        /// <param name="edmx"></param>
        /// <returns></returns>
        private static ODataTargetInfo ParseRequestTargetType(string requestPath, MethodCollection requestMethodCollection, EntityFramework edmx)
        {
            string[] requestParts = requestPath.Substring(1).Split(new char[] { '/'});

            EntityContainer entryPoint = (from s in edmx.DataServices.Schemas
                                          where s.EntityContainers.Count > 0
                                          select s.EntityContainers.FirstOrDefault()).SingleOrDefault();

            if (entryPoint == null) throw new InvalidOperationException("Couldn't locate an EntityContainer to begin target resolution");

            IODataNavigable currentObject = entryPoint;
            IODataNavigable previousObject = null;

            for(int i=0; i<requestParts.Length; i++)
            {
                string uriPart = requestParts[i];
                IODataNavigable nextObject = null;

                if (uriPart == "{var}" && requestParts.Length > i + 1)
                {
                    try
                    {
                        nextObject = currentObject.NavigateByEntityTypeKey(edmx);
                    }
                    catch (Exception ex)
                    {
                        throw new NotSupportedException("Unable to navigation into EntityType by key: " + currentObject.TypeIdentifier + " (" + ex.Message + ")");
                    }
                }
                else if (uriPart == "{var}")
                {
                    // We're actually addressing the collection modified by var in this case.
                    break;
                }
                else
                {
                    nextObject = currentObject.NavigateByUriComponent(uriPart, edmx);
                }

                if (nextObject == null && i == requestParts.Length - 1)
                {
                    // The last component wasn't known already, so that means we have a new thing.
                    // We assume that if the uriPart doesnt' have a namespace that this is a navigation property that isn't documented.

                    // TODO: We may need to be smarter about this if we allow actions without namespaces. If that's the case, we could look at the request
                    // method to figure out of this appears to be an action (POST?) or a navigationProperty (GET?)

                    return new ODataTargetInfo
                    {
                        Name = uriPart,
                        Classification = uriPart.HasNamespace() ? ODataTargetClassification.Unknown : ODataTargetClassification.NavigationProperty,
                        QualifiedType = edmx.LookupIdentifierForType(currentObject),
                        Target = currentObject
                    };
                }
                else if (nextObject == null)
                {
                    throw new InvalidOperationException(
                        string.Format("Uri path requires navigating into unknown object hierarchy: missing property '{0}' on '{1}'", uriPart, currentObject.TypeIdentifier));
                }
                previousObject = currentObject;
                currentObject = nextObject;
            }

            var response = new ODataTargetInfo
            {
                Name = requestParts.Last(),
                QualifiedType = edmx.LookupIdentifierForType(currentObject),
                Target = currentObject
            };

            if (currentObject is EntityType || currentObject is Singleton)
                response.Classification = ODataTargetClassification.EntityType;
            else if (currentObject is EntityContainer)
                response.Classification = ODataTargetClassification.EntityContainer;
            else if (currentObject is ODataSimpleType)
                response.Classification = ODataTargetClassification.SimpleType;
            else if (currentObject is ODataCollection || currentObject is EntitySet)
            {
                if (previousObject != entryPoint)
                {
                    response.Classification = ODataTargetClassification.NavigationProperty;
                    response.QualifiedType = edmx.LookupIdentifierForType(currentObject);
                }
                else
                {
                    response.Classification = ODataTargetClassification.EntitySet;
                }
            }
            else if (currentObject is ComplexType)
            {
                throw new NotSupportedException($"Encountered a ComplexType. This is probably a doc bug where type '{currentObject.TypeIdentifier}' should be defined with keyProperty to be an EntityType");
            }
            else
            {
                throw new NotSupportedException($"Unhandled object type: {currentObject.GetType().Name}");
            }

            return response;
        }



        // EntitySet is something in the format of /name/{var}
        private readonly static System.Text.RegularExpressions.Regex EntitySetPathRegEx = new System.Text.RegularExpressions.Regex(@"^\/(\w*)\/{var}$");
        // Singleton is something in the format of /name
        private readonly static System.Text.RegularExpressions.Regex SingletonPathRegEx = new System.Text.RegularExpressions.Regex(@"^\/(\w*)$");

        /// <summary>
        /// Parse the URI paths for methods defined in the documentation and construct an entity container that contains these
        /// entity sets / singletons in the largest namespace.
        /// </summary>
        /// <param name="edmx"></param>
        private void BuildEntityCollection(EntityFramework edmx, string baseUrlToRemove)
        {
            Dictionary<string, MethodCollection> uniqueRequestPaths = GetUniqueRequestPaths(baseUrlToRemove);
            var resourcePaths = uniqueRequestPaths.Keys.OrderBy(x => x).ToArray();

            EntityContainer container = new EntityContainer();
            foreach (var path in resourcePaths)
            {
                if (EntitySetPathRegEx.IsMatch(path))
                {
                    var name = EntitySetPathRegEx.Match(path).Groups[1].Value;
                    container.EntitySets.Add(new EntitySet { Name = name, EntityType = uniqueRequestPaths[path].ResponseType.ODataResourceName() });
                }
                else if (SingletonPathRegEx.IsMatch(path))
                {
                    // Before we declare this a singleton, see if any other paths that have the same root match the entity set regex
                    var query = (from p in resourcePaths where p.StartsWith(path + "/") && EntitySetPathRegEx.IsMatch(p) select p);
                    if (query.Any())
                    {
                        // If there's a similar resource path that matches the entity, we don't declare a singleton.
                        continue;
                    }

                    var name = SingletonPathRegEx.Match(path).Groups[1].Value;
                    container.Singletons.Add(new Singleton { Name = name, Type = uniqueRequestPaths[path].ResponseType.ODataResourceName() });
                }
            }

            // TODO: Allow the default schema name to be specified instead of inferred
            var largestSchema = (from x in edmx.DataServices.Schemas
                                 orderby x.Entities.Count descending
                                 select x).First();
            container.Name = largestSchema.Namespace;
            largestSchema.EntityContainers.Add(container);
        }

        private Dictionary<string, MethodCollection> cachedUniqueRequestPaths { get; set; }
    
        /// <summary>
        /// Return a dictionary of the unique request paths in the 
        /// documentation and the method definitions that defined them.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, MethodCollection> GetUniqueRequestPaths(string baseUrlToRemove)
        {
            if (cachedUniqueRequestPaths == null)
            {
                Dictionary<string, MethodCollection> uniqueRequestPaths = new Dictionary<string, MethodCollection>();
                foreach (var m in Documents.Methods)
                {
                    if (m.ExpectedResponseMetadata.ExpectError)
                    {
                        // Ignore thigns that are expected to error
                        continue;
                    }

                    var path = m.RequestUriPathOnly(baseUrlToRemove);
                    if (!path.StartsWith("/"))
                    {
                        // Ignore aboslute URI paths
                        continue;
                    }

                    Console.WriteLine("Converted '{0}' into generic form '{1}'", m.Request.FirstLineOnly(), path);

                    if (!uniqueRequestPaths.ContainsKey(path))
                    {
                        uniqueRequestPaths.Add(path, new MethodCollection());
                    }
                    uniqueRequestPaths[path].Add(m);

                    Console.WriteLine("{0} :: {1} --> {2}", path, m.RequestMetadata.ResourceType, m.ExpectedResponseMetadata.ResourceType);
                }
                cachedUniqueRequestPaths = uniqueRequestPaths;
            }
            return cachedUniqueRequestPaths;
        }

        /// <summary>
        /// Find an existing schema definiton or create a new one in an entity framework for a given namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="edmx"></param>
        /// <returns></returns>
        private Schema FindOrCreateSchemaForNamespace(string ns, EntityFramework edmx, bool overrideNamespaceFilter = false)
        {
            // Check to see if this is a namespace that should be exported.
            if (!overrideNamespaceFilter && validNamespaces != null && !validNamespaces.Contains(ns))
            {
                return null;
            }

            if (ns.Equals("odata", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var matchingSchema = (from s in edmx.DataServices.Schemas
                                 where s.Namespace == ns
                                 select s).FirstOrDefault();

            if (null != matchingSchema)
                return matchingSchema;

            var newSchema = new Schema() { Namespace = ns };
            edmx.DataServices.Schemas.Add(newSchema);
            return newSchema;
        }


        private void AddResourceToSchema(Schema schema, ResourceDefinition resource, EntityFramework edmx)
        {
            ComplexType type;
            if (!string.IsNullOrEmpty(resource.KeyPropertyName))
            {
                var entity = new EntityType();
                entity.Key = new Key { PropertyRef = new PropertyRef { Name = resource.KeyPropertyName } };
                entity.NavigationProperties = (from p in resource.Parameters
                                               where p.IsNavigatable
                                               select ConvertParameterToProperty<NavigationProperty>(p)).ToList();
                schema.Entities.Add(entity);
                type = entity;
            }
            else
            {
                type = new ComplexType();
                
                schema.ComplexTypes.Add(type);
            }
            type.Name = resource.Name.TypeOnly();
            type.OpenType = resource.OriginalMetadata.IsOpenType;
            type.Properties = (from p in resource.Parameters
                               where !p.IsNavigatable && !p.Name.StartsWith("@")
                               select ConvertParameterToProperty<Property>(p) ).ToList();

            if (!string.IsNullOrEmpty(resource.KeyPropertyName))
            {
                // Make sure the keyProperty is not nullable
                type.Properties.Where(x => x.Name == resource.KeyPropertyName).Single().Nullable = false;
            }

            var annotations = (from p in resource.Parameters where p.Name != null && p.Name.StartsWith("@") select p);
            ParseInstanceAnnotations(annotations, resource, edmx);
        }


        private void ParseInstanceAnnotations(IEnumerable<ParameterDefinition> annotations, ResourceDefinition containedResource, EntityFramework edmx)
        {
            foreach (var prop in annotations)
            {
                var qualifiedName = prop.Name.Substring(1);
                var ns = qualifiedName.NamespaceOnly();
                var localName = qualifiedName.TypeOnly();

                Term term = new Term { Name = localName, AppliesTo = containedResource.Name, Type = prop.Type.ODataResourceName() };
                if (settings.IncludeDescriptions && !string.IsNullOrEmpty(prop.Description))
                {
                    term.Annotations.Add(new Annotation { Term = Term.DescriptionTerm, String = prop.Description });
                }

                var targetSchema = FindOrCreateSchemaForNamespace(ns, edmx, overrideNamespaceFilter: true);
                if (null != targetSchema)
                {
                    targetSchema.Terms.Add(term);
                }
            }
        }


        private T ConvertParameterToProperty<T>(ParameterDefinition param) where T : Property, new()
        {
            var prop = new T()
            {
                Name = param.Name,
                Type = param.Type.ODataResourceName()
            };

            //if (param.Required.HasValue)
            //{
            //    prop.Nullable = !param.Required.Value;
            //}

            // Add description annotation
            if (settings.IncludeDescriptions && !string.IsNullOrEmpty(param.Description))
            {
                prop.Annotation = new List<Annotation>();
                prop.Annotation.Add(
                    new Annotation()
                    {
                        Term = Term.DescriptionTerm,
                        String = param.Description
                    });
            }
            return prop;
        }
    }

}
