using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;
using System.Linq;

namespace ChangeActivitiesContact
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "";
            string oldContactFullname = "Cathan Cook";
            string newContactFullname = "Blue Catfish";

            CrmServiceClient client = new CrmServiceClient(connectionString);
            IOrganizationService service = client.OrganizationWebProxyClient != null ? client.OrganizationWebProxyClient : (IOrganizationService)client.OrganizationServiceProxy;

            QueryByAttribute qbyaOldUser = new QueryByAttribute("contact");
            qbyaOldUser.AddAttributeValue("fullname", oldContactFullname);
            Guid oldContactId = (Guid)service.RetrieveMultiple(qbyaOldUser)[0].Attributes["contactid"];
            QueryByAttribute qbyaNewUser = new QueryByAttribute("contact");
            qbyaNewUser.AddAttributeValue("fullname", newContactFullname);
            Guid newContactId = (Guid)service.RetrieveMultiple(qbyaNewUser)[0].Attributes["contactid"];

            foreach (string activity in new string[]{ "activityparty", "task", "phonecall", "email", "fax", "appointment", "letter", "campaignresponse", "campaignactivity" }) // Add other activities as needed!!!
            {
                // Get Metadata for the activity entity
                RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.All,
                    LogicalName = activity
                };
                RetrieveEntityResponse retrieveAccountEntityResponse = (RetrieveEntityResponse)service.Execute(retrieveEntityRequest);
                EntityMetadata entityMetadata = retrieveAccountEntityResponse.EntityMetadata;
                var fieldnames = entityMetadata.ManyToOneRelationships
                                    .Where(e => e.ReferencedEntity == "contact")
                                    .Select(e => new { name = e.ReferencingAttribute });
                foreach (var field in fieldnames)
                {
                    QueryExpression query = new QueryExpression(activity)
                    {
                        ColumnSet = new ColumnSet("activityid", field.name)
                    };
                    query.Criteria.AddCondition(new ConditionExpression(field.name, ConditionOperator.Equal, oldContactId));

                    foreach (Entity e in service.RetrieveMultiple(query).Entities)
                    {
                        e.Attributes["ownerid"] = new EntityReference("systemuser", newContactId);
                        service.Update(e);
                    }
                }
            }
        }
    }
}
