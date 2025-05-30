using System.Threading.Tasks;
using Models;
using MultiChainAPI.Models;

namespace MultiChainAPI.Functionality
{
    public interface IMultichainService
    {
        /// <summary>
        /// Publishes data to the active node in Multichain.
        /// </summary>
        /// <param name="streamName">The name of the stream to publish data to.</param>
        /// <param name="data">The data to publish.</param>
        /// <returns>The result of the publish operation.</returns>
        Task<string> PublishDataAsync(string streamName,string keys, object data);
        Task<string> FetchDataAsync(string streamName);
        Task<string> FetchChaininfo();
        Task<string> FetchTransinfo();
        Task<string> FetchLastFiveDaysTransactions();
         Task<string> FetchPeerinfo();
        Task<string> FetchMasterDataAsync(string streamName, string key);

        List<Student> GetAllStudents();

        Task<string> FetchMasterDataforpdfAsync(string streamName, string key);
        /// <summary>
        /// Toggles between node1 and node2.
        /// </summary>
        /// <param name="isNode1Active">True if node1 should be active, false for node2.</param>
        void ToggleNode(bool isNode1Active);


    }

}
