using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Models;
using MultiChainAPI.Functionality;
using MultiChainAPI.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MultiChainAPI.Services
{
    public class MultichainService : IMultichainService
    {
        // Define your nodes
        private readonly dynamic _nodes = new
        {
            node1 = new { multichainUrl = "http://127.0.0.1:1234", username = "multichainrpc", password = "4mvtWqj4XxQCjV9MNC5wjs6H9i8FBVKC4ZkvWV9FnxZy" },
            node2 = new { multichainUrl = "http://192.168.10.206:1234", username = "multichainrpc", password = "CewXyskNnxWEB9SfpoC8S2dj2w4MUhsoMcNE4soZ1hBs" }
        };

        // Default node is node1
        private bool node1Active = true;

        public async Task<string> PublishDataAsync(string streamName, string keys, object data)
        {
            var currentNode = node1Active ? _nodes.node1 : _nodes.node2;
            var parameters = new object[] { streamName, keys, new { json = data } };
            return await SendRpcRequestAsync("publish", parameters, currentNode);
        }


        // Fetch data from MultiChain using streamName and key
        public async Task<string> FetchDataAsync(string streamName)
        {
            // Select the node to query (assuming node1Active determines the active node)
            var currentNode = node1Active ? _nodes.node1 : _nodes.node2;

            // Parameters for the RPC call to fetch data
            var parameters = new object[] { streamName};

            // Sending RPC request to fetch data
            return await SendRpcRequestAsync("liststreamkeys", parameters, currentNode);
        }

        // Fetch data from MultiChain using streamName and key
        //public async Task<string> FetchMasterDataAsync(string streamName, string key)
        //{
        //    // Select the node to query (assuming node1Active determines the active node)
        //    var currentNode = node1Active ? _nodes.node1 : _nodes.node2;
        //    var param1 = "false";
        //    var param2 = "100";
        //    // Parameters for the RPC call to fetch data
        //    var parameters = new object[] { streamName, key, param1, param2 };

        //    // Sending RPC request to fetch data
        //    return await SendRpcRequestAsync("liststreamkeyitems", parameters, currentNode);
        //}

     public async Task<string> FetchMasterDataAsync(string streamName, string key)
      {
          // Select the active node
          var currentNode = node1Active ? _nodes.node1 : _nodes.node2;

          // Correct parameter types
          bool verbose = false;  // Ensure this is a boolean
          int count = 100;       // Ensure this is an integer

          // Parameters for RPC request
          var parameters = new object[] { streamName, key, verbose, count };

          try
          {
              // Sending RPC request
              var response = await SendRpcRequestAsync("liststreamkeyitems", parameters, currentNode);

              // Debugging logs
             // Console.WriteLine($"FetchMasterDataAsync Response: {response}");

              return response;
          }
          catch (Exception ex)
          {
             // Console.WriteLine($"Error fetching data from MultiChain: {ex.Message}");
              return $"Error: {ex.Message}";
          }
      }

      public async Task<string> FetchMasterDataforpdfAsync(string streamName, string key)
      {
          // Select the active node
          var currentNode = node1Active ? _nodes.node1 : _nodes.node2;

          // Correct parameter types
          bool verbose = false;  // Ensure this is a boolean
          int count = 1;       // Ensure this is an integer

          // Parameters for RPC request
          var parameters = new object[] { streamName, key, verbose, count };

          try
          {
           // Console.WriteLine(parameters);
              // Sending RPC request
              var response = await SendRpcRequestAsync("liststreamkeyitems", parameters, currentNode);

              // Debugging logs
             // Console.WriteLine($"FetchMasterDataAsync Response: {response}");

              return response;
          }
          catch (Exception ex)
          {
             // Console.WriteLine($"Error fetching data from MultiChain: {ex.Message}");
              return $"Error: {ex.Message}";
          }
      }

        // Fetch data from MultiChain using streamName and key
        public async Task<string> FetchChaininfo()
        {
            // Select the node to query (assuming node1Active determines the active node)
            var currentNode = node1Active ? _nodes.node1 : _nodes.node2;

            // Parameters for the RPC call to fetch data
            var parameters = new object[] { };

            // Sending RPC request to fetch data
            return await SendRpcRequestAsync("getchaintotals", parameters, currentNode);
            
        }

        public async Task<string> FetchTransinfo()
        {
            // Select the node to query (assuming node1Active determines the active node)
            var currentNode = node1Active ? _nodes.node1 : _nodes.node2;

            // Parameters for the RPC call to fetch data
            var parameters = new object[] { 5,0};

            // Sending RPC request to fetch data
            return await SendRpcRequestAsync("listwallettransactions", parameters, currentNode);
            
        }


              public async Task<string> FetchLastFiveDaysTransactions()
        {
            // Select the node to query (assuming node1Active determines the active node)
            var currentNode = node1Active ? _nodes.node1 : _nodes.node2;

            // Parameters for the RPC call to fetch data
            var parameters = new object[] { 100,0};

            // Sending RPC request to fetch data
            return await SendRpcRequestAsync("listwallettransactions", parameters, currentNode);
            
        }

         // Fetch data from MultiChain using streamName and key
        public async Task<string> FetchPeerinfo()
        {
            // Select the node to query (assuming node1Active determines the active node)
            var currentNode = node1Active ? _nodes.node1 : _nodes.node2;

            // Parameters for the RPC call to fetch data
            var parameters = new object[] { };

            // Sending RPC request to fetch data
            return await SendRpcRequestAsync("getnetworkinfo", parameters, currentNode);
            
        }


        public void ToggleNode(bool isNode1Active)
        {
            node1Active = isNode1Active;
        }

        private async Task<string> SendRpcRequestAsync(string method, object[] parameters, dynamic currentNode)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var jsonRequest = new
                    {
                        jsonrpc = "2.0",
                        method,
                        @params = parameters,
                        id = 1
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(jsonRequest), Encoding.UTF8, "application/json");

                    // Add Authorization header using Basic Auth
                    var byteArray = Encoding.ASCII.GetBytes($"{currentNode.username}:{currentNode.password}");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    // Make the HTTP request to the Multichain node
                    var response = await client.PostAsync(currentNode.multichainUrl, content);
                    response.EnsureSuccessStatusCode();  // Ensure the HTTP request was successful

                    var responseString = await response.Content.ReadAsStringAsync();
                   // Console.WriteLine("Response: " + responseString);   
                    return responseString;
                }
                catch (Exception ex)
                {
                    // Log or handle exceptions as necessary
                    throw new Exception("Error while sending RPC request to Multichain", ex);
                }
            }
        }

        public List<Student> GetAllStudents()
        {
            throw new NotImplementedException();
        }
    }
}
