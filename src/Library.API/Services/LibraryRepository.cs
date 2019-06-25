using System;
using System.Collections.Generic;
using System.Linq;
using Library.API.Entities;
using Library.API.Helpers;

namespace Library.API.Services
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly LibraryContext _context;

        public LibraryRepository(LibraryContext context)
        {
            _context = context;
        }

        public void AddAuthor(Author author)
        {
            author.Id = Guid.NewGuid();
            _context.Authors.Add(author);

            // the repository fills the id (instead of using identity columns)
            if (!author.Books.Any()) return;
            foreach (var book in author.Books)
                book.Id = Guid.NewGuid();
        }

        public void AddBookForAuthor(Guid authorId, Book book)
        {
            var author = GetAuthor(authorId);
            if (author == null) return;
            // if there isn't an id filled out (ie: we're not upserting),
            // we should generate one
            if (book.Id == Guid.Empty) book.Id = Guid.NewGuid();
            author.Books.Add(book);
        }

        public bool AuthorExists(Guid authorId)
        {
            return _context.Authors.Any(a => a.Id == authorId);
        }

        public void DeleteAuthor(Author author)
        {
            _context.Authors.Remove(author);
        }

        public void DeleteBook(Book book)
        {
            _context.Books.Remove(book);
        }

        public Author GetAuthor(Guid authorId)
        {
            return _context.Authors.FirstOrDefault(a => a.Id == authorId);
        }

        public PagedList<Author> GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {
            // all of the items
            var collectionBeforePaging = _context.Authors
                .OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName).AsQueryable();

            // take filters into account
            if (!string.IsNullOrEmpty(authorsResourceParameters.Genre))
            {
                // trim & ignore casing
                var genreForWhereClause = authorsResourceParameters.Genre
                    .Trim().ToLowerInvariant();

                // apply filter
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.Genre.ToLowerInvariant() == genreForWhereClause);
            }

            // take search query into account
            if (!string.IsNullOrEmpty(authorsResourceParameters.SearchQuery))
            {
                // trim & ignore casing
                var searchQueryForWhereClause = authorsResourceParameters.SearchQuery
                    .Trim().ToLowerInvariant();

                // apply search query
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.Genre.ToLowerInvariant().Contains(searchQueryForWhereClause)
                                || a.FirstName.ToLowerInvariant().Contains(searchQueryForWhereClause)
                                || a.LastName.ToLowerInvariant().Contains(searchQueryForWhereClause));
            }

            // the items paged
            return PagedList<Author>.Create(collectionBeforePaging,
                authorsResourceParameters.PageNumber,
                authorsResourceParameters.PageSize);
        }

        public IEnumerable<Author> GetAuthors(IEnumerable<Guid> authorIds)
        {
            return _context.Authors.Where(a => authorIds.Contains(a.Id))
                .OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName)
                .ToList();
        }

        public void UpdateAuthor(Author author)
        {
            // no code in this implementation
        }

        public Book GetBookForAuthor(Guid authorId, Guid bookId)
        {
            return _context.Books.FirstOrDefault(b => b.AuthorId == authorId && b.Id == bookId);
        }

        public IEnumerable<Book> GetBooksForAuthor(Guid authorId)
        {
            return _context.Books
                .Where(b => b.AuthorId == authorId).OrderBy(b => b.Title).ToList();
        }

        public void UpdateBookForAuthor(Book book)
        {
            // no code in this implementation
        }

        public bool Save()
        {
            return _context.SaveChanges() >= 0;
        }
    }
}