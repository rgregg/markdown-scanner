﻿/*
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

namespace ApiDocs.Validation.OData
{
    using Utility;
    using Transformation;
    using System.Collections.Generic;
    using System.Xml.Serialization;


    [XmlRoot("Annotations", Namespace = ODataParser.EdmNamespace)]
    [Mergable(CollectionIdentifier ="ElementIdentifier")]
    public class Annotations : XmlBackedTransformableObject
    {
        [XmlElement("Annotation")]
        public List<Annotation> Annotation { get; set; }

        [XmlAttribute("Target"), SortBy, MergePolicy(MergePolicy.EqualOrNull)]
        public string Target { get; set; }

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public override string ElementIdentifier { get { return this.Target; } set { this.Target = value; } }

    }
}
