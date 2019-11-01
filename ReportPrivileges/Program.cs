using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System.Linq;

namespace ReportPrivileges
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "";

            CrmServiceClient client = new CrmServiceClient(connectionString);
            IOrganizationService service = client.OrganizationWebProxyClient != null ? client.OrganizationWebProxyClient : (IOrganizationService)client.OrganizationServiceProxy;

            var roles = from i in service.RetrieveMultiple(new QueryExpression("roles") { ColumnSet = new ColumnSet(true) }).Entities
                        select new { roleid = (Guid)i["id"], rolename = (string)i["name"] };

            var privileges = from i in service.RetrieveMultiple(new QueryExpression("privilege") { ColumnSet = new ColumnSet(true) }).Entities
                             select new { };
            var roleprivileges = service.RetrieveMultiple(new QueryExpression("roleprivilege") { ColumnSet = new ColumnSet(true) }).Entities;

            //var results = from rp in roleprivileges
            //              join r in roles
            //              on rp.
        }
    }
}
