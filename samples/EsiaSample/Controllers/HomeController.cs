﻿using AISGorod.AspNetCore.Authentication.Esia;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EsiaSample.Controllers
{
    public class HomeController : Controller
    {
        private IEsiaRestService esiaRestService;

        public HomeController(IEsiaRestService esiaRestService)
        {
            this.esiaRestService = esiaRestService;
        }

        public async Task<IActionResult> Index()
        {
            // This is what [Authorize] calls
            var userResult = await HttpContext.AuthenticateAsync();
            var props = userResult.Properties;
            ViewBag.UserProps = props?.Items;

            return View();
        }

        public IActionResult SignIn(string scopes)
        {
            return Challenge(new OpenIdConnectChallengeProperties()
            {
                RedirectUri = Url.Action("Index", "Home"),
                Scope = string.IsNullOrEmpty(scopes) ? null : scopes.Split(' ')
            }, "Esia");
        }

        [Authorize]
        public IActionResult SignOut()
        {
            return SignOut("Cookies", "Esia");
        }

        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            await esiaRestService.RefreshTokensAsync();
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Api()
        {
            var oId = User.Claims.First(i => i.Type == "sub").Value;

            ViewBag.Url = $"/rs/prns/{oId}/ctts?embed=(elements)";
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Api(string url, string method)
        {
            string result = default(string);
            try
            {
                HttpMethod httpMethod = default(HttpMethod);
                switch (method)
                {
                    case "get":
                        httpMethod = HttpMethod.Get;
                        break;
                    case "post":
                        httpMethod = HttpMethod.Post;
                        break;
                    case "put":
                        httpMethod = HttpMethod.Put;
                        break;
                    case "delete":
                        httpMethod = HttpMethod.Delete;
                        break;
                }
                var resultJson = await esiaRestService.CallAsync(url, httpMethod);
                result = resultJson.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                result = ex.Message + "\r\n\r\n" + ex.StackTrace;
            }
            return Content(result);
        }
    }
}