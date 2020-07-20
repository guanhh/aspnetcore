// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class ResponseCookiesTest
    {
        [Fact]
        public void DeleteCookieShouldSetDefaultPath()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);
            var testCookie = "TestCookie";

            cookies.Delete(testCookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testCookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

        [Fact]
        public void DeleteCookieWithCookieOptionsShouldKeepPropertiesOfCookieOptions()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);
            var testCookie = "TestCookie";
            var time = new DateTimeOffset(2000, 1, 1, 1, 1, 1, 1, TimeSpan.Zero);
            var options = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                Path = "/",
                Expires = time,
                Domain = "example.com",
                SameSite = SameSiteMode.Lax
            };

            cookies.Delete(testCookie, options);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testCookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
            Assert.Contains("secure", cookieHeaderValues[0]);
            Assert.Contains("httponly", cookieHeaderValues[0]);
            Assert.Contains("samesite", cookieHeaderValues[0]);
        }

        [Fact]
        public void NoParamsDeleteRemovesCookieCreatedByAdd()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);
            var testCookie = "TestCookie";

            cookies.Append(testCookie, testCookie);
            cookies.Delete(testCookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testCookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

        [Fact]
        public void ProvidesMaxAgeWithCookieOptionsArgumentExpectMaxAgeToBeSet()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);
            var cookieOptions = new CookieOptions();
            var maxAgeTime = TimeSpan.FromHours(1);
            cookieOptions.MaxAge = TimeSpan.FromHours(1);
            var testCookie = "TestCookie";

            cookies.Append(testCookie, testCookie, cookieOptions);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.Contains($"max-age={maxAgeTime.TotalSeconds.ToString()}", cookieHeaderValues[0]);
        }

        [Theory]
        [InlineData("value", "key=value")]
        [InlineData("!value", "key=%21value")]
        [InlineData("val^ue", "key=val%5Eue")]
        [InlineData("QUI+REU/Rw==", "key=QUI%2BREU%2FRw%3D%3D")]
        public void EscapesValuesBeforeSettingCookie(string value, string expected)
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);

            cookies.Append("key", value);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(expected, cookieHeaderValues[0]);
        }

        [Theory]
        [InlineData("key,")]
        [InlineData("ke@y")]
        public void InvalidKeysThrow(string key)
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);

            Assert.Throws<ArgumentException>(() => cookies.Append(key, "1"));
        }

        [Theory]
        [InlineData("key", "value", "key=value")]
        [InlineData("key,", "!value", "key%2C=%21value")]
        [InlineData("ke#y,", "val^ue", "ke%23y%2C=val%5Eue")]
        [InlineData("base64", "QUI+REU/Rw==", "base64=QUI%2BREU%2FRw%3D%3D")]
        public void AppContextSwitchEscapesKeysAndValuesBeforeSettingCookie(string key, string value, string expected)
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);
            cookies._enableCookieNameEncoding = true;

            cookies.Append(key, value);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(expected, cookieHeaderValues[0]);
        }
    }
}
