// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultHttpResponse : HttpResponse
    {
        private HttpContext _context;
        private FeatureReferences<FeatureInterfaces> _features;

        public DefaultHttpResponse(HttpContext context, IFeatureCollection features)
        {
            Initialize(context, features);
        }

        public virtual void Initialize(HttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = new FeatureReferences<FeatureInterfaces>(features);
        }

        public virtual void Uninitialize()
        {
            _context = null;
            _features = default(FeatureReferences<FeatureInterfaces>);
        }

        private IHttpResponseFeature HttpResponseFeature =>
            _features.Fetch(ref _features.Cache.Response, f => null);

        private IResponseCookiesFeature ResponseCookiesFeature =>
            _features.Fetch(ref _features.Cache.Cookies, f => new ResponseCookiesFeature(f));
        

        public override HttpContext HttpContext { get { return _context; } }

        public override int StatusCode
        {
            get { return HttpResponseFeature.StatusCode; }
            set { HttpResponseFeature.StatusCode = value; }
        }

        public override IHeaderDictionary Headers
        {
            get { return HttpResponseFeature.Headers; }
        }

        public override Stream Body
        {
            get { return HttpResponseFeature.Body; }
            set { HttpResponseFeature.Body = value; }
        }

        public override long? ContentLength
        {
            get
            {
                return ParsingHelpers.GetContentLength(Headers);
            }
            set
            {
                ParsingHelpers.SetContentLength(Headers, value);
            }
        }

        public override string ContentType
        {
            get
            {
                return Headers[HeaderNames.ContentType];
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    HttpResponseFeature.Headers.Remove(HeaderNames.ContentType);
                }
                else
                {
                    HttpResponseFeature.Headers[HeaderNames.ContentType] = value;
                }
            }
        }

        public override IResponseCookies Cookies
        {
            get { return ResponseCookiesFeature.Cookies; }
        }

        public override bool HasStarted
        {
            get { return HttpResponseFeature.HasStarted; }
        }

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            HttpResponseFeature.OnStarting(callback, state);
        }

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            HttpResponseFeature.OnCompleted(callback, state);
        }

        public override void Redirect(string location, bool permanent)
        {
            if (permanent)
            {
                HttpResponseFeature.StatusCode = 301;
            }
            else
            {
                HttpResponseFeature.StatusCode = 302;
            }

            Headers[HeaderNames.Location] = location;
        }

        struct FeatureInterfaces
        {
            public IHttpResponseFeature Response;
            public IResponseCookiesFeature Cookies;
        }
    }
}