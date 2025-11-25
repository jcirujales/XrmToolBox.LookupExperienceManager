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
using static BulkLookupConfiguration.XrmToolBoxTool.BulkLookupConfigurationControl;

namespace BulkLookupConfiguration.XrmToolBoxTool.Services
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
                    var formLabel = GetLookupLabelFromFormXml(form.GetAttributeValue<string>("formxml"), lookupField);
                    results.Add(new LookupInfo
                    {
                        Form = form.GetAttributeValue<string>("name"),
                        SourceEntity = sourceEntity,
                        Label = formLabel,
                        SchemaName = lookupField
                    });
                }
            }

            return results;
        }

        private static string GetLookupLabelFromFormXml(string formXml, string schemaName)
        {
            try
            {
                var doc = XDocument.Parse(formXml);

                var control = doc.Descendants("control")
                    .FirstOrDefault(c => c.Attribute("datafieldname")?.Value == schemaName);

                if (control == null) return schemaName;

                // PERFECT: Direct child only
                var labelsElement = control.Parent?.Elements("labels").FirstOrDefault();

                if (labelsElement != null)
                {
                    var label = labelsElement.Elements("label")
                        .FirstOrDefault(l => l.Attribute("languagecode")?.Value == "1033")
                        ?.Attribute("description")?.Value;

                    return label ?? schemaName;
                }

                return schemaName;
            }
            catch
            {
                return schemaName;
            }
        }
    }
}
