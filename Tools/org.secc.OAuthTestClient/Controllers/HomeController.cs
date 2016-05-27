﻿using System;
using System.Configuration;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using DotNetOpenAuth.OAuth2;

namespace Arena.Custom.SECC.AuthCodeGrantTest.Web.Controllers
{
    public class HomeController : Controller
    {
        private WebServerClient mWebServerClient;
        // GET: Home
        public ActionResult Index()
        {
            ViewBag.AccessToken = Request.Form["AccessToken"] ?? "";
            ViewBag.RefreshToken = Request.Form["RefreshToken"] ?? "";
            ViewBag.Action = "";
            ViewBag.ApiResponse = "";
            
            InitializeWebServerClient();
            var accessToken = Request.Form["AccessToken"];
            var resourceServerUri = new Uri( AppSettingValue( "ResourceServerUrl" ) );

            if ( string.IsNullOrEmpty( accessToken ) )
            {
                var authState = mWebServerClient.ProcessUserAuthorization( Request );
                if ( authState != null )
                {
                    ViewBag.AccessToken = authState.AccessToken;
                    ViewBag.RefreshToken = authState.RefreshToken;
                    ViewBag.Action = Request.Path;
                }
            }

            if ( !String.IsNullOrEmpty( Request.Form.Get( "submit.Authorize" ) ) )
            {
                var userAuth = mWebServerClient.PrepareRequestUserAuthorization( new[] { "profile", "family" } );
                userAuth.Send( HttpContext );
                Response.End();
            }
            else if(!string.IsNullOrEmpty(Request.Form.Get("submit.Refresh")))
            {
                var state = new AuthorizationState
                {
                    AccessToken = Request.Form["AccessToken"],
                    RefreshToken = Request.Form["RefreshToken"]

                };
                if ( mWebServerClient.RefreshAuthorization( state ) )
                {
                    ViewBag.AccessToken = state.AccessToken;
                    ViewBag.RefreshToken = state.RefreshToken;
                }
            }

  

            else if ( !string.IsNullOrEmpty( Request.Form.Get( "submit.CallApi" ) ) )
            {

                //try
                //{
                    var meEndpoint = string.Format( "{0}api/oauth/profile", AppSettingValue( "ResourceServerBaseEndpoint" ) );
                    var client = new HttpClient( mWebServerClient.CreateAuthorizingHandler( accessToken ) );
                    var body = client.GetStringAsync( new Uri( resourceServerUri, meEndpoint ) ).Result;
                    ViewBag.ApiResponse = body;
                //}
                //catch ( Exception ex )
                //{
                //    ViewBag.ApiResponse = new Uri( resourceServerUri, string.Format( "{0}api/me", AppSettingValue( "ResourceServerBaseEndpoint" ) ) ).ToString();
                //}


            }
            else if ( !string.IsNullOrEmpty( Request.Form.Get( "submit.CallApiFamily" ) ) )
            {
                var familyEndpoint = string.Format( "{0}api/oauth/family", AppSettingValue( "ResourceServerBaseEndpoint" ) );
                var client = new HttpClient( mWebServerClient.CreateAuthorizingHandler( accessToken ) );
                var body = client.GetStringAsync( new Uri( resourceServerUri, familyEndpoint ) ).Result;
                ViewBag.ApiResponse = body;
            }
            return View();
        }

        private string AppSettingValue( string key )
        {
            if ( ConfigurationManager.AppSettings[key] == null )
            {
                return null;
            }
            else
            {
                return ConfigurationManager.AppSettings[key];
            }
        }

        private void InitializeWebServerClient()
        {
            var authServerUri = new Uri( AppSettingValue( "AuthServerUrl" ) );

            var authServer = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri( authServerUri, AppSettingValue( "AuthorizationEndpoint" ) ),
                TokenEndpoint = new Uri( authServerUri, AppSettingValue( "TokenEndpoint" ) )
            };
            mWebServerClient = new WebServerClient( authServer, AppSettingValue( "ClientId" ), AppSettingValue( "ClientSecret" ) );
        }
    }
}