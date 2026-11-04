using System;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A part of an <see cref="MhtmlDocument"/>, with the URI that its Content-Location field resolves to.
    /// </summary>
    public sealed class MhtmlResource
    {
        internal MhtmlResource(ImfEntity entity, [AllowNull] Uri uri, MhtmlStructure structure, MhtmlStructure.Scope scope, string referenceBase)
        {
            Entity = entity;
            Uri = uri;
            Structure = structure;
            Scope = scope;
            ReferenceBase = referenceBase;
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        public ImfEntity Entity { get; }

        /// <summary>
        /// Gets the absolute URI that the Content-Location field of the entity resolves to (RFC 2557 §5), which starts with <c>thismessage:/</c> if a relative one has nothing else to
        /// resolve against; or <c>null</c> if the entity has no such field.  A field with a <c>cid:</c> URI is disregarded (RFC 2557 §8.3).
        /// </summary>
        /// <remarks>
        /// It labels the entity, and doesn't say where the entity came from, which a sender can misrepresent (RFC 2557 §11.1).  Its <see cref="Uri.OriginalString"/> is the text that is
        /// compared.  A URL that <see cref="System.Uri"/> can't hold after it is resolved also gives <c>null</c>, and can still be found.
        /// </remarks>
        [AllowNull, MaybeNull]
        public Uri Uri { get; }

        /// <summary>
        /// Gets the document structure that the resource belongs to.
        /// </summary>
        internal MhtmlStructure Structure { get; }

        /// <summary>
        /// Gets the <c>multipart/related</c> entity that the resource is inside, for what it can refer to.
        /// </summary>
        internal MhtmlStructure.Scope Scope { get; }

        /// <summary>
        /// Gets the base URL of what the resource refers to (RFC 2557 §5).
        /// </summary>
        internal string ReferenceBase { get; }
    }
}
