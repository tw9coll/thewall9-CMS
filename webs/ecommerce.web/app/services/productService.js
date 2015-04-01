﻿'use strict';
app.factory('productService', ["$http", "$q", 'localStorageService',
    function ($http, $q, localStorageService) {
        var uri = 'api/product';
        var productFactory = {};

        productFactory.cart = [];

        productFactory.initCart = function () {
            var _Cart = localStorageService.get('_cart');
            productFactory.cart = _Cart == null ? [] : _Cart;
        }

        productFactory.add = function (model) {
            var added = false;
            angular.forEach(productFactory.cart, function (item) {
                if (item.ProductID == model.ProductID) {
                    item.Number++;
                    added = true;
                }
            });
            if (!added) {
                productFactory.cart.push({
                    ProductID: model.ProductID,
                    ProductName: model.ProductName,
                    Number: 1,
                    Price: model.Price
                });
            }
            localStorageService.set('_cart', productFactory.cart);
        };
        productFactory.totalCart = function () {
            var _Total = 0;
            angular.forEach(productFactory.cart, function (item) {
                _Total += (item.Number * item.Price);
            });
            return _Total;
        }


        return productFactory;
    }
]);