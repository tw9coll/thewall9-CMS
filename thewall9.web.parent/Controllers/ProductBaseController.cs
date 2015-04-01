﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using thewall9.web.parent.BLL;
using thewall9.web.parent.HtmlHelpers;

namespace thewall9.web.parent.Controllers
{
    public class ProductBaseController : Controller
    {
        ProductBLL ProductService = new ProductBLL();
        private const int PAGE_SIZE = 10;

        public PartialViewResult GetProducts(int CategoryID = 0, int Page = 1)
        {
            var _P = ProductService.Get(APP._SiteID, Request.Url.Authority, APP._CurrentLang, null, APP._CurrentCurrencyID, CategoryID, PAGE_SIZE, Page);
            return PartialView("_Products", _P);
        }
        public ViewResult Index(string FriendlyUrl, int CategoryID = 0, int Page = 1)
        {
            var _P = ProductService.Get(APP._SiteID, Request.Url.Authority, APP._CurrentLang, FriendlyUrl, APP._CurrentCurrencyID, CategoryID, PAGE_SIZE, Page);
            APP._CurrentLang= _P.CultureName;
            ViewBag.Active = "products";
            return View(_P);
        }
    }
}