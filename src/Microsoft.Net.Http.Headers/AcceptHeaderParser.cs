// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    public static class AcceptHeaderParser
    {
        private static readonly GenericHeaderParser<MediaTypeSegmentWithQuality>.GetParsedValueLengthDelegate Parser =
            new GenericHeaderParser<MediaTypeSegmentWithQuality>.GetParsedValueLengthDelegate(GetMediaTypeWithQualityLenght);

        private static readonly StringSegment QualityParameter = new StringSegment("q");

        public static IList<MediaTypeSegmentWithQuality> ParseAcceptHeader(IList<string> acceptHeaders)
        {
            if (acceptHeaders == null)
            {
                throw new ArgumentNullException(nameof(acceptHeaders));
            }

            var parser = new GenericHeaderParser<MediaTypeSegmentWithQuality>(
                supportsMultipleValues: true,
                getParsedValueLength: Parser);

            return parser.ParseValues(acceptHeaders);
        }

        private static int GetMediaTypeWithQualityLenght(
            string input,
            int start,
            out MediaTypeSegmentWithQuality result)
        {
            result = default(MediaTypeSegmentWithQuality);
            var enumerator = new MediaTypeParser(input, start, length: null).GetEnumerator();

            double quality = 1.0;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.HasName(QualityParameter))
                {
                    quality = double.Parse(
                        enumerator.Current.Value.Value, NumberStyles.AllowDecimalPoint,
                        NumberFormatInfo.InvariantInfo);
                }
            }

            result = new MediaTypeSegmentWithQuality
            {
                MediaType = new StringSegment(input, start, enumerator.CurrentOffset - start),
                Quality = quality
            };

            return enumerator.CurrentOffset - start;
        }
    }
}
