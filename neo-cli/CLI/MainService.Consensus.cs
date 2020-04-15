using Neo.ConsoleService;
using Neo.Cryptography.ECC;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "start consensus" command
        /// </summary>
        [ConsoleCommand("start consensus", Category = "Consensus Commands")]
        private void OnStartConsensusCommand()
        {
            if (NoWallet()) return;
            ShowPrompt = false;
            NeoSystem.StartConsensus(CurrentWallet);
        }

        /// <summary>
        /// Process "vote for validators" command
        /// </summary>
        [ConsoleCommand("vote", Category = "Consensus Commands")]
        private void OnVotingCommand(string[] pubkeysArr, UInt160 account)
        {
            if (NoWallet()) return;
            ECPoint[] pubkeys = pubkeysArr.Select(p => ECPoint.Parse(p, ECCurve.Secp256r1)).ToArray();
            Transaction tx = new Transaction
            {
                Sender = UInt160.Zero,
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = Array.Empty<Witness>(),
                Cosigners = new Cosigner[] { new Cosigner() { Account = account } }
            };
            using (ScriptBuilder scriptBuilder = new ScriptBuilder())
            {
                scriptBuilder.EmitAppCall(NativeContract.NEO.Hash, "vote", new ContractParameter
                {
                    Type = ContractParameterType.Hash160,
                    Value = account
                }, new ContractParameter
                {
                    Type = ContractParameterType.Array,
                    Value = pubkeys.Select(p => new ContractParameter
                    {
                        Type = ContractParameterType.PublicKey,
                        Value = p
                    }).ToArray()
                });
                tx.Script = scriptBuilder.ToArray();
            }

            try
            {
                tx = CurrentWallet.MakeTransaction(tx.Script, null, tx.Attributes, tx.Cosigners);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Error: insufficient balance.");
                return;
            }
            SignAndSendTx(tx);
        }
    }
}
