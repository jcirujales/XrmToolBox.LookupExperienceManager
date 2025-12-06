using XrmToolBox.LookupExperienceManager.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XrmToolBox.LookupExperienceManager.Services
{
    public static class DataverseService
    {
        public static void WhoAmI(IOrganizationService service)
         => service.Execute(new WhoAmIRequest());

        public static EntityCollection GetSolutionCompoonents(Guid selectedSolutionId, IOrganizationService orgService)
        {
            var componentQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, selectedSolutionId),
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 1)
                    }
                }
            };

            var components = orgService.RetrieveMultiple(componentQuery);
            
            return components;
        }

        public static EntityMetadataCollection GetTables(Guid[] metadataIds, IOrganizationService orgService)
        {
            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new MetadataConditionExpression("MetadataId", MetadataConditionOperator.In, metadataIds)
                        }
                    },
                    Properties = new MetadataPropertiesExpression
                    {
                        PropertyNames = { "LogicalName", "DisplayName", "SchemaName" }
                    }
                }
            };

            var response = (RetrieveMetadataChangesResponse)orgService.Execute(request);
            return response.EntityMetadata;
        }

        public static EntityCollection GetSolutions(IOrganizationService orgService)
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("friendlyname", "uniquename", "ismanaged", "version", "solutionid"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("isvisible", ConditionOperator.Equal, true) }
                },
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
            };

            var result = orgService.RetrieveMultiple(query);
            return result;
        }

        public static EntityMetadata GetOneToManyRelationships(string targetEntityLogicalName, IOrganizationService orgService)
        {
            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, targetEntityLogicalName)
                        }
                    },
                    RelationshipQuery = new RelationshipQueryExpression
                    {
                        Criteria = new MetadataFilterExpression(LogicalOperator.And)
                        {
                            Conditions =
                            {
                                new MetadataConditionExpression("RelationshipType", MetadataConditionOperator.Equals, Microsoft.Xrm.Sdk.Metadata.RelationshipType.OneToManyRelationship),
                                new MetadataConditionExpression("IsValidForAdvancedFind", MetadataConditionOperator.Equals, true),
                                new MetadataConditionExpression("IsCustomizable", MetadataConditionOperator.Equals, true),
                            }
                        }
                    }
                }
            };

            var response = (RetrieveMetadataChangesResponse)orgService.Execute(request);

            return response.EntityMetadata.FirstOrDefault();
        }

        public static EntityCollection GetFormsContainingLookupField(
            string lookupSchemaName,
            string entityLogicalName,
            IOrganizationService orgService)
        {
            var query = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("name", "objecttypecode", "formxml", "type"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        // Search formxml for the exact datafieldname
                        new ConditionExpression("formxml", ConditionOperator.Like, $"%datafieldname=\"{lookupSchemaName}\"%"),
                        // Only main forms and quick create forms (most common)
                        new ConditionExpression("type", ConditionOperator.In, 2, 7)
                    }
                }
            };
            query.LinkEntities.Add(new LinkEntity
            {
                LinkFromEntityName = "systemform",
                LinkFromAttributeName = "objecttypecode",
                LinkToEntityName = "entity",
                LinkToAttributeName = "objecttypecode",
                LinkCriteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("logicalname", ConditionOperator.Equal, entityLogicalName)
                    }
                }
            });

            query.AddOrder("objecttypecode", OrderType.Ascending);
            query.AddOrder("type", OrderType.Ascending);
            query.AddOrder("name", OrderType.Ascending);

            var records = orgService.RetrieveMultiple(query);

            return records;
        }
        public static List<LookupInfo> GetLookupAttributeInfo(List<OneToManyRelationshipMetadata> relationships, IOrganizationService orgService)
        {
            var results = new List<LookupInfo>();
            foreach (var rel in relationships)
            {
                var sourceEntity = rel.ReferencingEntity;
                var lookupField = rel.ReferencingAttribute;

                var forms = DataverseService.GetFormsContainingLookupField(lookupField, sourceEntity, orgService);

                foreach (var form in forms.Entities)
                {
                    var formXml = form.GetAttributeValue<string>("formxml");
                    var formConfigSettings = GetLookupConfigurationSettings(formXml, lookupField);
                    
                    results.Add(new LookupInfo
                    {
                        Form = form.GetAttributeValue<string>("name"),
                        FormId = form.Id,
                        FormXml = formXml,
                        SourceEntity = sourceEntity,
                        Label = formConfigSettings.Label,
                        SchemaName = lookupField,
                        DisableMru = formConfigSettings.DisableMru,
                        IsInlineNewEnabled = formConfigSettings.IsInlineNewEnabled,
                        UseMainFormDialogForCreate = formConfigSettings.UseMainFormDialogForCreate,
                        UseMainFormDialogForEdit = formConfigSettings.UseMainFormDialogForEdit
                    });
                }
            }

            return results;
        }

        private static bool GetBool(XElement parameters, string name, bool defaultValue = false)
        {
            var element = parameters.Element(name);
            if (element == null) return defaultValue;
            return element.Value == "true";
        }

        private static LookupInfo GetLookupConfigurationSettings(string formXml, string schemaName)
        {
            var result = new LookupInfo
            {
                Label = schemaName,
                IsInlineNewEnabled = true,
                DisableMru = false,
                UseMainFormDialogForCreate = false,
                UseMainFormDialogForEdit = false
            };

            var doc = XDocument.Parse(formXml);
            var control = doc.Descendants("control")
                .FirstOrDefault(c => c.Attribute("datafieldname")?.Value == schemaName);

            if (control == null) return result;

            // Label
            var label = control.Parent?.Elements("labels")
                .FirstOrDefault()?
                .Elements("label")
                .FirstOrDefault(l => l.Attribute("languagecode")?.Value == "1033")
                ?.Attribute("description")?.Value;

            if (!string.IsNullOrEmpty(label))
                result.Label = label;

            // Parameters — direct children
            var parameters = control.Elements("parameters").FirstOrDefault();
            if (parameters != null)
            {
                result.IsInlineNewEnabled = GetBool(parameters, "IsInlineNewEnabled", true);
                result.DisableMru = GetBool(parameters, "DisableMru", false);
                // useMainFormDialogForCreate and useMainFormDialogForEdit should remain with first letter as lowercase as defined in customizations.xml!
                result.UseMainFormDialogForCreate = GetBool(parameters, FormXMLAttributes.UseMainFormDialogForCreate, false); 
                result.UseMainFormDialogForEdit = GetBool(parameters, FormXMLAttributes.UseMainFormDialogForEdit, false);
            }

            return result;
        }
    }
}
