using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;

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
    }
}
