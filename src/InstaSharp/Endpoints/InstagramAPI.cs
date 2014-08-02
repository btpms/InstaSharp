﻿using System.Security.Cryptography;
using InstaSharp.Extensions;
using InstaSharp.Models.Responses;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
namespace InstaSharp.Endpoints
{
    public class InstagramApi
    {
        internal String ips;
        internal string XInstaForwardedHeader { get; set; }

        public InstagramConfig InstagramConfig { get; private set; }

        public OAuthResponse OAuthResponse { get; private set; }

        internal HttpClient Client { get; private set; }

        /// <summary>
        ///   IP information: Comma-separated list of one or more IPs; if your app receives requests directly from clients,
        ///  then it should be the client's remote IP as detected by the your app's load balancer; if your app is behind another load balancer (for example, Amazon's ELB),
        ///  this should contain the exact contents of the original X-Forwarded-For header. You can use the 127.0.0.1 loopback address during testing
        /// </summary>
        public string Ips
        {
            set
            {
                ips = value;
                XInstaForwardedHeader = CreateXInstaForwardedHeader();
            }
        }

        public bool EnforceSignedHeader { get; set; }

        public InstagramApi(string endpoint, InstagramConfig instagramConfig)
            : this(endpoint, instagramConfig, null)
        {

        }

        public InstagramApi(string endpoint, InstagramConfig instagramConfig, OAuthResponse oauthResponse)
        {
            InstagramConfig = instagramConfig;
            OAuthResponse = oauthResponse;

            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip |
                                                 DecompressionMethods.Deflate;
            }

            Client = new HttpClient(handler) { BaseAddress = new Uri(new Uri(InstagramConfig.ApiUri), endpoint) };
        }

        protected void AssertIsAuthenticated()
        {
            if (OAuthResponse == null || OAuthResponse.User == null)
            {
                throw new InvalidOperationException("You are not authenticated");
            }
        }

        internal HttpRequestMessage Request(string fragment, HttpMethod method)
        {
            var request = new HttpRequestMessage(method, new Uri(Client.BaseAddress, fragment));
            AddHeaders(request);
            return AddAuth(request);
        }

        /// <param name="request"></param>
        private void AddHeaders(HttpRequestMessage request)
        {
            if (EnforceSignedHeader && !String.IsNullOrWhiteSpace(InstagramConfig.ClientSecret)) //tODO :check config is set some
            {
                request.Headers.Add("X-Insta-Forwarded-For", XInstaForwardedHeader);
            }
        }

        /// <summary>
        /// You can help us better identify API calls from your app by making server-side calls with a HTTP header named X-Insta-Forwarded-For
        /// signed using your Client Secret. This header is optional, but recommended for any app making server-to-server calls. To enable this
        /// setting, edit your OAuth Client configuration and mark the Enforce signed header checkbox. When enabled, Instagram will check for 
        /// the X-Insta-Forwarded-For HTTP header and verify its signature. 
        /// HMAC signed using the SHA256 hash algorithm with your client's IP address and Client Secret.
        /// </summary>
        /// <returns></returns>
        /// TODO: only internal for unit testing purposes. Move to another class?
        public string CreateXInstaForwardedHeader()
        {
            var encoding = new ASCIIEncoding();
            var hash = new HMACSHA256(encoding.GetBytes(InstagramConfig.ClientSecret)).ComputeHash(encoding.GetBytes(ips));
            var digest = hash.ByteArrayToString().ToLower(); //TODO: can the ToLower() be avoided
            return string.Format("{0}|{1}", ips, digest);
        }

        internal HttpRequestMessage Request(string fragment)
        {
            return Request(fragment, HttpMethod.Get);
        }

        internal virtual HttpRequestMessage AddAuth(HttpRequestMessage request)
        {
            if (OAuthResponse == null)
            {
                request.AddParameter("client_id", InstagramConfig.ClientId);
            }
            else
            {
                request.AddParameter("access_token", OAuthResponse.Access_Token);
            }

            return request;
        }
    }
}
