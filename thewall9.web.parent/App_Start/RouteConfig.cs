﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace thewall9.web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("robots.txt");
            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "Error",
                url: "error",
                defaults: new { controller = "Page", action = "Error" }
            );

            //PRODUCTS
            routes.MapRoute(
                name: "Product",
                url: "p/{FriendlyUrl}",
                defaults: new { controller = "Page", action = "Product" }
            );
            routes.MapRoute(
                name: "GetProductsPartialView",
                url: "get-products/{CategoryFriendlyUrl}/{CategoryID}/{Page}",
                defaults: new { controller = "Page", action = "GetProducts", CategoryFriendlyUrl = UrlParameter.Optional, CategoryID = UrlParameter.Optional, Page = UrlParameter.Optional }
            );
            routes.MapRoute(
                name: "ProductsInCategory",
                url: "{FriendlyUrl}/{CategoryFriendlyUrl}/{CategoryID}/{Page}",
                defaults: new { controller = "Page", action = "Products", Page = UrlParameter.Optional }
            );
            routes.MapRoute(
                name: "Products",
                url: "{FriendlyUrl}/{Page}",
                defaults: new { controller = "Page", action = "Products", Page = UrlParameter.Optional }
            );

            //DEFAULT ROUTE
            routes.MapRoute(
                name: "Default",
                url: "{FriendlyUrl}",
                defaults: new { controller = "Page", action = "Index", FriendlyUrl = UrlParameter.Optional }
            );
        }
    }
}
