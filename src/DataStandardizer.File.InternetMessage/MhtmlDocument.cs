using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A view of an <see cref="ImfMessage"/> as an aggregate document (RFC 2557): a web page saved with its resources, as in an MHTML file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Root.</b>  The root of a <c>multipart/related</c> body is the part that its <c>start</c> parameter names, or otherwise the first (RFC 2387 §3.2).  If that part is
    /// <c>multipart/alternative</c>, the root is its <c>text/html</c> part, or otherwise its last (RFC 2557 §7).  Any other body is its own root, as a page with no resources needs no
    /// <c>multipart/related</c> entity (RFC 2557 §6).
    /// </para>
    /// <para>
    /// <b>URIs.</b>  A relative Content-Location resolves against the nearest absolute Content-Location of the entities around it, then the base URI given here, and then
    /// <c>thismessage:/</c> (RFC 2557 §5).  URIs resolve as RFC 1808 §4 says, and are compared octet by octet, with no change to their <c>%</c> escapes (RFC 2557 §8.2).  A fragment
    /// is ignored, as it is not part of what a URL identifies (RFC 1808 §2.1).
    /// </para>
    /// <para>
    /// <b>Conformance.</b>  Each <c>multipart/related</c> entity is checked against RFC 2387 §3 and RFC 2557 §7, and each Content-Location field against RFC 2557 §4.  A problem causes an
    /// <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowInvalidRelatedStructure"/>, <see cref="ImfOptions.AllowInvalidValues"/> or
    /// <see cref="ImfOptions.AllowCardinalityViolations"/> is set in the options of the message.  With them set, it is recorded in <see cref="Diagnostics"/> instead.
    /// </para>
    /// <para>
    /// The document describes the message as it was when the document was made.  After a change to the message, make another.
    /// </para>
    /// </remarks>
    public sealed class MhtmlDocument
    {
        private const string CidScheme = "cid";

        private readonly ReadOnlyCollection<ImfDiagnostic> _diagnostics;
        private readonly ReadOnlyCollection<MhtmlResource> _resources;
        private readonly MhtmlStructure _structure;

        /// <summary>
        /// Creates a view of a message, with no base URI.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The message breaks a rule that its options don't accept.</exception>
        public MhtmlDocument(ImfMessage message) : this(message, null)
        {
        }

        /// <summary>
        /// Creates a view of a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="baseUri">The absolute URI for a relative one to resolve against when the message has none of its own, such as the one the file was retrieved with
        /// (RFC 2557 §5 (d)); or <c>null</c> for none.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="baseUri"/> is not absolute.</exception>
        /// <exception cref="ImfFileException">The message breaks a rule that its options don't accept.</exception>
        public MhtmlDocument(ImfMessage message, [AllowNull] Uri baseUri)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            CheckBaseUri(baseUri, nameof(baseUri));

            Message = message;
            _structure = MhtmlStructure.Read(message, baseUri is null ? MhtmlUrl.ThisMessageBase : baseUri.OriginalString);
            foreach (var diagnostic in _structure.Diagnostics)
            {
                if (!ImfConformance.IsAllowed(diagnostic.Kind, message.Options))
                    throw ImfConformance.BuildException(diagnostic, !(message.HeaderEnd is null));
            }

            _resources = new ReadOnlyCollection<MhtmlResource>(_structure.Resources);
            _diagnostics = new ReadOnlyCollection<ImfDiagnostic>(_structure.Diagnostics);
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public ImfMessage Message { get; }

        /// <summary>
        /// Gets the root entity.
        /// </summary>
        public ImfEntity Root => _structure.Root;

        /// <summary>
        /// Gets the media type of the root entity.
        /// </summary>
        public MimeMediaType RootMediaType => _structure.Root.ContentType;

        /// <summary>
        /// Gets every part, including the root, in the order of the message, each with its resolved URI.  A body that isn't multipart is the one part.  A multipart body isn't listed, as
        /// its Content-Location is that of the whole aggregate (RFC 2557 §4.3), and what is inside an embedded message isn't either.  The list is read-only.
        /// </summary>
        public IReadOnlyList<MhtmlResource> Resources => _resources;

        /// <summary>
        /// Gets the problems that the options of the message accept.  The problems that <see cref="ImfMessage.Validate"/> reports are not among them.  The list is read-only.
        /// </summary>
        public IReadOnlyList<ImfDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// Finds the part that the root refers to with a URI (RFC 2557 §8.2).
        /// </summary>
        /// <param name="uri">The URI, as the root gives it.  A <c>cid:</c> URI is matched against Content-ID fields only (RFC 2557 §8.3), and any other against Content-Location fields.  A
        /// relative one resolves against the base URI of the root.</param>
        /// <param name="resource">The part, if there is one.</param>
        /// <returns><c>true</c> if a part was found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        public bool TryResolve(Uri uri, [MaybeNullWhen(false)] out MhtmlResource resource)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            return TryFind(uri, _structure.RootScope, _structure.RootBase, out resource);
        }

        /// <summary>
        /// Finds the part that another part refers to with a URI (RFC 2557 §8.2), such as an image in a style sheet.
        /// </summary>
        /// <remarks>
        /// A reference reaches the parts of the <c>multipart/related</c> entity that holds the part it is in, and of those around that one.  RFC 2557 §7 and its example in §9.6 don't
        /// have it reach those of an entity that is nested or parallel.
        /// </remarks>
        /// <param name="uri">The URI, as the part gives it.  A <c>cid:</c> URI is matched against Content-ID fields only (RFC 2557 §8.3), and any other against Content-Location fields.  A
        /// relative one resolves against the base URI of that part.</param>
        /// <param name="referrer">The part that holds the URI.</param>
        /// <param name="resource">The part, if there is one.</param>
        /// <returns><c>true</c> if a part was found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> or <paramref name="referrer"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="referrer"/> is not from this document.</exception>
        public bool TryResolve(Uri uri, MhtmlResource referrer, [MaybeNullWhen(false)] out MhtmlResource resource)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));
            if (referrer is null)
                throw new ArgumentNullException(nameof(referrer));
            if (!ReferenceEquals(referrer.Structure, _structure))
                throw new ArgumentException("The part is not from this document.", nameof(referrer));

            return TryFind(uri, referrer.Scope, referrer.ReferenceBase, out resource);
        }

        /// <summary>
        /// Checks a base URI.
        /// </summary>
        /// <exception cref="ArgumentException">It is not absolute.</exception>
        internal static void CheckBaseUri([AllowNull] Uri baseUri, string parameterName)
        {
            if (!(baseUri is null) && !(baseUri.IsAbsoluteUri && MhtmlUrl.IsAbsolute(baseUri.OriginalString)))
                throw new ArgumentException("The base URI must be absolute.", parameterName);
        }

        private static bool TryFind(Uri uri, MhtmlStructure.Scope scope, string referenceBase, [MaybeNullWhen(false)] out MhtmlResource resource)
        {
            var text = uri.OriginalString;
            if (MhtmlUrl.HasScheme(text, CidScheme))
            {
                resource = null;
                return ImfMessageId.TryParseCidUri(text, out var contentId) && scope.TryFind(contentId, out resource);
            }

            return scope.TryFind(MhtmlUrl.WithoutFragment(MhtmlUrl.Resolve(referenceBase, text)), out resource);
        }
    }
}
