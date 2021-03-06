using Neo.ConsoleService;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Globalization;
using System.Linq;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "transfer" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        /// <param name="to">To</param>
        /// <param name="amount">Ammount</param>
        /// <param name="from">From</param>
        /// <param name="signersAccounts">Signer's accounts</param>
        [ConsoleCommand("transfer", Category = "NEP17 Commands")]
        private void OnTransferCommand(UInt160 tokenHash, UInt160 to, decimal amount, string data = null, UInt160 from = null, UInt160[] signersAccounts = null)
        {
            var asset = new AssetDescriptor(tokenHash);
            var value = BigDecimal.Parse(amount.ToString(CultureInfo.InvariantCulture), asset.Decimals);

            if (NoWallet()) return;

            Transaction tx;
            try
            {
                tx = CurrentWallet.MakeTransaction(new[]
                {
                    new TransferOutput
                    {
                        AssetId = tokenHash,
                        Value = value,
                        ScriptHash = to,
                        Data = data
                    }
                }, from: from, cosigners: signersAccounts?.Select(p => new Signer
                {
                    // default access for transfers should be valid only for first invocation
                    Scopes = WitnessScope.CalledByEntry,
                    Account = p
                })
                .ToArray() ?? new Signer[0]);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Error: " + GetExceptionMessage(e));
                return;
            }
            if (!ReadUserInput("Relay tx(no|yes)").IsYes())
            {
                return;
            }
            SignAndSendTx(tx);
        }

        /// <summary>
        /// Process "balanceOf" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        /// <param name="address">Address</param>
        [ConsoleCommand("balanceOf", Category = "NEP17 Commands")]
        private void OnBalanceOfCommand(UInt160 tokenHash, UInt160 address)
        {
            var arg = new JObject();
            arg["type"] = "Hash160";
            arg["value"] = address.ToString();

            var asset = new AssetDescriptor(tokenHash);

            if (!OnInvokeWithResult(tokenHash, "balanceOf", out StackItem balanceResult, null, new JArray(arg))) return;

            var balance = new BigDecimal(((PrimitiveType)balanceResult).GetInteger(), asset.Decimals);

            Console.WriteLine();
            Console.WriteLine($"{asset.AssetName} balance: {balance}");
        }

        /// <summary>
        /// Process "name" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        [ConsoleCommand("name", Category = "NEP17 Commands")]
        private void OnNameCommand(UInt160 tokenHash)
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ContractState contract = NativeContract.ContractManagement.GetContract(snapshot, tokenHash);
            if (contract == null) Console.WriteLine($"Contract hash not exist: {tokenHash}");
            else Console.WriteLine($"Result : {contract.Manifest.Name.ToString()}");
        }

        /// <summary>
        /// Process "decimals" command
        /// </summary>
        /// <param name="tokenHash">Script hash</param>
        [ConsoleCommand("decimals", Category = "NEP17 Commands")]
        private void OnDecimalsCommand(UInt160 tokenHash)
        {
            if (!OnInvokeWithResult(tokenHash, "decimals", out StackItem result, null)) return;

            Console.WriteLine($"Result : {((PrimitiveType)result).GetInteger()}");
        }
    }
}
