using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiChainAPI.Models
{
    public class NodeConfig
    {
    public string MultichainUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    }

    public class MultiChainTransaction
{
    public string TxId { get; set; }          // Transaction ID
    public decimal Amount { get; set; }       // Amount in transaction
    public long Time { get; set; }            // Unix Timestamp (Seconds)
    public string Confirmations { get; set; } // Confirmation status
}
}