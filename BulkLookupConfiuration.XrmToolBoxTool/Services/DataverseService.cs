using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services.Description;
using System.Windows.Documents;
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
        public static List<LookupInfo> GetLookupAttributeInfo(List<OneToManyRelationshipMetadata> relationships, IOrganizationService orgService)
        {
            var results = new List<LookupInfo>();
            foreach (var rel in relationships)
            {
                var sourceEntity = rel.ReferencingEntity;
                var lookupField = rel.ReferencingAttribute;

                var attrRequest = new RetrieveAttributeRequest
                {
                    EntityLogicalName = sourceEntity,
                    LogicalName = lookupField,
                    RetrieveAsIfPublished = true
                };

                var attrResponse = (RetrieveAttributeResponse)orgService.Execute(attrRequest);
                var lookupAttr = (LookupAttributeMetadata)attrResponse.AttributeMetadata;

                var label = lookupAttr.DisplayName?.UserLocalizedLabel?.Label
                            ?? lookupField;

                results.Add(new LookupInfo
                {
                    SourceEntity = sourceEntity,
                    Label = "TODO: Form Label",
                    SchemaName = lookupField
                });
            }

            return results;
        }
    }
}
