// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    public struct MediaTypeParser : IEnumerable<MediaTypeComponent>
    {
        private readonly string _mediaType;
        private readonly int _offset;
        private readonly int? _length;

        public MediaTypeParser(StringSegment mediaType)
            : this(mediaType.Buffer, mediaType.Offset, mediaType.Length)
        {
        }

        public MediaTypeParser(string mediaType, int offset, int? length)
        {
            if (mediaType == null)
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            if (offset < 0 || offset >= mediaType.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length != null && offset + length > mediaType.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _mediaType = mediaType;
            _offset = offset;
            _length = length;
        }

        public MediaTypeComponent? GetParameter(string parameterName)
        {
            return GetParameter(new StringSegment(parameterName));
        }

        public MediaTypeComponent? GetParameter(StringSegment parameterName)
        {
            var componentsEnumerator = GetEnumerator();

            if (!(componentsEnumerator.MoveNext() || componentsEnumerator.MoveNext()))
            {
                // Failed to parse media type.
                return null;
            }

            while (componentsEnumerator.MoveNext())
            {
                if (componentsEnumerator.Current.HasName(parameterName))
                {
                    return componentsEnumerator.Current;
                }
            }

            return null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<MediaTypeComponent> IEnumerable<MediaTypeComponent>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_mediaType, _offset, _length);
        }

        public struct Enumerator : IEnumerator<MediaTypeComponent>
        {
            private ParsingStatus _parsingStatus;
            private string _mediaType;
            private int _initialOffset;
            private int _currentOffset;
            private int? _length;
            private MediaTypeComponent _current;
            private StringSegment _subtype;

            public Enumerator(string mediaType, int offset, int? length)
            {
                _parsingStatus = ParsingStatus.NotStarted;
                _mediaType = mediaType;
                _initialOffset = offset;
                _length = length;
                _currentOffset = _initialOffset;
                _current = default(MediaTypeComponent);
                _subtype = default(StringSegment);
            }

            public MediaTypeComponent Current
            {
                get
                {
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_parsingStatus == ParsingStatus.NotStarted)
                    {
                        throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                    }
                    return Current;
                }
            }

            public int CurrentOffset => _currentOffset;

            public bool ParsingFailed => _parsingStatus == ParsingStatus.Failed;

            public bool MoveNext()
            {
                switch (_parsingStatus)
                {
                    case ParsingStatus.NotStarted:
                        MediaType mediaTypeSegment;
                        int mediaTypeSegmentLength = GetMediaTypeLenght(
                            _mediaType, 
                            _currentOffset,
                            out mediaTypeSegment);

                        if (FailedToParse(mediaTypeSegmentLength, _length != null ? _initialOffset + _length : null))
                        {
                            _parsingStatus = ParsingStatus.Failed;
                            return false;
                        }
                        else
                        {
                            _subtype = mediaTypeSegment.subtype;
                            _current = new MediaTypeComponent(MediaTypeComponent.Type,mediaTypeSegment.type);
                            _currentOffset += mediaTypeSegmentLength;
                            _parsingStatus = ParsingStatus.TypeParsed;
                            return true;
                        }
                    case ParsingStatus.TypeParsed:
                        _current = new MediaTypeComponent(MediaTypeComponent.Subtype, _subtype);
                        _parsingStatus = ParsingStatus.SubtypeParsed;
                        return true;
                    case ParsingStatus.SubtypeParsed:
                        if (_currentOffset < _mediaType.Length && _mediaType[_currentOffset] == ';')
                        {
                            var currentOffset = _currentOffset;
                            currentOffset++; // Skip delimiter
                            var whitespaceCount = HttpRuleParser.GetWhitespaceLength(_mediaType, currentOffset);
                            currentOffset = currentOffset + whitespaceCount;

                            MediaTypeComponent parameter;
                            int parameterLength = GetParameterLength(_mediaType, currentOffset, out parameter);
                            if (parameterLength == 0)
                            {
                                _parsingStatus = ParsingStatus.Failed;
                                return false;
                            }
                            else
                            {
                                _current = parameter;
                                _currentOffset = currentOffset + parameterLength;
                                if (_currentOffset - _initialOffset == _length)
                                {
                                    _parsingStatus = ParsingStatus.Finished;
                                }
                                return true;
                            }

                        }
                        else
                        {
                            if (_length != null && _currentOffset != _length)
                            {
                                _parsingStatus = ParsingStatus.Failed;
                            }
                            else
                            {
                                _parsingStatus = ParsingStatus.Finished;
                            }
                            return false;
                        }
                    case ParsingStatus.Finished:
                    case ParsingStatus.Failed:
                    default:
                        return false;
                }
            }

            public void Reset()
            {
                _currentOffset = _initialOffset;
                _parsingStatus = ParsingStatus.NotStarted;
                _current = default(MediaTypeComponent);
            }

            public void Dispose()
            {
            }

            private static bool FailedToParse(int mediaTypeLength, int? parsingBoundary)
            {
                return mediaTypeLength == 0 ||
                    (parsingBoundary != null &&  mediaTypeLength > parsingBoundary);
            }

            private static int GetMediaTypeLenght(
                string input,
                int offset,
                out MediaType result)
            {
                return MediaTypeHeaderValue.ParseMediaTypeExpresion(input, offset, out result,
                    (buffer, start, length) => new StringSegment(buffer, start, length),
                    (type, subtype) =>
                    {
                        if (subtype.HasValue)
                        {
                            return new MediaType { type = type, subtype = subtype };
                        }
                        else
                        {
                            var slash = type.IndexOf('/');
                            return new MediaType
                            {
                                type = type.Subsegment(0, slash),
                                subtype = type.Subsegment(slash + 1, type.Length - (slash + 1))
                            };
                        }
                    });
            }

            private static int GetParameterLength(
                string mediaType,
                int currentPosition,
                out MediaTypeComponent parameter)
            {
                return NameValueHeaderValue.ParseNameValueHeader(
                    mediaType,
                    currentPosition,
                    out parameter,
                    (input, start, length) => new StringSegment(input, start, length),
                    (name, value) => new MediaTypeComponent(name, value));
            }

            private struct MediaType
            {
                public StringSegment type;
                public StringSegment subtype;
            }

            private enum ParsingStatus
            {
                Failed,
                NotStarted,
                TypeParsed,
                SubtypeParsed,
                Finished
            }
        }
    }
}
