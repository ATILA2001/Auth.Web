using System;
using System.Reflection;
using Auth.Web.Components.Account.Shared;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Auth.Web.Tests
{
    public class RedirectToLoginTests
    {
        private sealed class FakeNavigationManager : NavigationManager
        {
            public string? LastNavigateTo;

            public FakeNavigationManager(string baseUri, string uri)
            {
                Initialize(baseUri, uri);
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastNavigateTo = uri;
                // update internal Uri to the absolute version
                var abs = ToAbsoluteUri(uri);
                Uri = abs.ToString();
            }
        }

        [Fact]
        public void OnInitialized_Navigates_To_Login_With_ReturnUrl()
        {
            // Arrange
            var baseUri = "https://localhost/";
            var currentUri = "https://localhost/some/page";
            var nav = new FakeNavigationManager(baseUri, currentUri);

            var component = new RedirectToLogin();

            // Set the private [Inject] NavigationManager property via reflection
            var prop = typeof(RedirectToLogin).GetProperty("NavigationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(prop);
            prop!.SetValue(component, nav);

            // Capture the current Uri before the component triggers navigation
            var originalUri = nav.Uri;

            // Act - invoke protected OnInitialized method via reflection
            var method = typeof(RedirectToLogin).GetMethod("OnInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(component, null);

            // Assert: the component should navigate to Account/Login with returnUrl set to the original current Uri
            var expected = $"Account/Login?returnUrl={Uri.EscapeDataString(originalUri)}";
            Assert.Equal(expected, nav.LastNavigateTo);
        }
    }
}
