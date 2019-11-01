using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;

namespace GetD365Token
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string serviceUrl = ConfigurationManager.AppSettings["serviceUrl"];
                string clientId = ConfigurationManager.AppSettings["clientId"];
                string userName = ConfigurationManager.AppSettings["userName"];
                string password = ConfigurationManager.AppSettings["password"];
                string input;

                Console.WriteLine("GetD365Token - retrieve a testing OAuth access token from user/password.");
                // serviceUrl
                Console.WriteLine("Please input the service URL{0}{1}{2}?", !string.IsNullOrEmpty(serviceUrl) ? " (or press enter to use [" : string.Empty, serviceUrl, !string.IsNullOrEmpty(serviceUrl) ? "])" : string.Empty);
                input = Console.ReadLine();
                serviceUrl = string.IsNullOrEmpty(input) ? serviceUrl : input;
                // clientId
                Console.WriteLine("Please input the client ID{0}{1}{2}?", !string.IsNullOrEmpty(clientId) ? " (or press enter to use [" : string.Empty, clientId, !string.IsNullOrEmpty(clientId) ? "])" : string.Empty);
                input = Console.ReadLine();
                clientId = string.IsNullOrEmpty(input) ? clientId : input;
                // userName
                Console.WriteLine("Please input user name{0}{1}{2}?", !string.IsNullOrEmpty(userName) ? " (or press enter to use [" : string.Empty, userName, !string.IsNullOrEmpty(userName) ? "])" : string.Empty);
                input = Console.ReadLine();
                userName = string.IsNullOrEmpty(input) ? userName : input;
                // password
                Console.WriteLine("Please input the password?");
                password = Console.ReadLine();

                AuthenticationContext authContext =
                new AuthenticationContext("https://login.microsoftonline.com/common", false);
                UserCredential credential = new UserCredential(userName, password);
                AuthenticationResult result = authContext.AcquireToken(serviceUrl, clientId, credential);
                //The access token
                string accessToken = result.AccessToken;

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(serviceUrl);
                    client.Timeout = new TimeSpan(0, 2, 0);  //2 minutes  
                    client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                    client.DefaultRequestHeaders.Add("OData-Version", "4.0");
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage request =
                        new HttpRequestMessage(HttpMethod.Get, "/api/data/v9.0/WhoAmI");
                    //Set the access token
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                    //Get the response content and parse it.  
                        JObject body = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                        Guid userId = (Guid)body["UserId"];
                        Console.WriteLine("Your system user ID is: {0}", userId);
                        Console.WriteLine("The access token is: {0}", accessToken);
                        Console.WriteLine("Press any key...");
                        Console.ReadKey();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0} - {1}", ex.GetType().ToString(), ex.Message);
            }
        }
    }
}
