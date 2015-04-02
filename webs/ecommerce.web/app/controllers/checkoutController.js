﻿'use strict';
app.controller('checkoutController', ['$scope', '$rootScope', 'productService', 'toastrService',
    function ($scope, $rootScope, productService, toastrService) {
        
        $scope.removeCart = function (item) {
            productService.removeCart(item);
        };
        $scope.updateNumber = function (item, number) {
            productService.updateNumber(item, number);
        }
        $scope.sendOrder = function (orderSentUrl) {
            productService.sendOrder().then(function () {
                window.location = "/"+orderSentUrl;
                toastrService.success("Sent");
            });
        }
    }
]);