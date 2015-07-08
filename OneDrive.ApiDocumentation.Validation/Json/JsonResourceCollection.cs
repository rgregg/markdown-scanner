﻿namespace OneDrive.ApiDocumentation.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JsonResourceCollection
    {
        private Dictionary<string, JsonSchema> m_RegisteredSchema = new Dictionary<string,JsonSchema>();

        public JsonSchema[] RegisteredSchema
        {
            get { return m_RegisteredSchema.Values.ToArray(); }
        }

        public void RegisterJsonResource(ResourceDefinition resource)
        {
            var schema = new JsonSchema(resource);
            m_RegisteredSchema[resource.Metadata.ResourceType] = schema;
        }

        /// <summary>
        /// Validates the value of json according to an implicit schmea defined by expectedJson
        /// </summary>
        /// <param name="expectedJson"></param>
        /// <param name="actualJson"></param>
        /// <returns></returns>
        public bool ValidateJsonExample(CodeBlockAnnotation expectedResponseAnnotation, string actualResponseBodyJson, out ValidationError[] errors)
        {
            List<ValidationError> newErrors = new List<ValidationError>();

            var resourceType = expectedResponseAnnotation.ResourceType;
            if (resourceType == "stream")
            {
                // No validation since we're streaming data
                errors = null;
                return true;
            }
            else
            {
                JsonSchema schema;
                if (string.IsNullOrEmpty(resourceType))
                {
                    schema = JsonSchema.EmptyResponseSchema;
                }
                else if (!m_RegisteredSchema.TryGetValue(resourceType, out schema))
                {
                    newErrors.Add(new ValidationWarning(ValidationErrorCode.ResponseResourceTypeMissing, null, "Missing required resource: {0}. Validation limited to basics only.", resourceType));
                    // Create a new schema based on what's avaiable in the json
                    schema = new JsonSchema(actualResponseBodyJson, new CodeBlockAnnotation { ResourceType = expectedResponseAnnotation.ResourceType });
                }

                ValidationError[] validationJsonOutput;
                ValidateJsonCompilesWithSchema(schema, new JsonExample(actualResponseBodyJson, expectedResponseAnnotation), out validationJsonOutput);

                newErrors.AddRange(validationJsonOutput);
                errors = newErrors.ToArray();
                return errors.Length == 0;
            }
        }

        /// <summary>
        /// Validates that the actual response body matches the schema defined for the response and any additional constraints
        /// from the expected request (e.g. properties that are included in the expected response are required in the actual 
        /// response even if the metadata defines that the response is truncated)
        /// </summary>
        /// <param name="actualResponse"></param>
        /// <param name="expectedResponse"></param>
        /// <param name="schemaErrors"></param>
        /// <returns></returns>
        internal bool ValidateResponseMatchesSchema(MethodDefinition method, Http.HttpResponse actualResponse, Http.HttpResponse expectedResponse, out ValidationError[] schemaErrors)
        {
            List<ValidationError> newErrors = new List<ValidationError>();

            var expectedResourceType = method.ExpectedResponseMetadata.ResourceType;
            if (expectedResourceType == "stream")
            {
                // No validation since we're streaming data
                schemaErrors = new ValidationError[0];
                return true;
            }

            // Get a reference of our JsonSchema that we're checking the response with
            var expectedResponseJson = (null != expectedResponse) ? expectedResponse.Body : null;
            JsonSchema schema = GetJsonSchema(expectedResourceType, newErrors, expectedResponseJson);

            if (null == schema)
            {
                newErrors.Add(new ValidationError(ValidationErrorCode.ResourceTypeNotFound, null, "Unable to locate a definition for resource type: {0}", expectedResourceType));
            }
            else
            {
                ValidationError[] validationJsonOutput;
                ValidateJsonCompilesWithSchema(schema, new JsonExample(actualResponse.Body, method.ExpectedResponseMetadata), out validationJsonOutput, (null != expectedResponseJson) ? new JsonExample(expectedResponseJson) : null);
                newErrors.AddRange(validationJsonOutput);
            }

            schemaErrors = newErrors.ToArray();
            return !schemaErrors.WereWarningsOrErrors();

        }

        /// <summary>
        /// Returns a JSON schema reference, either by finding an example in the registered schema or by 
        /// creating a new temporary schema from the fallback.
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="errors"></param>
        /// <param name="jsonStringForFallbackIfMissingResource"></param>
        /// <returns></returns>
        protected JsonSchema GetJsonSchema(string resourceType, IList<ValidationError> errors, string jsonStringForFallbackIfMissingResource)
        {
            JsonSchema schema = null;
            if (string.IsNullOrEmpty(resourceType))
            {
                errors.Add(new ValidationMessage(null, "Resource type was null or missing, so we assume there is no response to validate."));
                schema = JsonSchema.EmptyResponseSchema;
            }
            else if (!m_RegisteredSchema.TryGetValue(resourceType, out schema) && !string.IsNullOrEmpty(jsonStringForFallbackIfMissingResource))
            {
                errors.Add(new ValidationWarning(ValidationErrorCode.ResponseResourceTypeMissing, null, "Missing required resource: {0}. Validation based on fallback example.", resourceType));
                // Create a new schema based on what's avaiable in the expected response JSON
                schema = new JsonSchema(jsonStringForFallbackIfMissingResource, new CodeBlockAnnotation { ResourceType = resourceType });
            }
            
            return schema;
        }

        /// <summary>
        /// Examines input json string to ensure that it compiles with the JsonSchema definition. Any errors in the
        /// validation of the schema are returned via the errors out parameter.
        /// </summary>
        /// <param name="schema">Schema definition used as a reference.</param>
        /// <param name="inputJson">Input json example to be validated</param>
        /// <param name="errors">Out parameter that provides any errors, warnings, or messages that were generated</param>
        /// <param name="expectedJson"></param>
        /// <returns></returns>
        public bool ValidateJsonCompilesWithSchema(JsonSchema schema, JsonExample inputJson, out ValidationError[] errors, JsonExample expectedJson = null)
        {
            string collectionPropertyName = "value";
            if (null != inputJson && null != inputJson.Annotation && null != inputJson.Annotation.CollectionPropertyName)
            {
                collectionPropertyName = inputJson.Annotation.CollectionPropertyName;
            }

            ValidationOptions options = new ValidationOptions
            {
                AllowTruncatedResponses = (inputJson.Annotation ?? new CodeBlockAnnotation()).TruncatedResult,
                CollectionPropertyName =  collectionPropertyName
            };

            return schema.ValidateJson(inputJson, out errors, m_RegisteredSchema, options, expectedJson);
        }

        internal void RegisterJsonResources(IEnumerable<ResourceDefinition> resources)
        {
            foreach (var resource in resources)
            {
                RegisterJsonResource(resource);
            }
        }

        internal void Clear()
        {
            m_RegisteredSchema.Clear();
        }


    }


   
}
