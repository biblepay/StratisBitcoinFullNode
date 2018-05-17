﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using NBitcoin;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.Consensus.Interfaces;
using Stratis.Bitcoin.Features.SmartContracts.Models;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Utilities.JsonErrors;
using Stratis.SmartContracts;
using Stratis.SmartContracts.Core;
using Stratis.SmartContracts.Core.Backend;
using Stratis.SmartContracts.Core.Serialization;
using Stratis.SmartContracts.Core.State;

namespace Stratis.Bitcoin.Features.SmartContracts.Controllers
{
    [Route("api/[controller]")]
    public class SmartContractsController : Controller
    {
        private readonly ContractStateRepositoryRoot stateRoot;
        private readonly IConsensusLoop consensus;
        private readonly IWalletTransactionHandler walletTransactionHandler;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILogger logger;
        private readonly Network network;
        private readonly CoinType coinType;
        private readonly IWalletManager walletManager;
        private readonly IBroadcasterManager broadcasterManager;

        public SmartContractsController(
            IBroadcasterManager broadcasterManager,
            IConsensusLoop consensus,
            IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory,
            Network network,
            ContractStateRepositoryRoot stateRoot,
            IWalletManager walletManager,
            IWalletTransactionHandler walletTransactionHandler)
        {
            this.stateRoot = stateRoot;
            this.consensus = consensus;
            this.walletTransactionHandler = walletTransactionHandler;
            this.dateTimeProvider = dateTimeProvider;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.network = network;
            this.coinType = (CoinType)network.Consensus.CoinType;
            this.walletManager = walletManager;
            this.broadcasterManager = broadcasterManager;
        }

        [Route("code")]
        [HttpGet]
        public IActionResult GetCode([FromQuery]string address)
        {
            uint160 addressNumeric = new Address(address).ToUint160(this.network);
            byte[] contractCode = this.stateRoot.GetCode(addressNumeric);

            if (contractCode == null || !contractCode.Any())
            {
                return Json(new GetCodeResponse
                {
                    Message = string.Format("No contract execution code exists at {0}", address)
                });
            }

            using (var memStream = new MemoryStream(contractCode))
            {
                var modDefinition = ModuleDefinition.ReadModule(memStream);
                var decompiler = new CSharpDecompiler(modDefinition, new DecompilerSettings { });
                string cSharp = decompiler.DecompileAsString(modDefinition.Types.FirstOrDefault(x => x.FullName != "<Module>"));
                return Json(new GetCodeResponse
                {
                    Message = string.Format("Contract execution code retrieved at {0}", address),
                    Bytecode = contractCode.ToHexString(),
                    CSharp = cSharp
                });
            }
        }

        [Route("balance")]
        [HttpGet]
        public IActionResult GetBalance([FromQuery]string address)
        {
            uint160 addressNumeric = new Address(address).ToUint160(this.network);
            ulong balance = this.stateRoot.GetCurrentBalance(addressNumeric);
            return Json(balance);
        }

        [Route("storage")]
        [HttpGet]
        public IActionResult GetStorage([FromQuery] GetStorageRequest request)
        {
            if (!this.ModelState.IsValid)
                return BuildErrorResponse(this.ModelState);

            uint160 addressNumeric = new Address(request.ContractAddress).ToUint160(this.network);
            byte[] storageValue = this.stateRoot.GetStorageValue(addressNumeric, Encoding.UTF8.GetBytes(request.StorageKey));
            return Json(GetStorageValue(request.DataType, storageValue).ToString());
        }

        [Route("build-create")]
        [HttpPost]
        public IActionResult BuildCreateSmartContractTransaction([FromBody] BuildCreateContractTransactionRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return BuildErrorResponse(this.ModelState);
            }

            return Json(BuildCreateTx(request));
        }

        [Route("build-call")]
        [HttpPost]
        public IActionResult BuildCallSmartContractTransaction([FromBody] BuildCallContractTransactionRequest request)
        {
            if (!this.ModelState.IsValid)
                return BuildErrorResponse(this.ModelState);

            return Json(BuildCallTx(request));
        }


        [Route("build-and-send-create")]
        [HttpPost]
        public IActionResult BuildAndSendCreateSmartContractTransaction([FromBody] BuildCreateContractTransactionRequest request)
        {
            if (!this.ModelState.IsValid)
                return BuildErrorResponse(this.ModelState);

            BuildCreateContractTransactionResponse response = BuildCreateTx(request);
            if (!response.Success)
                return Json(response);

            var transaction = new Transaction(response.Hex);
            this.walletManager.ProcessTransaction(transaction, null, null, false);
            this.broadcasterManager.BroadcastTransactionAsync(transaction).GetAwaiter().GetResult();

            return Json(response);
        }

        [Route("build-and-send-call")]
        [HttpPost]
        public IActionResult BuildAndSendCallSmartContractTransaction([FromBody] BuildCallContractTransactionRequest request)
        {
            if (!this.ModelState.IsValid)
                return BuildErrorResponse(this.ModelState);

            BuildCallContractTransactionResponse response = BuildCallTx(request);
            if (!response.Success)
                return Json(response);

            var transaction = new Transaction(response.Hex);
            this.walletManager.ProcessTransaction(transaction, null, null, false);
            this.broadcasterManager.BroadcastTransactionAsync(transaction).GetAwaiter().GetResult();

            return Json(response);
        }

        [Route("address-balances")]
        [HttpGet]
        public IActionResult GetAddressesWithBalances([FromQuery] string walletName)
        {
            IEnumerable<IGrouping<HdAddress, UnspentOutputReference>> allSpendable = this.walletManager.GetSpendableTransactionsInWallet(walletName, 10).GroupBy(x => x.Address);
            var result = new List<object>();
            foreach (IGrouping<HdAddress, UnspentOutputReference> grouping in allSpendable)
            {
                result.Add(new
                {
                    grouping.Key.Address,
                    Sum = grouping.Sum(x => x.Transaction.SpendableAmount(false))
                });
            }
            return Json(result);
        }

        private BuildCreateContractTransactionResponse BuildCreateTx(BuildCreateContractTransactionRequest request)
        {
            AddressBalance addressBalance = this.walletManager.GetAddressBalance(request.Sender);
            if (addressBalance.AmountConfirmed == 0)
                return BuildCreateContractTransactionResponse.Failed($"The 'Sender' address you're trying to spend from doesn't have a confirmed balance. Current unconfirmed balance: {addressBalance.AmountUnconfirmed}. Please check the 'Sender' address.");

            var selectedInputs = new List<OutPoint>();
            var coinbaseMaturity = Convert.ToInt32(this.network.Consensus.Option<SmartContractConsensusOptions>().CoinbaseMaturity);
            selectedInputs = this.walletManager.GetSpendableTransactionsInWallet(request.WalletName, coinbaseMaturity).Where(x => x.Address.Address == request.Sender).Select(x => x.ToOutPoint()).ToList();
            if (!selectedInputs.Any())
                return BuildCreateContractTransactionResponse.Failed(string.Format("Your wallet does not contain any spendable transactions. Received funds needs to reach maturity of {0}", coinbaseMaturity));

            ulong gasPrice = ulong.Parse(request.GasPrice);
            ulong gasLimit = ulong.Parse(request.GasLimit);

            SmartContractCarrier carrier;
            if (request.Parameters != null && request.Parameters.Any())
                carrier = SmartContractCarrier.CreateContract(ReflectionVirtualMachine.VmVersion, request.ContractCode.HexToByteArray(), gasPrice, new Gas(gasLimit), request.Parameters);
            else
                carrier = SmartContractCarrier.CreateContract(ReflectionVirtualMachine.VmVersion, request.ContractCode.HexToByteArray(), gasPrice, new Gas(gasLimit));

            HdAddress senderAddress = null;
            if (!string.IsNullOrWhiteSpace(request.Sender))
            {
                Wallet.Wallet wallet = this.walletManager.GetWallet(request.WalletName);
                HdAccount account = wallet.GetAccountByCoinType(request.AccountName, this.coinType);
                senderAddress = account.GetCombinedAddresses().FirstOrDefault(x => x.Address == request.Sender);
            }

            ulong totalFee = (gasPrice * gasLimit) + Money.Parse(request.FeeAmount);
            var context = new TransactionBuildContext(
                new WalletAccountReference(request.WalletName, request.AccountName),
                new[] { new Recipient { Amount = 0, ScriptPubKey = new Script(carrier.Serialize()) } }.ToList(),
                request.Password)
            {
                TransactionFee = totalFee,
                ChangeAddress = senderAddress,
                SelectedInputs = selectedInputs
            };

            try
            {
                Transaction transaction = this.walletTransactionHandler.BuildTransaction(context);
                return BuildCreateContractTransactionResponse.Succeeded(transaction, context.TransactionFee, transaction.GetNewContractAddress().ToAddress(this.network));
            }
            catch (Exception exception)
            {
                return BuildCreateContractTransactionResponse.Failed(exception.Message);
            }
        }

        private BuildCallContractTransactionResponse BuildCallTx(BuildCallContractTransactionRequest request)
        {
            AddressBalance addressBalance = this.walletManager.GetAddressBalance(request.Sender);
            if (addressBalance.AmountConfirmed == 0)
                return BuildCallContractTransactionResponse.Failed($"The 'Sender' address you're trying to spend from doesn't have a confirmed balance. Current unconfirmed balance: {addressBalance.AmountUnconfirmed}. Please check the 'Sender' address.");

            var selectedInputs = new List<OutPoint>();
            var coinbaseMaturity = Convert.ToInt32(this.network.Consensus.Option<SmartContractConsensusOptions>().CoinbaseMaturity);
            selectedInputs = this.walletManager.GetSpendableTransactionsInWallet(request.WalletName, coinbaseMaturity).Where(x => x.Address.Address == request.Sender).Select(x => x.ToOutPoint()).ToList();
            if (!selectedInputs.Any())
                return BuildCallContractTransactionResponse.Failed(string.Format("Your wallet does not contain any spendable transactions. Received funds needs to reach maturity of {0}", coinbaseMaturity));

            ulong gasPrice = ulong.Parse(request.GasPrice);
            ulong gasLimit = ulong.Parse(request.GasLimit);
            uint160 addressNumeric = new Address(request.ContractAddress).ToUint160(this.network);

            SmartContractCarrier carrier;
            if (request.Parameters != null && request.Parameters.Any())
                carrier = SmartContractCarrier.CallContract(ReflectionVirtualMachine.VmVersion, addressNumeric, request.MethodName, gasPrice, new Gas(gasLimit), request.Parameters);
            else
                carrier = SmartContractCarrier.CallContract(ReflectionVirtualMachine.VmVersion, addressNumeric, request.MethodName, gasPrice, new Gas(gasLimit));

            HdAddress senderAddress = null;
            if (!string.IsNullOrWhiteSpace(request.Sender))
            {
                Wallet.Wallet wallet = this.walletManager.GetWallet(request.WalletName);
                HdAccount account = wallet.GetAccountByCoinType(request.AccountName, this.coinType);
                senderAddress = account.GetCombinedAddresses().FirstOrDefault(x => x.Address == request.Sender);
            }

            ulong totalFee = (gasPrice * gasLimit) + Money.Parse(request.FeeAmount);
            var context = new TransactionBuildContext(
                new WalletAccountReference(request.WalletName, request.AccountName),
                new[] { new Recipient { Amount = request.Amount, ScriptPubKey = new Script(carrier.Serialize()) } }.ToList(),
                request.Password)
            {
                TransactionFee = totalFee,
                ChangeAddress = senderAddress,
                SelectedInputs = selectedInputs
            };

            try
            {
                Transaction transaction = this.walletTransactionHandler.BuildTransaction(context);
                return BuildCallContractTransactionResponse.Succeeded(request.MethodName, transaction, context.TransactionFee);
            }
            catch (Exception exception)
            {
                return BuildCallContractTransactionResponse.Failed(exception.Message);
            }
        }

        private object GetStorageValue(SmartContractDataType dataType, byte[] bytes)
        {
            PersistentStateSerializer serializer = new PersistentStateSerializer();
            switch (dataType)
            {
                case SmartContractDataType.Address:
                    return serializer.Deserialize<Address>(bytes, this.network);
                case SmartContractDataType.Bool:
                    return serializer.Deserialize<bool>(bytes, this.network);
                case SmartContractDataType.Bytes:
                    return serializer.Deserialize<byte[]>(bytes, this.network);
                case SmartContractDataType.Char:
                    return serializer.Deserialize<char>(bytes, this.network);
                case SmartContractDataType.Int:
                    return serializer.Deserialize<int>(bytes, this.network);
                case SmartContractDataType.Long:
                    return serializer.Deserialize<long>(bytes, this.network);
                case SmartContractDataType.Sbyte:
                    return serializer.Deserialize<sbyte>(bytes, this.network);
                case SmartContractDataType.String:
                    return serializer.Deserialize<string>(bytes, this.network);
                case SmartContractDataType.Uint:
                    return serializer.Deserialize<uint>(bytes, this.network);
                case SmartContractDataType.Ulong:
                    return serializer.Deserialize<ulong>(bytes, this.network);
            }
            return null;
        }

        /// <summary>
        /// Builds an <see cref="IActionResult"/> containing errors contained in the <see cref="ControllerBase.ModelState"/>.
        /// </summary>
        /// <returns>A result containing the errors.</returns>
        private static IActionResult BuildErrorResponse(ModelStateDictionary modelState)
        {
            List<ModelError> errors = modelState.Values.SelectMany(e => e.Errors).ToList();
            return ErrorHelpers.BuildErrorResponse(
                HttpStatusCode.BadRequest,
                string.Join(Environment.NewLine, errors.Select(m => m.ErrorMessage)),
                string.Join(Environment.NewLine, errors.Select(m => m.Exception?.Message)));
        }
    }
}