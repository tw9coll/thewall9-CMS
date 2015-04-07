﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using thewall9.bll.Exceptions;
using thewall9.data;
using thewall9.data.binding;
using thewall9.data.Models;

namespace thewall9.bll
{
    public enum ContentRuleExceptionCode
    {
        MORECULTURES = 0
    }
    public class ContentBLL : BaseBLL
    {
        public List<ContentBindingList> Get(int SiteID, string UserID)
        {
            using (var _c = db)
            {
                Can(SiteID, UserID, _c);
                return Get(SiteID);
            }
        }
        public List<ContentBindingList> Get(int SiteID)
        {
            using (var _c = db)
            {
                List<ContentProperty> _List = null;
                _List = (from p in _c.ContentProperties
                         where p.SiteID == SiteID
                         select p).ToList();
                return Order(_List, 0);
            }
        }

        private List<ContentBindingList> Order(List<ContentProperty> Content, int ParentID)
        {
            return (from p in Content
                    where p.ContentPropertyParentID == ParentID
                    select new ContentBindingList
                    {
                        ContentPropertyAlias = p.ContentPropertyAlias,
                        ContentPropertyID = p.ContentPropertyID,
                        ContentPropertyParentID = p.ContentPropertyParentID,
                        Priority = p.Priority,
                        SiteID = p.SiteID,
                        ContentPropertyType = p.ContentPropertyType,
                        Lock = p.Lock,
                        ShowInContent = p.ShowInContent,
                        ContentCultures = p.ContentPropertyCultures.Select(m => new ContentCultureBinding
                        {
                            ContentPropertyID = p.ContentPropertyID,
                            ContentPropertyValue = m.ContentPropertyValue,
                            CultureID = m.CultureID,
                            CultureName = m.Culture.Name,
                            Hint = m.Hint
                        }).ToList(),
                        Items = Content.Where(m => m.ContentPropertyParentID == p.ContentPropertyID).Any() ? Order(Content, p.ContentPropertyID) : new List<ContentBindingList>()
                    }).OrderBy(m => m.Priority).ToList();
        }
        private List<ContentBindingList> GetOrder(List<ContentProperty> Content, int ParentID)
        {
            using (var _c = db)
            {
                return (from p in Content
                        where p.ContentPropertyParentID == ParentID
                        select new ContentBindingList
                        {
                            ContentPropertyAlias = p.ContentPropertyAlias,
                            ContentPropertyID = p.ContentPropertyID,
                            ContentPropertyParentID = p.ContentPropertyParentID,
                            Priority = p.Priority,
                            SiteID = p.SiteID,
                            ContentPropertyType = p.ContentPropertyType,
                            Lock = p.Lock,
                            ContentCultures = p.ContentPropertyCultures.Select(m => new ContentCultureBinding
                            {
                                ContentPropertyID = p.ContentPropertyID,
                                ContentPropertyValue = m.ContentPropertyValue,
                                CultureID = m.CultureID,
                                CultureName = m.Culture.Name
                            }).ToList(),
                            Items = _c.ContentProperties.Where(m => m.ContentPropertyParentID == p.ContentPropertyID).Any() ? GetOrder(_c.ContentProperties.Where(m => m.ContentPropertyParentID == p.ContentPropertyID).ToList(), p.ContentPropertyID) : new List<ContentBindingList>()
                        }).OrderBy(m => m.Priority).ToList();
            }
        }
        public ContentBindingList GetContent(int SiteID, String Url, string AliasList, string Lang)
        {
            using (var _c = db)
            {
                var _Q = SiteID != 0
                   ? from p in _c.ContentProperties
                     where p.SiteID == SiteID
                     select p
                    : from m in _c.ContentProperties
                      join u in _c.SiteUrls on m.Site.SiteID equals u.SiteID
                      where u.Url.Equals(Url)
                      select m;

                List<ContentProperty> _List = null;
                _List = (from p in _Q
                         where p.ContentPropertyAlias.ToLower().Equals(AliasList.ToLower())
                         && p.ContentPropertyType == ContentPropertyType.LIST
                         select p).ToList();
                var _ListBinding = GetOrder(_List, 0, Lang);
                if (_ListBinding == null || _ListBinding.Count == 0)
                    return new ContentBindingList();
                return _ListBinding[0];
            }
        }
        private List<ContentBindingList> GetOrder(List<ContentProperty> Content, int ParentID, string Lang)
        {
            using (var _c = db)
            {
                return (from p in Content
                        where p.ContentPropertyParentID == ParentID
                        select new ContentBindingList
                        {
                            ContentPropertyAlias = p.ContentPropertyAlias,
                            ContentPropertyID = p.ContentPropertyID,
                            ContentPropertyParentID = p.ContentPropertyParentID,
                            Priority = p.Priority,
                            SiteID = p.SiteID,
                            ContentPropertyType = p.ContentPropertyType,
                            ContentCultures = p.ContentPropertyCultures.Where(m => m.Culture.Name.ToLower().Equals(Lang.ToLower()) && m.Culture.SiteID == p.SiteID).Select(m => new ContentCultureBinding
                            {
                                ContentPropertyID = p.ContentPropertyID,
                                ContentPropertyValue = m.ContentPropertyValue,
                                CultureID = m.CultureID
                            }).ToList(),
                            Items = _c.ContentProperties.Where(m => m.ContentPropertyParentID == p.ContentPropertyID).Any() ? GetOrder(_c.ContentProperties.Where(m => m.ContentPropertyParentID == p.ContentPropertyID).ToList(), p.ContentPropertyID, Lang) : new List<ContentBindingList>()
                        }).OrderBy(m => m.Priority).ToList();
            }
        }

        #region TREE
        public List<ContentTree> GetTree(int SiteID, int CultureID, ApplicationDbContext _c)
        {
            var _List = from p in _c.ContentProperties
                        where p.SiteID == SiteID
                        select p;
            return GetTreeOrder(_List.ToList(), 0, CultureID, _c);
        }
        public List<ContentTree> GetTree(int SiteID, int CultureID, string UserID)
        {
            using (var _c = db)
            {
                Can(SiteID, UserID, _c);
                return GetTree(SiteID, CultureID, _c);
            }
        }
        public List<ContentTree> GetTreeOrder(List<ContentProperty> Content, int ParentID, int CultureID, ApplicationDbContext _c)
        {
            return (from p in Content
                    where p.ContentPropertyParentID == ParentID
                    orderby p.Priority
                    select new ContentTree
                    {
                        ContentPropertyID = p.ContentPropertyID,
                        ContentPropertyParentID = p.ContentPropertyParentID,
                        ContentPropertyType = p.ContentPropertyType,

                        ContentPropertyValue = p.ContentPropertyCultures.Where(m => m.CultureID == CultureID).Any()
                        ? p.ContentPropertyCultures.Where(m => m.CultureID == CultureID).Select(m => m.ContentPropertyValue).FirstOrDefault()
                        : p.ContentPropertyCultures.Select(m => m.ContentPropertyValue).FirstOrDefault(),

                        Edit = p.ContentPropertyCultures.Where(m => m.CultureID == CultureID).Any()
                        ? false
                        : true,

                        ShowInContent = p.ShowInContent,

                        Hint = p.ContentPropertyCultures.Where(m => m.CultureID == CultureID).Any()
                        ? p.ContentPropertyCultures.Where(m => m.CultureID == CultureID).Select(m => m.Hint).FirstOrDefault()
                        : p.ContentPropertyCultures.Select(m => m.Hint).FirstOrDefault(),

                        IsEditable = _c.ContentProperties.Where(m => m.SiteID == p.SiteID && m.ContentPropertyParentID == p.ContentPropertyParentID && m.ShowInContent && m.ContentPropertyID != p.ContentPropertyID).Any(),

                        Items = _c.ContentProperties.Where(m => m.ContentPropertyParentID == p.ContentPropertyID).Any()
                        ? GetTreeOrder(_c.ContentProperties.Where(m => m.ContentPropertyParentID == p.ContentPropertyID).ToList(), p.ContentPropertyID, CultureID, _c)
                        : new List<ContentTree>()
                    }).ToList();
        }
        public void SaveTree(ContentTreeBinding Model, string UserID)
        {
            using (var _c = db)
            {
                Can(Model.SiteID, UserID, _c);
                foreach (var item in Model.Items)
                {
                    if (item.Edit)
                    {
                        var _ContentCulture = _c.ContentPropertyCultures.Where(m => m.ContentPropertyID == item.ContentPropertyID && m.CultureID == Model.CultureID).SingleOrDefault();
                        if (_ContentCulture == null)
                        {
                            _ContentCulture = new ContentPropertyCulture();
                            _ContentCulture.CultureID = Model.CultureID;
                            _ContentCulture.ContentPropertyID = item.ContentPropertyID;
                            _c.ContentPropertyCultures.Add(_ContentCulture);
                        }
                        _ContentCulture.Hint = item.Hint;

                        if (item.ContentPropertyType == ContentPropertyType.IMG)
                        {
                            if (!string.IsNullOrEmpty(item.FileName))
                                _ContentCulture.ContentPropertyValue = SaveFile(item.ContentPropertyID, Model.CultureID, item.FileName, item.FileContent);
                            else if (!string.IsNullOrEmpty(item.ContentPropertyValue))
                            {
                                //CASE SAVE IMG IN OTHER LANGUAGE
                                _ContentCulture.ContentPropertyValue = item.ContentPropertyValue;
                            }
                        }
                        else if (item.ContentPropertyType == ContentPropertyType.TXT || item.ContentPropertyType == ContentPropertyType.HTML)
                            _ContentCulture.ContentPropertyValue = item.ContentPropertyValue;
                        _c.SaveChanges();
                    }
                    if (item.Items.Any())
                    {
                        SaveTree(new ContentTreeBinding
                        {
                            CultureID = Model.CultureID,
                            SiteID = Model.SiteID,
                            Items = item.Items
                        }, UserID);
                    }

                }
            }
        }
        #endregion

        #region IMPORT/EXPORT
        public string Export(int ContentPropertyID, int SiteID, string UserID)
        {
            using (var _c = db)
            {
                Can(SiteID, UserID, _c);
                List<ContentBindingList> _CP = null;
                if (ContentPropertyID == 0)
                {
                    _CP = Get(SiteID, UserID);
                }
                else
                {
                    var _M = _c.ContentProperties.Where(m => m.ContentPropertyID == ContentPropertyID).ToList();
                    _CP = GetOrder(_M, _M[0].ContentPropertyParentID)[0].Items.ToList();
                }
                ExportFillFile(_CP);
                var _URL = SiteID + (ContentPropertyID == 0 ? "" : "/" + ContentPropertyID) + "/PROPERTIES.json";

                var blob = new Utils.FileUtil().GetBlob("export", _URL);
                using (Stream blobStream = blob.OpenWrite())
                {
                    using (StreamWriter writer = new StreamWriter(blobStream))
                    {
                        string _JSON = JsonConvert.SerializeObject(_CP);
                        writer.Write(_JSON);
                    }
                }
                return StorageUrl + "/export/" + _URL;
            }
        }
        public List<ContentBindingList> Export(int SiteID)
        {
            var _Model = Get(SiteID);
            ExportFillFile(_Model);
            return _Model; ;
        }
        public void ExportFillFile(ICollection<ContentBindingList> Model)
        {
            foreach (var item in Model)
            {
                if (item.ContentPropertyType == ContentPropertyType.IMG)
                {
                    foreach (var ic in item.ContentCultures)
                    {
                        using (var _w = MyWebClient)
                        {
                            try
                            {
                                if (ic.ContentPropertyValue != null)
                                    ic.ContentPropertyBinary = "data:image;base64," + Convert.ToBase64String(_w.DownloadData(ic.ContentPropertyValue));
                            }
                            catch (WebException)
                            {
                                ic.ContentPropertyBinary = null;
                            }
                        }
                    }
                }
                ExportFillFile(item.Items);
            }
        }
        public void Import(ImportBinding Model, string UserID)
        {
            using (var _c = db)
            {
                Can(Model.SiteID, UserID, _c);
                byte[] binary = Convert.FromBase64String(Model.FileRead.FileContent.Split(',')[1]);
                var _Items = JsonConvert.DeserializeObject<ICollection<ContentBindingList>>(Encoding.UTF8.GetString(binary));
                Import(_Items, Model.SiteID, Model.ContentPropertyID, UserID, _c);
            }
        }
        private void Import(ICollection<ContentBindingList> List, int SiteID, int ContentPropertyID, string UserID, ApplicationDbContext _c)
        {
            foreach (var item in List)
            {
                item.SiteID = SiteID;
                item.ContentPropertyParentID = ContentPropertyID;
                item.ContentPropertyID = 0;
                var _ContentPropertyID = Save(item, UserID);
                List<ContentCultureBinding> _ListCulture = new List<ContentCultureBinding>();
                List<ContentCultureBindingFile> _ListCultureFile = new List<ContentCultureBindingFile>();
                foreach (var ic in item.ContentCultures)
                {
                    var _C = _c.Cultures.Where(m => m.SiteID == SiteID && m.Name.ToLower().Equals(ic.CultureName.ToLower())).SingleOrDefault();
                    if (_C != null)
                    {
                        var _M = new ContentCultureBinding();
                        _M.ContentPropertyID = _ContentPropertyID;
                        _M.CultureID = _C.CultureID;
                        if (item.ContentPropertyType == ContentPropertyType.TXT || item.ContentPropertyType == ContentPropertyType.HTML)
                            _M.ContentPropertyValue = ic.ContentPropertyValue;
                        else if (item.ContentPropertyType == ContentPropertyType.IMG)
                        {
                            var _MF = new ContentCultureBindingFile();
                            _MF.ContentPropertyID = _M.ContentPropertyID;
                            _MF.FileName = ic.ContentPropertyValue.Split('/').Last();
                            _MF.CultureID = _M.CultureID;
                            _MF.FileContent = ic.ContentPropertyBinary;
                            ic.ContentPropertyValue = null;
                            _ListCultureFile.Add(_MF);
                        }
                        _ListCulture.Add(_M);
                    }
                }
                if (item.ContentPropertyType == ContentPropertyType.IMG)
                    SaveCulture(_ListCultureFile, UserID);
                else
                    SaveCulture(_ListCulture, UserID);
                Import(item.Items, SiteID, _ContentPropertyID, UserID, _c);
            }
        }
        #endregion

        private string SaveFile(int ContentPropertyID, int CultureID, string FileName, string FileContent)
        {
            var ContentPropertyValue = ContentPropertyID + "/" + CultureID + "/" + FileName;
            new Utils.FileUtil().DeleteFolder("content", ContentPropertyID + "/" + CultureID + "/");
            new Utils.FileUtil().UploadImage(Utils.ImageUtil.StringToStream(FileContent), "content", ContentPropertyValue);
            return StorageUrl + "/content/" + ContentPropertyValue;
        }

        public int Save(ContentBinding Model)
        {
            using (var _c = db)
            {
                var _Model = new ContentProperty();
                if (Model.ContentPropertyID == 0)
                {
                    var _IQParent = _c.ContentProperties.Where(m => m.SiteID == Model.SiteID && m.ContentPropertyParentID == Model.ContentPropertyParentID);
                    _Model.ContentPropertyParentID = Model.ContentPropertyParentID;
                    _Model.SiteID = Model.SiteID;
                    _Model.Priority = _IQParent.Any() ? _IQParent.Select(m => m.Priority).Max() + 1 : 0;
                    _Model.Lock = Model.Lock;
                    _c.ContentProperties.Add(_Model);
                }
                else
                {
                    _Model = _c.ContentProperties.Where(m => m.ContentPropertyID == Model.ContentPropertyID).SingleOrDefault();
                }
                _Model.ContentPropertyAlias = Model.ContentPropertyAlias;
                _Model.ContentPropertyType = Model.ContentPropertyType;
                _c.SaveChanges();
                return _Model.ContentPropertyID;
            }
        }
        public int Save(ContentBinding Model, string UserID)
        {
            using (var _c = db)
            {
                Can(Model.SiteID, UserID, _c);
                return Save(Model);
            }
        }
        public void SaveCulture(List<ContentCultureBinding> Model, string UserID)
        {
            using (var _c = db)
            {
                foreach (var item in Model)
                {
                    var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == item.ContentPropertyID).SingleOrDefault();
                    Can(_CP.SiteID, UserID, _c);
                    var _ContentCulture = _c.ContentPropertyCultures.Where(m => m.ContentPropertyID == item.ContentPropertyID && m.CultureID == item.CultureID).SingleOrDefault();
                    if (_ContentCulture == null)
                    {
                        _ContentCulture = new ContentPropertyCulture(item);
                        _c.ContentPropertyCultures.Add(_ContentCulture);
                    }
                    else
                    {
                        _ContentCulture.SetValues(item);
                    }
                    _c.SaveChanges();
                }
            }
        }
        public List<ContentCultureBindingFile> SaveCulture(List<ContentCultureBindingFile> Model, string UserID)
        {
            using (var _c = db)
            {
                foreach (var item in Model)
                {
                    var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == item.ContentPropertyID).SingleOrDefault();
                    Can(_CP.SiteID, UserID, _c);
                    if (!string.IsNullOrEmpty(item.FileContent))
                    {
                        var _ContentCulture = _c.ContentPropertyCultures.Where(m => m.ContentPropertyID == item.ContentPropertyID && m.CultureID == item.CultureID).SingleOrDefault();
                        item.ContentPropertyValue = SaveFile(item.ContentPropertyID, item.CultureID, item.FileName, item.FileContent);
                        if (_ContentCulture == null)
                        {
                            _ContentCulture = new ContentPropertyCulture(item);
                            _c.ContentPropertyCultures.Add(_ContentCulture);
                        }
                        else
                        {
                            _ContentCulture.SetValues(item);
                        }
                        _c.SaveChanges();
                        item.FileContent = "";

                    }
                }
                return Model;
            }
        }
        private void SaveCulture(ContentCultureBinding Model, int SiteID)
        {
            using (var _c = db)
            {
                var _CultureID = _c.Cultures.Where(m => m.Name.Equals(Model.CultureName) && m.SiteID == SiteID).SingleOrDefault().CultureID;
                var _CPC = new ContentPropertyCulture();
                _CPC.ContentPropertyID = Model.ContentPropertyID;
                _CPC.CultureID = _CultureID;
                _CPC.Hint = Model.Hint;
                if (!string.IsNullOrEmpty(Model.ContentPropertyBinary))
                    _CPC.ContentPropertyValue = SaveFile(Model.ContentPropertyID, _CultureID, Model.ContentPropertyValue.Split('/').Last(), Model.ContentPropertyBinary);
                else
                    _CPC.ContentPropertyValue = Model.ContentPropertyValue;
                _c.ContentPropertyCultures.Add(_CPC);
                _c.SaveChanges();
            }
        }
        public int Save(ContentBindingList Model)
        {
            using (var _c = db)
            {
                var _CP = new ContentProperty
                {
                    ContentPropertyParentID = Model.ContentPropertyParentID,
                    SiteID = Model.SiteID,
                    ContentPropertyType = Model.ContentPropertyType,
                    ContentPropertyAlias = Model.ContentPropertyAlias,
                    Priority = Model.Priority,
                    Lock = Model.Lock,
                    ShowInContent = Model.ShowInContent
                };
                _c.ContentProperties.Add(_CP);
                _c.SaveChanges();

                var _NumCultureSite = _c.Cultures.Where(m => m.SiteID == Model.SiteID).Count();
                if (_NumCultureSite < Model.ContentCultures.Count)
                    throw new RuleException(ContentRuleExceptionCode.MORECULTURES.ToString(), "Content Alias:" + Model.ContentPropertyAlias + " with " + Model.ContentCultures.Count + " and Site have " + _NumCultureSite + " Cultures");
                foreach (var item in Model.ContentCultures)
                {
                    item.ContentPropertyID = _CP.ContentPropertyID;
                    SaveCulture(item, Model.SiteID);
                }
                return _CP.ContentPropertyID;
            }
        }
        public void SaveAlias(ContentBinding Model, string UserID)
        {
            using (var _c = db)
            {
                var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == Model.ContentPropertyID).SingleOrDefault();
                Can(_CP.SiteID, UserID, _c);
                var _Content = _c.ContentProperties.Where(m => m.ContentPropertyID == Model.ContentPropertyID).SingleOrDefault();
                _Content.ContentPropertyAlias = Model.ContentPropertyAlias;
                _c.SaveChanges();
            }
        }

        public ContentBindingList Duplicate(ContentBinding Model, string UserID)
        {
            using (var _c = db)
            {
                Can(Model.SiteID, UserID, _c);
                int _ParentID = _c.ContentProperties.Where(m => m.ContentPropertyID == Model.ContentPropertyID).Select(m => m.ContentPropertyParentID).SingleOrDefault();
                int _CPID = Duplicate(Model.ContentPropertyID, _ParentID, false);
                var _Model = new List<ContentProperty>();
                _Model.Add(_c.ContentProperties.Where(m => m.ContentPropertyID == _CPID).SingleOrDefault());
                return GetOrder(_Model, _ParentID)[0];
            }
        }
        private int Duplicate(int ContentPropertyID, int ContentPropertyParentID, Boolean ShowInContent)
        {
            using (var _c = db)
            {
                var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == ContentPropertyID).SingleOrDefault();

                var _IQParent = _c.ContentProperties.Where(m => m.ContentPropertyParentID == ContentPropertyParentID);
                var _CPNew = new ContentProperty
                {
                    ContentPropertyAlias = _CP.ContentPropertyAlias,
                    ContentPropertyParentID = ContentPropertyParentID,
                    SiteID = _CP.SiteID,
                    ContentPropertyType = _CP.ContentPropertyType,
                    Priority = _IQParent.Any() ? _IQParent.Select(m => m.Priority).Max() + 1 : 0,

                };
                if (ShowInContent)
                    _CPNew.ShowInContent = _CP.ShowInContent;
                _c.ContentProperties.Add(_CPNew);
                _c.SaveChanges();
                //COPY CULTURES
                var _CPC = _c.ContentPropertyCultures.Where(m => m.ContentPropertyID == ContentPropertyID).ToList();
                foreach (var item in _CPC)
                {
                    _c.ContentPropertyCultures.Add(new ContentPropertyCulture
                    {
                        ContentPropertyID = _CPNew.ContentPropertyID,
                        CultureID = item.CultureID,
                        ContentPropertyValue = item.ContentPropertyValue,
                        Hint = item.Hint
                    });
                }
                _c.SaveChanges();
                //Make the same with childs
                var _CPChilds = _c.ContentProperties.Where(m => m.ContentPropertyParentID == ContentPropertyID).ToList();
                foreach (var item in _CPChilds)
                {
                    Duplicate(item.ContentPropertyID, _CPNew.ContentPropertyID, true);
                }
                return _CPNew.ContentPropertyID;
            }
        }

        public void Move(ContentMoveBinding Model, string UserID)
        {
            using (var _c = db)
            {
                var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == Model.ContentPropertyID).SingleOrDefault();
                Can(_CP.SiteID, UserID, _c);
                var _Item = (from p in _c.ContentProperties
                             where p.ContentPropertyID == Model.ContentPropertyID
                             select p).SingleOrDefault();
                if (Model.ContentPropertyParentID == _Item.ContentPropertyParentID)
                {
                    _c.Database.ExecuteSqlCommand(@"UPDATE ContentProperties SET Priority=Priority+1 
WHERE SiteID={0} AND ContentPropertyParentID={1} AND Priority>={2} AND Priority < {3} AND  ContentPropertyID<>{4}", _Item.SiteID, Model.ContentPropertyParentID, Model.Index, _Item.Priority, Model.ContentPropertyID);

                    _c.Database.ExecuteSqlCommand(@"UPDATE ContentProperties SET Priority=Priority-1 
WHERE SiteID={0} AND ContentPropertyParentID={1} AND Priority<= {2} AND Priority >{3} AND ContentPropertyID<>{4}", _Item.SiteID, _Item.ContentPropertyParentID, Model.Index, _Item.Priority, Model.ContentPropertyID);
                }
                else
                {
                    //SUBSTRACT OLD PARENT ID
                    _c.Database.ExecuteSqlCommand(@"UPDATE ContentProperties SET Priority=Priority-1 
WHERE SiteID={0} AND ContentPropertyParentID={1} AND Priority>= {2}  AND ContentPropertyID<>{3}", _Item.SiteID, _Item.ContentPropertyParentID, _Item.Priority, Model.ContentPropertyID);
                    //SUM NEW PARENT ID
                    _c.Database.ExecuteSqlCommand(@"UPDATE ContentProperties SET Priority=Priority+1 
WHERE SiteID={0} AND ContentPropertyParentID={1} AND Priority>={2} AND ContentPropertyID<>{3}", _Item.SiteID, Model.ContentPropertyParentID, Model.Index, Model.ContentPropertyID);

                }

                _Item.ContentPropertyParentID = Model.ContentPropertyParentID;
                _Item.Priority = Model.Index;
                _c.SaveChanges();

            }
        }
        public void Delete(int ContentPropertyID, string UserID)
        {
            using (var _c = db)
            {
                var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == ContentPropertyID).SingleOrDefault();
                Can(_CP.SiteID, UserID, _c);
                var _ItemToDelete = (from p in _c.ContentProperties
                                     where p.ContentPropertyID == ContentPropertyID
                                     select p).SingleOrDefault();

                var _ItemChilds = (from p in _c.ContentProperties
                                   where p.ContentPropertyParentID == ContentPropertyID
                                   select p).ToList();

                foreach (var item in _ItemChilds)
                {
                    Delete(item.ContentPropertyID, UserID);
                }
                //UPDATE PRIORITY
                _c.Database.ExecuteSqlCommand(@"UPDATE ContentProperties SET Priority=Priority-1 WHERE ContentPropertyParentID={0} AND Priority>={1}", _ItemToDelete.ContentPropertyParentID, _ItemToDelete.Priority);
                _c.ContentProperties.Remove(_ItemToDelete);
                _c.SaveChanges();
            }
        }



        public void Lock(ContentBoolean Model, string UserID)
        {
            using (var _c = db)
            {
                var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == Model.ContentPropertyID).SingleOrDefault();
                Can(_CP.SiteID, UserID, _c);
                _CP.Lock = Model.Boolean;
                _c.SaveChanges();
            }
        }
        public void ShowInContent(ContentBoolean Model, string UserID)
        {
            using (var _c = db)
            {
                var _CP = _c.ContentProperties.Where(m => m.ContentPropertyID == Model.ContentPropertyID).SingleOrDefault();
                Can(_CP.SiteID, UserID, _c);
                _CP.ShowInContent = Model.Boolean;
                _c.SaveChanges();
            }
        }

        public void LockAll(int SiteID, string UserID)
        {
            using (var _c = db)
            {
                Can(SiteID, UserID, _c);
                var _CP = _c.ContentProperties.Where(m => m.SiteID == SiteID).ToList();
                foreach (var item in _CP)
                {
                    item.Lock = true;
                }
                _c.SaveChanges();
            }
        }
    }
}
