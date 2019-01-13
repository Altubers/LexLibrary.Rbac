﻿using LexLibrary.Rbac.Extensions;
using LexLibrary.Rbac.Models;
using LexLibrary.Rbac.Models.Consts;
using LexLibrary.Rbac.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LexLibrary.Rbac.Attributes
{
    public class LexLibraryRbacAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string FunctionIds { get; set; }

        public string RoleIds { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var services = context.HttpContext.RequestServices;
            var userManager = services.GetService<IUserManager>();
            var setting = services.GetService<LexLibraryRbacSetting>();
            var request = context.HttpContext.Request;

            // 跳過驗證
            if (context.ActionDescriptor.FilterDescriptors.Any(x => x.Filter is AllowAnonymousFilter))
            {
                return;
            }

            if (!userManager.IsLogin)
            {
                string returnUrl = request.GetDisplayUrl();
                string url = string.Format("{0}?{1}={2}",
                                           setting.LoginPath.ToString().TrimEnd('/'),
                                           ApplicationConst.ReturnUrl,
                                           HttpUtility.UrlEncode(returnUrl));

                context.Result = new RedirectResult(url);
                return;
            }

            IEnumerable<int> functionIds = null;
            if (!string.IsNullOrWhiteSpace(FunctionIds))
            {
                functionIds = FunctionIds
                                .Split(',')
                                .Select(x => int.TryParse(x, out int id) ? id : -1)
                                .Where(x => x > 0);
            }

            IEnumerable<int> roleIds = null;
            if (!string.IsNullOrWhiteSpace(RoleIds))
            {
                roleIds = RoleIds
                            .Split(',')
                            .Select(x => int.TryParse(x, out int id) ? id : -1)
                            .Where(x => x > 0);
            }

            if ((functionIds != null || roleIds != null) &&
                !userManager.HasPermission(functionIds, roleIds))
            {
                string url = null;

                if (request.Headers.ContainsKey("Referer"))
                {
                    url = request.Headers["Referer"].ToString();
                    if (string.Equals(url, request.GetDisplayUrl(), StringComparison.OrdinalIgnoreCase))
                    {
                        url = request.GetBaseUrl();
                    }
                }
                else
                {
                    url = request.GetBaseUrl();
                }

                context.Result = new RedirectResult(url);
                return;
            }
        }
    }
}
