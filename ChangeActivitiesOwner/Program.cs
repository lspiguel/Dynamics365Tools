using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace ChangeActivitiesOwner
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "";
            string oldUserFullname = "Alan Steiner (Sample Data)";
            string newUserFullname = "Kelly Krout (Sample Data)";

            CrmServiceClient client = new CrmServiceClient(connectionString);
            IOrganizationService service = client.OrganizationWebProxyClient != null ? client.OrganizationWebProxyClient : (IOrganizationService)client.OrganizationServiceProxy;

            QueryByAttribute qbyaOldUser = new QueryByAttribute("systemuser");
            qbyaOldUser.AddAttributeValue("fullname", oldUserFullname);
            Guid olduserid = (Guid)service.RetrieveMultiple(qbyaOldUser)[0].Attributes["systemuserid"];
            QueryByAttribute qbyaNewUser = new QueryByAttribute("systemuser");
            qbyaNewUser.AddAttributeValue("fullname", newUserFullname);
            Guid newuserid = (Guid)service.RetrieveMultiple(qbyaNewUser)[0].Attributes["systemuserid"];

            foreach (string activity in new string[]{ "task", "phonecall", "email", "fax", "appointment", "letter", "campaignresponse", "campaignactivity" }) // Add other activities as needed!!!
            {
                QueryExpression query = new QueryExpression(activity)
                {
                    ColumnSet = new ColumnSet("activityid", "ownerid")
                };
                query.Criteria.AddCondition(new ConditionExpression("ownerid", ConditionOperator.Equal, olduserid));

                foreach (Entity e in service.RetrieveMultiple(query).Entities)
                {
                    e.Attributes["ownerid"] = new EntityReference("systemuser", newuserid);
                    service.Update(e);
                }
            }
        }
    }
}
