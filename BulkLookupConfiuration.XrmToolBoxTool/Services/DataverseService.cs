using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace BulkLookupConfiguration.XrmToolBoxTool.Services
{
    public static class DataverseService
    {
        public static void WhoAmI(IOrganizationService service)
         => service.Execute(new WhoAmIRequest());
    }
}
