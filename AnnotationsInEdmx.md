# Supported EDMX Generation

To facilititate the integration with Microsoft Graph API and VIPR SDK generation technology, markdown-scanner now supports generating EDMX metadata from documentation.
This data includes adding supported capabilities through OData annotations to the output metadata.

The descriptions of the capabilities are documented on the OData website: [Capabilities vocabulary](http://www.odata.org/blog/introducing-a-capabilities-vocabulary/).

## Supported Annotation

### Org.OData.Capabilities.V1

#### CountRestrictions (Not Yet Implemented)

```xml
<Term Name="CountRestrictions" Type="Capabilities.CountRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on /$count path suffix and $count=true system query option"/>
</Term>
<ComplexType Name="CountRestrictionsType">
	<Property Name="Countable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="Entities can be counted"/>
	</Property>
	<Property Name="NonCountableProperties" Type="Collection(Edm.PropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These collection properties do not allow /$count segments"/>
	</Property>
	<Property Name="NonCountableNavigationProperties" Type="Collection(Edm.NavigationPropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These navigation properties do not allow /$count segments"/>
	</Property>
</ComplexType>
```


#### TopSupported (Not Yet Implemented)

```xml
<Term Name="TopSupported" Type="Core.Tag" DefaultValue="true" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Supports $top"/>
</Term>
```

#### SkipSupported (Not Yet Implemented)

```xml
<Term Name="SkipSupported" Type="Core.Tag" DefaultValue="true" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Supports $skip"/>
</Term>
```

#### FilterRestrictions (Not Yet Implemented)

```xml
<Term Name="FilterRestrictions" Type="Capabilities.FilterRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on $filter expressions"/>
</Term>
<ComplexType Name="FilterRestrictionsType">
	<Property Name="Filterable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="$filter is supported"/>
	</Property>
	<Property Name="RequiresFilter" Type="Edm.Boolean" DefaultValue="false">
		<Annotation Term="Core.Description" String="$filter is required"/>
	</Property>
	<Property Name="RequiredProperties" Type="Collection(Edm.PropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These properties must be specified in the $filter clause (properties of derived types are not allowed here)"/>
	</Property>
	<Property Name="NonFilterableProperties" Type="Collection(Edm.PropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These properties cannot be used in $filter expressions"/>
	</Property>
</ComplexType>
```

#### SortRestrictions (Not Yet Implemented)

```xml
<Term Name="SortRestrictions" Type="Capabilities.SortRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on $orderby expressions"/>
</Term>
<ComplexType Name="SortRestrictionsType">
	<Property Name="Sortable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="$orderby is supported"/>
	</Property>
	<Property Name="AscendingOnlyProperties" Type="Collection(Edm.PropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These properties can only be used for sorting in Ascending order"/>
	</Property>
	<Property Name="DescendingOnlyProperties" Type="Collection(Edm.PropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These properties can only be used for sorting in Descending order"/>
	</Property>
	<Property Name="NonSortableProperties" Type="Collection(Edm.PropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These properties cannot be used in $orderby expressions"/>
	</Property>
</ComplexType>
```

#### ExpandRestrictions (Not Yet Implemented)

```xml
<Term Name="ExpandRestrictions" Type="Capabilities.ExpandRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on $expand expressions"/>
</Term>
<ComplexType Name="ExpandRestrictionsType">
	<Property Name="Expandable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="$expand is supported"/>
	</Property>
	<Property Name="NonExpandableProperties" Type="Collection(Edm.NavigationPropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These properties cannot be used in $expand expressions"/>
	</Property>
</ComplexType>
```

#### InsertRestrictions (Not Yet Implemented)

aka "writeable"

```xml
<Term Name="InsertRestrictions" Type="Capabilities.InsertRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on insert operations"/>
</Term>
<ComplexType Name="InsertRestrictionsType">
	<Property Name="Insertable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="Entities can be inserted"/>
	</Property>
	<Property Name="NonInsertableNavigationProperties" Type="Collection(Edm.NavigationPropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These navigation properties do not allow deep inserts"/>
	</Property>
</ComplexType>
```
#### UpdateRestrictions (Not Yet Implemented)

aka "writeable" 

```xml
<Term Name="UpdateRestrictions" Type="Capabilities.UpdateRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on update operations"/>
</Term>
<ComplexType Name="UpdateRestrictionsType">
	<Property Name="Updatable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="Entities can be updated"/>
	</Property>
	<Property Name="NonUpdatableNavigationProperties" Type="Collection(Edm.NavigationPropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These navigation properties do not allow rebinding"/>
	</Property>
</ComplexType>
```

#### DeleteRestrictions (Not Yet Implemented)

aka "deletable"

```xml
<Term Name="DeleteRestrictions" Type="Capabilities.DeleteRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on delete operations"/>
</Term>
<ComplexType Name="DeleteRestrictionsType">
	<Property Name="Deletable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="Entities can be deleted"/>
	</Property>
	<Property Name="NonDeletableNavigationProperties" Type="Collection(Edm.NavigationPropertyPath)" Nullable="false">
		<Annotation Term="Core.Description" String="These navigation properties do not allow DeleteLink requests"/>
	</Property>
</ComplexType>
```

#### SelectRestrictions

Couldn't find a description of this annotation anywhere.


### Org.OData.Core.V1

#### Description (Implemented)

```xml
<Term Name="Description" Type="Edm.String">
	<Annotation Term="Core.Description" String="A brief description of a model element"/>
	<Annotation Term="Core.IsLanguageDependent"/>
</Term>
<Term Name="LongDescription" Type="Edm.String">
	<Annotation Term="Core.Description" String="A lengthy description of a model element"/>
	<Annotation Term="Core.IsLanguageDependent"/>
</Term>
```

## Org.Microsoft.OneDrive.V1


#### Enumerable (Not Yet Implemented)

```xml
<Term Name="EnumerateRestrictions" Type="Capabilities.EnumerateRestrictionsType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Restrictions on enumerating operations"/>
</Term>
<ComplexType Name="EnumerateRestrictionsType">
	<Property Name="Enumerable" Type="Edm.Boolean" DefaultValue="true">
		<Annotation Term="Core.Description" String="Entities can be enumerated from the entity set."/>
	</Property>
</ComplexType>
```

#### Async (Not Yet Implemented)

```xml
<Term Name="LongRunningOperationRescriptions" Type="Capabilities.LongRunningOperationRescriptionsType" AppliesTo="Action Function">
	<Annotation Term="Core.Description" String="Restrictions on requests that are long running."/>
</Term>
<ComplexType Name="LongRunningOperationRescriptionsType">
	<Property Name="AllowsAsyncRequest" Type="Edm.Boolean" DefaultValue="false">
		<Annotation Term="Core.Description" String="Action or function can be called asynchronously if true." />
	</Property>
	<Property Name="RequiresAsyncRequest" Type="Edm.Boolean" DefaultValue="false>
		<Annotation Term="Core.Description" String="Indicates the target must be called asynchronously to succeed." />
	</Property>
</ComplexType>
```

#### SpecialCollection (Not Yet Implemented)

```xml
<Term Name="CollectionWithAdditionalProperties" Type="Capabilities.CollectionWithAdditionalPropertiesType" AppliesTo="EntitySet">
	<Annotation Term="Core.Description" String="Indicates that when enumerating the collection additional properties may be returned as peers of the collection."/>
</Term>
<ComplexType Name="CollectionWithAdditionalPropertiesType">
	<Property Name="ReturnsExtraProperties" Type="Edm.Boolean" DefaultValue="false">
		<Annotation Term="Core.Description" String="Indicates that the entity set will return additional properties as peers to 'value'." />
	</Property>
</ComplexType>
```

