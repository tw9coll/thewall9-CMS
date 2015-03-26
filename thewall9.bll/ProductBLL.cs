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
                         select c;
                return _P.ToList().Select(m => Get(m, _c)).ToList();
            }
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
                if (Model.ProductCultures.GroupBy(m => m.FriendlyUrl).Count() < Model.ProductCultures.Count)
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

        //CATEGORIES
        public List<ProductCategoryBinding> GetCategories(int SiteID, string Query)
        {
            using (var _c = db)
            {
                return (from cc in _c.CategoryCultures
                        where cc.CategoryName.ToLower().Contains(Query.ToLower())
                        || cc.Category.CategoryAlias.ToLower().Contains(Query.ToLower())
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
    }
}
