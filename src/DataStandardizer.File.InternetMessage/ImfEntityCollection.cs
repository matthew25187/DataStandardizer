using System;
using System.Collections;
using System.Collections.Generic;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The parts of an <see cref="ImfMultipart"/>, in order.
    /// </summary>
    /// <remarks>
    /// An entity can be in one place at a time, so a tree can't hold itself.
    /// </remarks>
    public sealed class ImfEntityCollection : IReadOnlyList<ImfEntity>
    {
        private readonly List<ImfEntity> _entities = new List<ImfEntity>();
        private readonly ImfMultipart _owner;

        internal ImfEntityCollection(ImfMultipart owner)
        {
            _owner = owner;
        }

        /// <inheritdoc />
        public int Count => _entities.Count;

        /// <inheritdoc />
        public ImfEntity this[int index] => _entities[index];

        /// <summary>
        /// Adds a part after the others.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entity"/> is, or holds, the multipart entity it is being added to.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="entity"/> is already the body of a message or a part of a multipart entity.</exception>
        public void Add(ImfEntity entity)
        {
            CheckCanAttach(entity);
            entity.Attach();
            _entities.Add(entity);
        }

        /// <summary>
        /// Adds a part at an index.
        /// </summary>
        /// <param name="index">The index the part gets.</param>
        /// <param name="entity">The entity.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entity"/> is, or holds, the multipart entity it is being added to.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0, or more than <see cref="Count"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="entity"/> is already the body of a message or a part of a multipart entity.</exception>
        public void Insert(int index, ImfEntity entity)
        {
            CheckCanAttach(entity);
            _entities.Insert(index, entity);
            entity.Attach();
        }

        /// <summary>
        /// Replaces the part at an index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="entity">The entity.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entity"/> is, or holds, the multipart entity it is being added to.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0, or not less than <see cref="Count"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="entity"/> is already the body of a message or a part of a multipart entity.</exception>
        public void Replace(int index, ImfEntity entity)
        {
            CheckCanAttach(entity);
            _entities[index].Detach();
            _entities[index] = entity;
            entity.Attach();
        }

        /// <summary>
        /// Removes the part at an index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0, or not less than <see cref="Count"/>.</exception>
        public void RemoveAt(int index)
        {
            _entities[index].Detach();
            _entities.RemoveAt(index);
        }

        /// <summary>
        /// Adds a part that a reader read, which can't hold anything that it is in.
        /// </summary>
        internal void AddRead(ImfEntity entity)
        {
            entity.Attach();
            _entities.Add(entity);
        }

        /// <inheritdoc />
        public IEnumerator<ImfEntity> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CheckCanAttach(ImfEntity entity)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity));
            if (entity.IsAttached)
                throw new InvalidOperationException("The entity is already the body of a message or a part of a multipart entity.");
            if (entity.Holds(_owner))
                throw new ArgumentException("An entity can't be a part of a multipart entity that it holds.", nameof(entity));
        }
    }
}
