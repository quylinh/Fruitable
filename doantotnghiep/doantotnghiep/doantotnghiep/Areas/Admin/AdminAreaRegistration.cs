﻿using System.Web.Mvc;

namespace doantotnghiep.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName {
            get {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Admin_default",
                "Admin/{controller}/{action}/{id}",
                new
                {
                    controller = "AdminHome", action = "Index", id = UrlParameter.Optional
                },
                namespaces: new[] { "doantotnghiep.Areas.Admin.Controllers" }
            );
        }
    }
}
