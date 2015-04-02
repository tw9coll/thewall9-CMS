﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using thewall9.bll.Exceptions;
using thewall9.data;
using thewall9.data.binding;
using thewall9.data.Models;
using thewall9.bll.Utils;
namespace thewall9.bll
{
    public class ProductBLL : BaseBLL
    {
        #region Web
        //private ProductCulture GetCulture(int SiteID, string Lang, string FriendlyUrl, ApplicationDbContext _c)
        //{
        //    return _c.ProductCultures.Where(m => m.Product.SiteID == SiteID
        //        && (!string.IsNullOrEmpty(FriendlyUrl)
        //            ? m.FriendlyUrl.ToLower().Equals(FriendlyUrl.ToLower())
        //            : (!string.IsNullOrEmpty(Lang)
        //                ? m.Culture.Name.ToLower().Equals(Lang.ToLower())
        //                : false))).FirstOrDefault();
        //}
        private IQueryable<ProductCulture> Get(int SiteID, int CultureID, int CategoryID, ApplicationDbContext _c)
        {
            return (CategoryID == 0
                ? from m in _c.ProductCultures
                  where m.Product.SiteID == SiteID && m.CultureID == CultureID
                  orderby m.Product.Priority
                  select m
               : from m in _c.ProductCultures
                 join u in _c.ProductCategories on m.ProductID equals u.ProductID
                 where u.CategoryID == CategoryID && m.CultureID == CultureID
                 orderby m.Product.Priority
                 select m);
        }
        private IQueryable<ProductWeb> Get(int SiteID, int CultureID, int CurrencyID, int CategoryID, ApplicationDbContext _c)
        {
            return Get(SiteID, CultureID, CategoryID, _c).Select(m => new ProductWeb
            {
                AdditionalInformation = m.AdditionalInformation,
                Description = m.Description,
                FriendlyUrl = m.FriendlyUrl,
                IconPath = m.IconPath,
                ProductName = m.ProductName,
                ProductID = m.ProductID,

                //TO-DO OPTIMIZE ME
                Price = (CurrencyID == 0 || !m.Product.ProductCurrencies.Where(p => p.CurrencyID == CurrencyID).Any())
                ? (m.Product.ProductCurrencies.Any()
                    ? m.Product.ProductCurrencies.FirstOrDefault().Price
                    : 0)
                : m.Product.ProductCurrencies.Where(p => p.CurrencyID == CurrencyID).FirstOrDefault().Price
            });
        }
        public ProductsWeb Get(int SiteID, string Url, string Lang, string FriendlyUrl, int CurrencyID, int CategoryID, int Take, int Page)
        {
            using (var _c = db)
            {
                if (SiteID == 0)
                    SiteID = new SiteBLL().Get(Url, _c).SiteID;
                var _Culture = new CategoryBLL().GetCulture(SiteID, Lang, FriendlyUrl, _c);
                var _Q = Get(SiteID, _Culture.CultureID, CurrencyID, CategoryID, _c);
                var _PW = new ProductsWeb();
                _PW.Products = _Q.Skip(Take * (Page - 1)).Take(Take).ToList();
                _PW.NumberPages = _Q.Count() / Take;
                _PW.Categories = new CategoryBLL().Get(SiteID, null, CategoryID, Lang, FriendlyUrl);
                _PW.CultureID = _Culture.CultureID;
                _PW.CultureName = _Culture.Name;
                return _PW;
            }
        }
        public ProductWeb Get(int SiteID, string Url, string FriendlyUrl, int CurrencyID)
        {
            using (var _c = db)
            {
                if (SiteID == 0)
                    SiteID = new SiteBLL().Get(Url, _c).SiteID;
                return (from m in _c.ProductCultures
                        where m.Product.SiteID == SiteID && m.FriendlyUrl.ToLower().Equals(FriendlyUrl.ToLower())
                        select new ProductWeb
                         {
                             AdditionalInformation = m.AdditionalInformation,
                             Description = m.Description,
                             FriendlyUrl = m.FriendlyUrl,
                             IconPath = m.IconPath,
                             ProductName = m.ProductName,
                             ProductID = m.ProductID,
                             CultureName = m.Culture.Name,

                             //TO-DO OPTIMIZE ME
                             Price = (CurrencyID == 0 || !m.Product.ProductCurrencies.Where(p => p.CurrencyID == CurrencyID).Any())
                             ? (m.Product.ProductCurrencies.Any()
                                 ? m.Product.ProductCurrencies.FirstOrDefault().Price
                                 : 0)
                             : m.Product.ProductCurrencies.Where(p => p.CurrencyID == CurrencyID).FirstOrDefault().Price
                         }).FirstOrDefault();
            }
        }

        #endregion

        #region Customer
        private Product GetByID(int ProductID, ApplicationDbContext _c)
        {
            return _c.Products.Where(m => m.ProductID == ProductID).SingleOrDefault();
        }
        private ProductBinding Get(Product c, ApplicationDbContext _c)
        {
            return (new ProductBinding
            {
                ProductID = c.ProductID,
                ProductAlias = c.ProductAlias,
                SiteID = c.SiteID,
                ProductCultures = c.ProductCultures.Select(m => new ProductCultureBinding
                {
                    ProductName = m.ProductName,
                    CultureID = m.CultureID,
                    CultureName = m.Culture.Name,
                    Description = m.Description,
                    AdditionalInformation = m.AdditionalInformation,
                    IconPath = m.IconPath,
                    FriendlyUrl = m.FriendlyUrl
                }).ToList(),
                ProductTags = c.ProductTags.Select(m => new ProductTagBinding
                {
                    ProductID = m.ProductID,
                    TagID = m.TagID,
                    TagName = m.Tag.TagName
                }).ToList(),
                ProductCategories = c.ProductCategories.Select(m => new ProductCategoryBinding
                {
                    ProductID = m.ProductID,
                    CategoryID = m.CategoryID,
                    CategoryAlias = m.Category.CategoryAlias
                }).ToList(),
                ProductGalleries = c.ProductGalleries.Select(m => new ProductGalleryBinding
                {
                    ProductID = m.ProductID,
                    ProductGalleryID = m.ProductGalleryID,
                    PhotoPath = m.PhotoPath
                }).ToList(),
                ProductCurrencies = c.ProductCurrencies.Select(m => new ProductCurrencyBinding
                {
                    CurrencyID = m.CurrencyID,
                    CurrencyName = m.Currency.CurrencyName,
                    ProductID = m.ProductID,
                    Price = m.Price
                }).ToList()
            });
        }
        public ProductBinding GetByID(int ProductID, string UserID)
        {
            using (var _c = db)
            {
                var _P = (from c in _c.Products
                          where c.ProductID == ProductID
                          select c).SingleOrDefault();
                Can(_P.SiteID, UserID, _c);
                return Get(_P, _c);
            }
        }
        public List<ProductBinding> Get(int SiteID, string UserID)
        {
            using (var _c = db)
            {
                Can(SiteID, UserID, _c);
                var _P = from c in _c.Products
                         where c.SiteID == SiteID
                         orderby c.Priority
                         select c;
                return _P.ToList().Select(m => Get(m, _c)).ToList();
            }
        }

        private string SaveIcon(int ProductID, int CultureID, FileRead FileReadModel)
        {
            var _Container = "product-icon";
            var _ContainerReference = ProductID + "/" + CultureID + "/" + FileReadModel.FileName;
            new Utils.FileUtil().DeleteFolder(_Container, ProductID + "/" + CultureID + "/");
            new Utils.FileUtil().UploadImage(Utils.ImageUtil.StringToStream(FileReadModel.FileContent), _Container, _ContainerReference);
            return StorageUrl + "/" + _Container + "/" + _ContainerReference;
        }
        public int Save(ProductBinding Model, string UserID)
        {
            using (var _c = db)
            {
                Can(Model.SiteID, UserID, _c);
                //VALIDATE IT HAS A CATEGORY
                //TO-DO VALIDATE IT HAS CATEGORIES BUT FOR DELETE
                if (Model.ProductCategories == null || Model.ProductCategories.Count == 0)
                    throw new RuleException("Categories Empty", "0x000");

                var _Product = new Product();
                if (Model.ProductID == 0)
                {
                    //CREATING
                    _Product.SiteID = Model.SiteID;
                    _Product.ProductCultures = new List<ProductCulture>();
                    _Product.ProductCategories = new List<ProductCategory>();
                    _Product.ProductCurrencies = new List<ProductCurrency>();
                    _Product.ProductTags = new List<ProductTag>();
                    _Product.Priority = _c.Products.Where(m => m.SiteID == Model.SiteID).Any()
                    ? _c.Products.Where(m => m.SiteID == Model.SiteID).Select(m => m.Priority).Max() + 1
                    : 0;
                    _c.Products.Add(_Product);
                }
                else
                {
                    //UPDATING
                    _Product = GetByID(Model.ProductID, _c);
                }
                _Product.ProductAlias = Model.ProductAlias;

                //ADDING CULTURES
                if (Model.ProductCultures != null)
                {
                    foreach (var item in Model.ProductCultures)
                    {
                        //GENERATE FRIENDLYURL
                        if (string.IsNullOrEmpty(item.FriendlyUrl))
                            item.FriendlyUrl = item.ProductName.CleanUrl();


                        if (Model.ProductID != 0)
                        {
                            if (_c.ProductCultures.Where(m => m.Product.SiteID == Model.SiteID
                                && m.FriendlyUrl == item.FriendlyUrl
                                && m.ProductID != Model.ProductID
                                && m.CultureID != item.CultureID).Any())
                                throw new RuleException("FriendlyURL Exist", "0x001");
                            if (!item.Adding)
                            {
                                var _CC = _Product.ProductCultures.Where(m => m.CultureID == item.CultureID).SingleOrDefault();
                                _CC.ProductName = item.ProductName;
                                _CC.Description = item.Description;
                                _CC.AdditionalInformation = item.AdditionalInformation;
                                _CC.IconPath = item.IconPath;
                                _CC.FriendlyUrl = item.FriendlyUrl;
                            }
                        }
                        else
                        {
                            if (_c.ProductCultures.Where(m => m.Product.SiteID == Model.SiteID
                                && m.FriendlyUrl == item.FriendlyUrl).Any())
                                throw new RuleException("FriendlyURL Exist", "0x001");

                        }
                        if (Model.ProductID == 0 || item.Adding)
                        {
                            _Product.ProductCultures.Add(new ProductCulture
                            {
                                ProductName = item.ProductName,
                                CultureID = item.CultureID,
                                Description = item.Description,
                                AdditionalInformation = item.AdditionalInformation,
                                IconPath = item.IconPath,
                                FriendlyUrl = item.FriendlyUrl
                            });
                        }
                    }
                }
                var _G = Model.ProductCultures.GroupBy(m => m.FriendlyUrl);
                if (_G.Count() < Model.ProductCultures.Count)
                    throw new RuleException("FriendlyURL Should be Different", "0x002");
                //CURRENCIES
                if (Model.ProductCurrencies != null)
                {
                    foreach (var item in Model.ProductCurrencies)
                    {
                        if (Model.ProductID != 0)
                        {
                            if (!item.Adding)
                            {
                                var _CC = _Product.ProductCurrencies.Where(m => m.CurrencyID == item.CurrencyID).SingleOrDefault();
                                _CC.Price = item.Price;
                            }
                        }
                        if (Model.ProductID == 0 || item.Adding)
                        {
                            _Product.ProductCurrencies.Add(new ProductCurrency
                            {
                                CurrencyID = item.CurrencyID,
                                Price = item.Price
                            });
                        }
                    }
                }
                //ADDING CATEGORIES
                foreach (var item in Model.ProductCategories)
                {
                    ProductCategory _PC = null;
                    if (Model.ProductID != 0)
                        _PC = _c.ProductCategories.Where(m => m.CategoryID == item.CategoryID && m.ProductID == Model.ProductID).SingleOrDefault();
                    if (item.Adding || Model.ProductID == 0)
                    {
                        if (_PC == null)
                            _Product.ProductCategories.Add(new ProductCategory
                            {
                                CategoryID = item.CategoryID
                            });
                    }
                    else if (item.Deleting)
                    {
                        if (_PC != null)
                            _Product.ProductCategories.Remove(_PC);
                    }
                }
                //ADDING TAGS
                if (Model.ProductTags != null)
                {
                    foreach (var item in Model.ProductTags)
                    {
                        ProductTag _PT = null;
                        if (Model.ProductID != 0)
                            _PT = _c.ProductTags.Where(m => m.TagID == item.TagID && m.ProductID == Model.ProductID).SingleOrDefault();
                        if (item.Adding || Model.ProductID == 0)
                        {
                            if (_PT == null)
                                _Product.ProductTags.Add(new ProductTag
                                {
                                    TagID = item.TagID
                                });
                        }
                        else if (item.Deleting)
                        {
                            if (_PT != null)
                                _Product.ProductTags.Remove(_PT);
                        }
                    }
                }

                _c.SaveChanges();
                //ADDING ICON
                if (Model.ProductCultures != null)
                {
                    foreach (var item in Model.ProductCultures)
                    {
                        if (item.IconFile != null)
                        {
                            var _PC = _c.ProductCultures.Where(m => m.ProductID == _Product.ProductID && m.CultureID == item.CultureID).SingleOrDefault();
                            _PC.IconPath = SaveIcon(_Product.ProductID, item.CultureID, item.IconFile);
                        }
                    }
                    _c.SaveChanges();
                }
                return _Product.ProductID;
            }
        }
        public void Delete(int ProductID, string UserID)
        {
            using (var _c = db)
            {
                var _Category = GetByID(ProductID, _c);
                Can(_Category.SiteID, UserID, _c);
                _c.Products.Remove(_Category);
                _c.SaveChanges();
            }
        }
        public void UpdatePriorities(ProductUpdatePriorities Model, string UserID)
        {
            using (var _c = db)
            {
                var _P = GetByID(Model.ProductID, _c);
                Can(_P.SiteID, UserID, _c);
                if (Model.Index > _P.Priority)
                {
                    var _OP = _c.Products.Where(m => m.SiteID == _P.SiteID && m.Priority > _P.Priority && m.Priority <= Model.Index).ToList();
                    foreach (var item in _OP)
                    {
                        item.Priority--;
                    }
                }
                else if (Model.Index < _P.Priority)
                {
                    var _OP = _c.Products.Where(m => m.SiteID == _P.SiteID && m.Priority < _P.Priority && m.Priority >= Model.Index).ToList();
                    foreach (var item in _OP)
                    {
                        item.Priority++;
                    }
                }
                _P.Priority = Model.Index;
                _c.SaveChanges();
            }
        }

        //CATEGORIES
        public List<ProductCategoryBinding> GetCategories(int SiteID, string Query)
        {
            using (var _c = db)
            {
                return (from cc in _c.CategoryCultures
                        where cc.Category.SiteID == SiteID && (cc.CategoryName.ToLower().Contains(Query.ToLower())
                        || cc.Category.CategoryAlias.ToLower().Contains(Query.ToLower()))
                        select new ProductCategoryBinding
                        {
                            CategoryID = cc.CategoryID,
                            CategoryAlias = cc.Category.CategoryAlias
                        }).Distinct().ToList();
            }
        }
        //TAGS
        public List<ProductTagBinding> GetTags(int SiteID, string Query)
        {
            using (var _c = db)
            {
                return (from cc in _c.Tags
                        where cc.TagName.ToLower().Contains(Query.ToLower())
                        select new ProductTagBinding
                        {
                            TagID = cc.TagID,
                            TagName = cc.TagName
                        }).Distinct().ToList();
            }
        }
        #endregion
    }
}
