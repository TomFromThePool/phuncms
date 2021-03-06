﻿namespace Phun.Tests
{
    using System.Web.Mvc;
    using System.Web.Routing;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Phun.Routing;

    /// <summary>
    /// Unit tests for Bootstrapper.
    /// </summary>
    [TestClass]
    public class BootstrapperTests
    {
        /// <summary>
        /// Tests the boot strapper does not inject simple CMS MVC route handler.
        /// </summary>
        [TestMethod]
        public void TestBootStrapperDoesNotInjectPhunCmsMvcRouteHandler()
        {
            // Arrange
            RouteTable.Routes.Clear();
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });
            var expected = RouteTable.Routes[0];
            var notExpectedLength = RouteTable.Routes.Count;

            // Act                          
            Bootstrapper.Default.hasInitialized = false;
            Bootstrapper.Default.Initialize(false, false);

            // Assert
            Assert.IsInstanceOfType(((Route)expected).RouteHandler, typeof(MvcRouteHandler));
            Assert.AreNotEqual(notExpectedLength, RouteTable.Routes.Count);
        }

        /// <summary>
        /// Tests the boot strapper does inject simple CMS MVC route handler.
        /// </summary>
        [TestMethod]
        public void TestBootStrapperDoesInjectPhunCmsMvcRouteHandler()
        {
            // Arrange
            RouteTable.Routes.Clear();
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });
            var expected = RouteTable.Routes[0];
            var notExpectedLength = RouteTable.Routes.Count;

            // Act
            Bootstrapper.Default.hasInitialized = false;
            Bootstrapper.Default.Initialize(true, false);

            // Assert
            Assert.IsInstanceOfType(((Route)expected).RouteHandler, typeof(MvcRouteHandler));
            Assert.AreNotEqual(notExpectedLength, RouteTable.Routes.Count);
        }
    }
}