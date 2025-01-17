﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using Lexplorer.Models;
using System.Threading.Tasks;
using System.Threading;

namespace Lexplorer.Services
{
    public class NftMetadataService : IDisposable
    {
        const string _ipfsBaseUrl = "https://fudgey.mypinata.cloud/ipfs/";

        readonly RestClient _client;

        private string makeAbsolutelink(string link)
        {
            return link.StartsWith("ipfs://") ? _ipfsBaseUrl + link.Remove(0, 7) : link; //remove the ipfs portion and add base
        }

        public NftMetadataService()
        {
            _client = new RestClient();
        }

        public void Dispose()
        {
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<NftMetadata?> GetMetadata(string link, CancellationToken cancellationToken = default)
        {
            link = makeAbsolutelink(link);
            //there is a fallback for if the metadata fails on the first try because
            //loopring deployed two different contracts for the nfts so some
            //metadata.json needs to be referenced directly while others are in a folder in ipfs
            NftMetadata? nmd = await GetMetadataFromURL(link, cancellationToken);
            if (nmd == null)
                nmd = await GetMetadataFromURL(link + "/metadata.json", cancellationToken);
            return nmd;
        }

        private async Task<NftMetadata?> GetMetadataFromURL(string URL, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest(URL);
            try
            {
                request.Timeout = 10000; //we can't afford to wait forever here, 10s must be enough
                var response = await _client.GetAsync(request, cancellationToken);
                return JsonConvert.DeserializeObject<NftMetadata>(response.Content!);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.StackTrace + "\n" + e.Message);
                return null;
            }
        }

        public async Task<string?> GetContentTypeFromURL(string link, CancellationToken cancellationToken = default)
        {
            link = makeAbsolutelink(link);
            var request = new RestRequest(link, Method.Head);
            try
            {
                request.Timeout = 20000; //we can't afford to wait forever here, 20s must be enough
                var response = await _client.HeadAsync(request, cancellationToken); //Send head request so we only get header not the content
                return response?.ContentType;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.StackTrace + "\n" + e.Message);
                return null;
            }
        }

    }
}
