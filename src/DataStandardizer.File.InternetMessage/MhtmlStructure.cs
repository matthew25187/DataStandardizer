using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// What RFC 2387 and RFC 2557 make of the entities of a message: its root, its parts with their URLs, what each can refer to, and the problems found on the way.
    /// </summary>
    /// <remarks>
    /// The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack.  What is inside an embedded message is not looked at: RFC 2557 §7 doesn't cover
    /// references into another message.
    /// </remarks>
    internal sealed class MhtmlStructure
    {
        private const string AlternativeSubtype = "alternative";
        private const string CidScheme = "cid";
        private const string HtmlSubtype = "html";
        private const string RelatedSubtype = "related";
        private const string StartParameterName = "start";
        private const string TypeParameterName = "type";

        private const string BadStartMessage = "The start parameter of a multipart/related entity names no part (RFC 2387 §3.2).";
        private const string BadTypeMessage = "The type parameter of a multipart/related entity is not the media type of its root part (RFC 2387 §3.1).";
        private const string DuplicateContentIdMessage = "Two parts have the same Content-ID, which has to be unique (RFC 2045 §7, RFC 2557 §7).";
        private const string DuplicateLocationMessage = "Two parts of a multipart/related entity have a Content-Location that resolves to the same URL (RFC 2557 §7).";
        private const string InvalidLocationMessage = "The body of the Content-Location field is not a URI that a header can hold (RFC 2557 §4.1 and §4.4.1).";
        private const string NoPartsMessage = "A multipart/related entity has no parts, so it has no root (RFC 2387 §3.2).";
        private const string MissingTypeMessage = "A multipart/related entity has no type parameter, which RFC 2387 §3.1 requires.";
        private const string TooManyLocationsMessage = "An entity has more than one Content-Location field, which RFC 2557 §4.2 doesn't allow.";

        private MhtmlStructure(ImfEntity root, Scope rootScope, string rootBase)
        {
            Root = root;
            RootScope = rootScope;
            RootBase = rootBase;
        }

        /// <summary>
        /// Gets the root: the part that a <c>multipart/related</c> body starts with, and within a <c>multipart/alternative</c> part, the one to show (RFC 2557 §7).
        /// </summary>
        internal ImfEntity Root { get; }

        /// <summary>
        /// Gets what the root can refer to.
        /// </summary>
        internal Scope RootScope { get; private set; }

        /// <summary>
        /// Gets the base URL of what the root refers to.
        /// </summary>
        internal string RootBase { get; private set; }

        /// <summary>
        /// Gets each part, in the order of the message.
        /// </summary>
        internal List<MhtmlResource> Resources { get; } = new List<MhtmlResource>();

        /// <summary>
        /// Gets the problems found.
        /// </summary>
        internal List<ImfDiagnostic> Diagnostics { get; } = new List<ImfDiagnostic>();

        /// <summary>
        /// Reads the structure of a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="baseUrl">The absolute base URL for when no Content-Location field gives one.</param>
        /// <exception cref="ImfFileException"><see cref="ImfMessage.RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        internal static MhtmlStructure Read(ImfMessage message, string baseUrl)
        {
            var body = message.Body;
            var top = new Scope(null, false);
            var structure = new MhtmlStructure(FindRoot(body), top, baseUrl);

            var pending = new Stack<Node>();
            pending.Push(new Node(body, string.Empty, top, baseUrl));
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                var referenceBase = structure.Visit(node, ReferenceEquals(node.Entity, body));
                if (ReferenceEquals(node.Entity, structure.Root))
                {
                    structure.RootScope = node.Scope;
                    structure.RootBase = referenceBase;
                }

                if (!(node.Entity is ImfMultipart multipart)) continue;

                var scope = node.Scope;
                if (IsRelated(multipart))
                {
                    structure.CheckRelated(multipart, node.Path);
                    scope = new Scope(node.Scope, true);
                }

                for (var index = multipart.Parts.Count - 1; index >= 0; index--)
                {
                    var number = (index + 1).ToString(CultureInfo.InvariantCulture);
                    pending.Push(new Node(multipart.Parts[index], node.Path.Length == 0 ? number : node.Path + "." + number, scope, referenceBase));
                }
            }

            return structure;
        }

        /// <summary>
        /// Checks an entity, and adds it to the parts unless it is a multipart body, whose Content-Location is that of the whole aggregate (RFC 2557 §4.3).
        /// </summary>
        /// <returns>The base URL of what the entity refers to, and of its parts.</returns>
        private string Visit(Node node, bool isBody)
        {
            var entity = node.Entity;
            var fields = entity.Fields.FindAll(ImfFieldNames.ContentLocation);
            for (var index = 1; index < fields.Count; index++)
            {
                AddDiagnostic(ImfDiagnosticKind.CardinalityViolation, TooManyLocationsMessage, node.Path, fields[index]);
            }

            var location = entity.ContentLocation;
            if (fields.Count > 0 && location is null) AddDiagnostic(ImfDiagnosticKind.InvalidFieldValue, InvalidLocationMessage, node.Path, fields[0]);

            // ref. RFC 2557 §8.3: a Content-Location with a cid: URI is disregarded.
            var text = location is null || MhtmlUrl.HasScheme(location.OriginalString, CidScheme) ? null : location.OriginalString;

            // ref. RFC 2557 §5 (b) and (c): only an absolute URI is a base.
            var referenceBase = !(text is null) && MhtmlUrl.IsAbsolute(text) ? text : node.InheritedBase;
            if (isBody && entity is ImfMultipart) return referenceBase;

            var resolved = text is null ? null : MhtmlUrl.Resolve(node.InheritedBase, text);
            var uri = !(resolved is null) && Uri.TryCreate(resolved, UriKind.Absolute, out var created) ? created : null;
            var resource = new MhtmlResource(entity, uri, this, node.Scope, referenceBase);
            Resources.Add(resource);

            // ref. RFC 2557 §7: "Within a multipart/related structure", so outside one the first simply stays.
            if (!(resolved is null) && !node.Scope.TryAdd(MhtmlUrl.WithoutFragment(resolved), resource) && node.Scope.IsRelated)
            {
                AddDiagnostic(ImfDiagnosticKind.InvalidRelatedStructure, DuplicateLocationMessage, node.Path, fields[0]);
            }

            var contentId = entity.ContentId;
            if (!(contentId is null) && !node.Scope.TryAdd(contentId, resource))
            {
                AddDiagnostic(ImfDiagnosticKind.InvalidRelatedStructure, DuplicateContentIdMessage, node.Path, entity.Fields.Find(ImfFieldNames.ContentId));
            }

            return referenceBase;
        }

        private void CheckRelated(ImfMultipart multipart, string path)
        {
            var contentType = multipart.ContentType;
            var field = multipart.Fields.Find(ImfFieldNames.ContentType);
            if (multipart.Parts.Count == 0)
            {
                AddDiagnostic(ImfDiagnosticKind.InvalidRelatedStructure, NoPartsMessage, path, field);
                return;
            }

            if (contentType.TryGetParameter(StartParameterName, out var start) && !NamesPart(multipart, start.Value))
            {
                AddDiagnostic(ImfDiagnosticKind.InvalidRelatedStructure, BadStartMessage, path, field);
            }

            if (!contentType.TryGetParameter(TypeParameterName, out var type))
            {
                AddDiagnostic(ImfDiagnosticKind.InvalidRelatedStructure, MissingTypeMessage, path, field);
                return;
            }

            var root = ImfMessage.GetRelatedRoot(multipart);
            if (!(root is null) && !IsMediaType(root.ContentType, type.Value))
            {
                AddDiagnostic(ImfDiagnosticKind.InvalidRelatedStructure, BadTypeMessage, path, field);
            }
        }

        private void AddDiagnostic(ImfDiagnosticKind kind, string text, string path, [AllowNull] ImfHeaderField field)
        {
            Diagnostics.Add(field is null
                ? new ImfDiagnostic(kind, text, -1, path, ImfFieldNames.ContentType, 0)
                : new ImfDiagnostic(kind, text, field.BytePosition, path, field.Name, field.LineNumber));
        }

        private static bool NamesPart(ImfMultipart multipart, string start)
        {
            if (!ImfMessageId.TryParse(start, out var contentId)) return false;

            foreach (var part in multipart.Parts)
            {
                if (part.ContentId == contentId) return true;
            }

            return false;
        }

        // ref. RFC 2387 §3.4: "type" "=" type "/" subtype, so any parameters of the root part don't come into it.
        private static bool IsMediaType(MimeMediaType contentType, string value)
        {
            return MimeMediaType.TryParse(value, out var mediaType)
                && string.Equals(contentType.Type, mediaType.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(contentType.Subtype, mediaType.Subtype, StringComparison.OrdinalIgnoreCase);
        }

        private static ImfEntity FindRoot(ImfEntity body)
        {
            var current = body;
            while (current is ImfMultipart multipart)
            {
                var next = IsRelated(multipart) ? ImfMessage.GetRelatedRoot(multipart) : IsAlternative(multipart) ? GetBestAlternative(multipart) : null;
                if (next is null) return current;

                current = next;
            }

            return current;
        }

        // ref. RFC 2557 §7: an alternative "which is a suitable atomic object, such as a text/html resource".  Otherwise the last is the best, as RFC 2046 §5.1.4 says.
        [return: MaybeNull]
        private static ImfEntity GetBestAlternative(ImfMultipart multipart)
        {
            var parts = multipart.Parts;
            for (var index = parts.Count - 1; index >= 0; index--)
            {
                var contentType = parts[index].ContentType;
                if (string.Equals(contentType.Type, MimeTopLevelTypes.Text, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(contentType.Subtype, HtmlSubtype, StringComparison.OrdinalIgnoreCase))
                    return parts[index];
            }

            return parts.Count == 0 ? null : parts[parts.Count - 1];
        }

        private static bool IsRelated(ImfMultipart multipart)
        {
            return string.Equals(multipart.ContentType.Subtype, RelatedSubtype, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAlternative(ImfMultipart multipart)
        {
            return string.Equals(multipart.ContentType.Subtype, AlternativeSubtype, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The labels of the parts of one <c>multipart/related</c> entity, or of those outside any.  A reference can reach the same one or a surrounding one, and not one that is nested
        /// or parallel (RFC 2557 §7 and §8.2).  The first of the parts with the same label is the one that is found.
        /// </summary>
        internal sealed class Scope
        {
            private readonly Dictionary<ImfMessageId, MhtmlResource> _contentIds = new Dictionary<ImfMessageId, MhtmlResource>();
            private readonly Dictionary<string, MhtmlResource> _locations = new Dictionary<string, MhtmlResource>(StringComparer.Ordinal);
            [AllowNull]
            private readonly Scope _parent;

            internal Scope([AllowNull] Scope parent, bool isRelated)
            {
                _parent = parent;
                IsRelated = isRelated;
            }

            /// <summary>
            /// Gets a flag indicating that these are the parts of a <c>multipart/related</c> entity, and not those outside any.
            /// </summary>
            internal bool IsRelated { get; }

            internal bool TryAdd(string location, MhtmlResource resource)
            {
                if (_locations.ContainsKey(location)) return false;

                _locations.Add(location, resource);
                return true;
            }

            internal bool TryAdd(ImfMessageId contentId, MhtmlResource resource)
            {
                if (_contentIds.ContainsKey(contentId)) return false;

                _contentIds.Add(contentId, resource);
                return true;
            }

            internal bool TryFind(string location, [MaybeNullWhen(false)] out MhtmlResource resource)
            {
                for (var scope = this; !(scope is null); scope = scope._parent)
                {
                    if (scope._locations.TryGetValue(location, out resource)) return true;
                }

                resource = null;
                return false;
            }

            internal bool TryFind(ImfMessageId contentId, [MaybeNullWhen(false)] out MhtmlResource resource)
            {
                for (var scope = this; !(scope is null); scope = scope._parent)
                {
                    if (scope._contentIds.TryGetValue(contentId, out resource)) return true;
                }

                resource = null;
                return false;
            }
        }

        private readonly struct Node
        {
            internal Node(ImfEntity entity, string path, Scope scope, string inheritedBase)
            {
                Entity = entity;
                Path = path;
                Scope = scope;
                InheritedBase = inheritedBase;
            }

            internal ImfEntity Entity { get; }

            internal string Path { get; }

            internal Scope Scope { get; }

            internal string InheritedBase { get; }
        }
    }
}
