// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    public struct MediaTypeSegmentWithQuality
    {
        public StringSegment MediaType { get; set; }
        public double Quality { get; set; }

        public override string ToString()
        {
            // For logging purposes
            return MediaType.ToString();
        }
    }
}
