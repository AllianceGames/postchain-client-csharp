using Chromia.Encoding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Chromia.TransactionReceipt;

namespace Chromia.Transport
{
    internal class RestClient
    {
        private enum Request
        {
            Get,
            PostBytes,
            PostJson
        }

        private enum RequestType
        {
            Features,
            Query,
            Tx,
            TxStatus,
        }

        private struct FeaturesResponse
        {
            [JsonProperty("merkle_hash_version")]
            public int MerkleHashVersion { get; set; }
        }

        public Buffer BlockchainRID { get { return _blockchainRID; } }
        public int PollingRetries { get { return _pollingRetries; } }
        public int PollingInterval { get { return _pollingInterval; } }
        public int AttemptsPerEndpoint { get { return _attemptsPerEndpoint; } }
        public int AttemptInterval { get { return _attemptInterval; } }

        private readonly Buffer _blockchainRID;
        private readonly List<Uri> _nodeUrls;
        private int _pollingRetries = 10;
        private int _pollingInterval = 500;
        private int _attemptsPerEndpoint = 5;
        private int _attemptInterval = 500;

        private static ITransport _transport = new DefaultTransport();
        private static readonly Random _random = new Random();

        private Uri FeaturesUri(Uri baseUri) => new Uri(baseUri, $"./config/{_blockchainRID.Parse()}/features");
        private Uri QueryUri(Uri baseUri) => new Uri(baseUri, $"./query_gtv/{_blockchainRID.Parse()}");
        private Uri TxUri(Uri baseUri) => new Uri(baseUri, $"./tx/{_blockchainRID.Parse()}");
        private Uri TxStatusUri(Uri baseUri, Buffer transactionRID) => new Uri(baseUri, $"./tx/{_blockchainRID.Parse()}/{transactionRID.Parse()}/status");

        public RestClient(List<Uri> nodeUrl, Buffer blockchainRID)
        {
            _nodeUrls = nodeUrl;
            _blockchainRID = blockchainRID;
        }

        #region Static
        public static void SetTransport(ITransport transport)
        {
            _transport = transport;
        }

        public async static Task<Buffer> GetBlockchainRID(Uri nodeUri, int blockchainIID, CancellationToken ct)
        {
            var uri = new Uri(nodeUri, "./brid/iid_" + blockchainIID);
            var response = await _transport.Get(uri, ct);
            return Buffer.From(response.ParseUTF8());
        }

        public static void EnsureBlockchainRID(Buffer blockchainRID)
        {
            if (blockchainRID.Length != 32)
                throw new ArgumentOutOfRangeException(nameof(blockchainRID), "has to be 32 bytes");
        }

        public async static Task<List<Uri>> GetNodesFromDirectory(List<Uri> directoryNodeUrls, Buffer blockchainRID, CancellationToken ct)
        {
            var directoryBrid = await GetDirectoryBridFromNodes(directoryNodeUrls, ct);
            var tmpClient = new RestClient(directoryNodeUrls, directoryBrid);
            var queryObject = new Dictionary<string, object>()
            {
                { "blockchain_rid", blockchainRID }
            };
            var result = await tmpClient.Query<List<string>>("cm_get_blockchain_api_urls", queryObject, ct);
            return result.Select(n => new Uri(n)).ToList();
        }

        private async static Task<Buffer> GetDirectoryBridFromNodes(List<Uri> directoryNodeUrls, CancellationToken ct) {
            Exception lastException = new TransportException(TransportException.ReasonCode.MalformedUri, "no directory nodes found");
            
            foreach (var directoryNodeUrl in directoryNodeUrls)
            {
                try
                {
                    return await GetBlockchainRID(directoryNodeUrl, 0, ct);
                }
                catch (Exception e)
                {
                    lastException = e;
                }
            }
            throw lastException;
        }

        public async static Task<int> GetHashVersion(List<Uri> nodeUrls, Buffer blockchainRID, CancellationToken ct)
        {
            var tmpClient = new RestClient(nodeUrls, blockchainRID);
            try
            {
                var result = await tmpClient.RequestWithRetries(
                    RequestType.Features,
                    Request.Get,
                    attempts: 1,
                    attemptInterval: 0,
                    ct: ct
                );
                var response = JsonConvert.DeserializeObject<FeaturesResponse>(result.ParseUTF8());
                if (response.MerkleHashVersion == 0)
                {
                    return 1;
                }
                return response.MerkleHashVersion;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        #endregion

        #region Setter
        public void AddNodeUrl(Uri nodeUrl)
        {
            _nodeUrls.Add(nodeUrl);
        }

        public void AddNodeUrl(List<Uri> nodeUrl)
        {
            _nodeUrls.AddRange(nodeUrl);
        }

        public void SetPollingInterval(int pollingInterval)
        {
            _pollingInterval = pollingInterval;
        }

        public void SetPollingRetries(int pollingRetries)
        {
            _pollingRetries = pollingRetries;
        }

        public void SetAttemptsPerEndpoint(int attemptsPerEndpoint)
        {
            _attemptsPerEndpoint = attemptsPerEndpoint;
        }

        public void SetAttemptInterval(int attemptInterval)
        {
            _attemptInterval = attemptInterval;
        }
        #endregion

        public async Task<T> Query<T>(string name, object parameters, CancellationToken ct)
        {
            var queryObject = new object[] { name, parameters };
            var buffer = Gtv.Encode(queryObject);
            var response = await RequestWithRetries(RequestType.Query, Request.PostBytes, buffer, ct: ct);
            var jsonObj = Gtv.Decode(response);
            if (jsonObj == null)
            {
                return default;
            }

            try
            {
                var settings = new JsonSerializerSettings()
                {
                    Converters = new List<JsonConverter> { new BigIntegerConverter(), new BufferConverter() },
                    ContractResolver = new PostchainPropertyContractResolver()
                };
                var jToken = JToken.FromObject(jsonObj, JsonSerializer.Create(settings));
                if (typeof(T) == typeof(float))
                    return (T)(object)float.Parse(jToken.ToObject<string>(), CultureInfo.InvariantCulture);
                else if (typeof(T) == typeof(double))
                    return (T)(object)double.Parse(jToken.ToObject<string>(), CultureInfo.InvariantCulture);
                else if (typeof(T) == typeof(Buffer))
                    return (T)jsonObj;
                else
                    return jToken.ToObject<T>(JsonSerializer.Create(settings));
            }
            catch (Exception e)
            {
                throw new ChromiaException($"failed to parse return data: {e.Message}");
            }
        }

        public Task<Buffer> SendTransaction(Transaction.Signed tx, CancellationToken ct)
        {
            var txObject = new JObject()
            {
                { "tx", tx.GtvBody.Parse() }
            };

            return RequestWithRetries(RequestType.Tx, Request.PostJson, JsonConvert.SerializeObject(txObject), ct: ct);
        }

        public async Task<TransactionStatusResponse> GetTransactionStatus(Buffer transactionRID, CancellationToken ct)
        {
            var response = await RequestWithRetries(RequestType.TxStatus, Request.Get, transactionRID, ct: ct);
            return JsonConvert.DeserializeObject<TransactionStatusResponse>(response.ParseUTF8());
        }

        public async Task<TransactionReceipt> WaitForConfirmation(Buffer transactionRID, int retry = 0, CancellationToken ct = default)
        {
            var txStatus = await GetTransactionStatus(transactionRID, ct);
            if (IsWaitingStatus(txStatus) && retry < _pollingRetries)
            {
                await _transport.Delay(_pollingInterval, ct);
                return await WaitForConfirmation(transactionRID, retry + 1, ct);
            }
            return new TransactionReceipt(transactionRID, txStatus, retry >= _pollingRetries);
        }

        // "Waiting" happens when we ask a node that has seen the transaction but the transaction has not been put in a block.
        // "Unknown" happens when we ask a node that has not even seen the transaction.
        private bool IsWaitingStatus(TransactionStatusResponse txStatus) {
            return txStatus.Status == ResponseStatus.Waiting || txStatus.Status == ResponseStatus.Unknown;
        }


        private async Task<Buffer> RequestWithRetries(
            RequestType type,
            Request request,
            object content = null,
            int? attempts = null,
            int? attemptInterval = null,
            CancellationToken ct = default
        )
        {
            var response = Buffer.Empty();
            var lastException = new TransportException(TransportException.ReasonCode.MalformedUri, "no nodes found");
            attempts ??= _attemptsPerEndpoint;
            attemptInterval ??= _attemptInterval;

            foreach (var endpoint in _nodeUrls.OrderBy(_ => _random.Next()))
            {
                var uri = type switch
                {
                    RequestType.Features => FeaturesUri(endpoint),
                    RequestType.Query => QueryUri(endpoint),
                    RequestType.Tx => TxUri(endpoint),
                    RequestType.TxStatus => TxStatusUri(endpoint, (Buffer)content),
                    _ => throw new NotSupportedException($"request type {type} not supported")
                };

                for (var attempt = 0; attempt < attempts; attempt++)
                {
                    try
                    {
                        return request switch
                        {
                            Request.Get => await _transport.Get(uri, ct),
                            Request.PostBytes => await _transport.Post(uri, (Buffer)content, ct),
                            Request.PostJson => await _transport.Post(uri, (string)content, ct),
                            _ => throw new NotSupportedException($"request {request} not supported")
                        };
                    }
                    catch (TransportException e)
                    {
                        lastException = e;
                        if (attemptInterval.HasValue && attemptInterval.Value > 0)
                        {
                            await _transport.Delay(attemptInterval.Value, ct);
                        }
                    }
                }
            }

            throw lastException;
        }
    }
}
