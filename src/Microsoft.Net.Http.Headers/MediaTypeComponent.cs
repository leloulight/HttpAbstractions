// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    public struct MediaTypeComponent : IEquatable<MediaTypeComponent>
    {
        public static readonly StringSegment Type = new StringSegment("type");
        public static readonly StringSegment Subtype = new StringSegment("subtype");

        public MediaTypeComponent(StringSegment name, StringSegment value)
        {
            Name = name;
            Value = value;
        }

        public StringSegment Name { get; set; }
        public StringSegment Value { get; set; }

        public bool IsAcceptAll()
        {
            return (Name == Type || Name == Subtype) &&
                Value.Equals("*", StringComparison.OrdinalIgnoreCase);
        }

        public bool HasName(string name)
        {
            return HasName(new StringSegment(name));
        }

        public bool HasName(StringSegment name)
        {
            return Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        public bool HasValue(string value)
        {
            return HasValue(new StringSegment(value));
        }

        public bool HasValue(StringSegment value)
        {
            return Value.Equals(value, StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(MediaTypeComponent other)
        {
            return HasName(other.Name) && HasValue(other.Value);
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is MediaTypeComponent && Equals((MediaTypeComponent)obj);
        }

        public override int GetHashCode()
        {
            return 17 * Name.GetHashCode() ^ Value.GetHashCode();
        }
    }
}