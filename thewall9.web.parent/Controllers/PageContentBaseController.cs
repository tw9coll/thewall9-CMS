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
    public class PageContentBaseController : Controller
    {
        PageBLL PageService = new PageBLL();
        ProductBLL ProductService = new ProductBLL();
        private const int PAGE_SIZE = 10;

        public ActionResult Index(string FriendlyUrl)
        {
            if (string.IsNullOrEmpty(FriendlyUrl))
            {
                if (!string.IsNullOrEmpty(APP._CurrentFriendlyUrl))
                    return Redirect("/" + APP._CurrentFriendlyUrl);
            }
            var _Model = PageService.Get(FriendlyUrl, APP._SiteID, Request.Url.Authority);
            if (_Model == null)
                throw new HttpException(404, "Page Not Found");
            else
            {
                if (!string.IsNullOrEmpty(_Model.Page.RedirectUrl))
                    return Redirect(_Model.Page.RedirectUrl);
                ViewBag.Title = _Model.Page.TitlePage;
                ViewBag.MetaDescription = _Model.Page.MetaDescription;
                ViewBag.Active = "page-" + FriendlyUrl;
                APP._CurrentLang = _Model.Page.CultureName;
                return View(_Model.Page.ViewRender, _Model);
            }
        }
        public ActionResult Error()
        {
            return View();
        }
        [Route("sitemap.xml")]
        [OutputCache(Duration = 12 * 3600, VaryByParam = "*")]
        public ActionResult SiteMap()
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var items = PageService.GetSitemap(APP._SiteID, Request.Url.Authority);
            var sitemap = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset",
                from i in items
                select
                new XElement(ns + "url",
                    new XElement(ns + "loc", Request.Url.Scheme + "://" + Request.Url.Authority + "/" + i.FriendlyUrl),
                    new XElement(ns + "lastmod", String.Format("{0:yyyy-MM-dd}", DateTime.Now)),
                    new XElement(ns + "changefreq", "always"),
                    new XElement(ns + "priority", "0.5")
            )));
            return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + sitemap.ToString(), "text/xml");
        }
        [Route("change-lang")]
        public ActionResult ChangeLang(string Lang, string FriendlyUrl)
        {
            HttpCookie cookie = Request.Cookies["_Culture"];
            if (cookie != null)
                cookie.Value = Lang;
            else
            {
                cookie = new HttpCookie("_Culture");
                cookie.Value = Lang;
                cookie.Expires = DateTime.Now.AddYears(1);
            }
            Response.Cookies.Add(cookie);
            var _UrlRedirect = "/" + PageService.GetPageFriendlyUrl(APP._SiteID, Request.Url.Authority, FriendlyUrl, Lang);
            return Redirect(_UrlRedirect);
        }
        [Route("change-currency")]
        public ActionResult ChangeCurrency(int CurrencyID, string Url)
        {
            APP._CurrentCurrencyID = CurrencyID;
            return Redirect(Url);
        }

        #region Products
        public ActionResult Products(string FriendlyUrl, string CategoryFriendlyUrl, int CategoryID = 0, int Page = 1)
        {
            return Index(FriendlyUrl);
        }
        public ActionResult Product(string FriendlyUrl)
        {
            var _Model = ProductService.Get(APP._SiteID, Request.Url.Authority,FriendlyUrl, APP._CurrentCurrencyID);
            if (_Model == null)
                throw new HttpException(404, "Page Not Found");
            else
            {
                ViewBag.Title = _Model.ProductName;
                ViewBag.MetaDescription = _Model.ProductName+" "+_Model.Price;
                //ViewBag.Active = "page-" + FriendlyUrl;
                APP._CurrentLang = _Model.CultureName;
                return View(_Model);
            }
        }
        public PartialViewResult GetProducts(int CategoryID = 0, int Page = 1)
        {
            var _P = ProductService.Get(APP._SiteID, Request.Url.Authority, APP._CurrentLang, null, APP._CurrentCurrencyID, CategoryID, PAGE_SIZE, Page);
            return PartialView("_Products", _P);
        }
        #endregion
    }
}