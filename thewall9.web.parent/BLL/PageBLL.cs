﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using thewall9.data.binding;
using Newtonsoft.Json;
using Elmah;

namespace thewall9.web.parent.BLL
{
    public class PageBLL:BaseBLL
    { 
        public PageWeb Get(string FriendlyUrl, int SiteID, string Url)
        {
            return DownloadObject<PageWeb>("api/page?SiteID=" + SiteID + "&Url=" + Url + "&FriendlyUrl=" + FriendlyUrl);
        }
        public PageWeb GetByAlias(int SiteID, string Url, string Alias, string Lang)
        {
            return DownloadObject<PageWeb>("api/page?SiteID=" + SiteID + "&Url=" + Url + "&Alias=" + Alias + "&Lang=" + Lang);
        }
        public string GetPageFriendlyUrl(int SiteID, string Url, string FriendlyUrl, string TargetLang)
        {
            return DownloadObject<string>("api/page?SiteID=" + SiteID + "&Url=" + Url + "&FriendlyUrl=" + FriendlyUrl+"&TargetLang="+TargetLang);
        }
        public SiteMapModel GetSitemap(int SiteID, string Url)
        {
            return DownloadObject<SiteMapModel>("api/page/sitemap?SiteID=" + SiteID + "&Url=" + Url);
        }
    }
}
