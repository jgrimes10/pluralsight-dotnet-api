using System;
using System.Collections.Generic;
using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;

        public BooksController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        [HttpGet]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            // check if the author exists
            if (!_libraryRepository.AuthorExists(authorId)) return NotFound();

            // fetch books for author
            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);

            // map to book DTO
            var booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            return Ok(booksForAuthor);
        }

        [HttpGet("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            // check if author exists
            if (!_libraryRepository.AuthorExists(authorId)) return NotFound();

            // try to fetch the book
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            // if no book found
            if (bookForAuthorFromRepo == null) return NotFound();

            var bookForAuthor = Mapper.Map<BookDto>(bookForAuthorFromRepo);
            return Ok(bookForAuthor);
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            // make sure the book could be serialized from request body
            if (book == null) return BadRequest();

            // make sure the author to which the book is being added exists
            if (!_libraryRepository.AuthorExists(authorId)) return NotFound();

            // map BookForCreationDto to Book entity
            var bookEntity = Mapper.Map<Book>(book);

            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            if (!_libraryRepository.Save())
                throw new Exception($"Creating a book for author {authorId} failed on save.");

            // map book entity to bookDto
            var bookToReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor",
                new {authorId, id = bookToReturn.Id},
                bookToReturn);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            // make sure the author exists
            if (!_libraryRepository.AuthorExists(authorId)) return NotFound();

            // get the book for the author
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            // make sure the book was found
            if (bookForAuthorFromRepo == null) return NotFound();

            // delete the book
            _libraryRepository.DeleteBook(bookForAuthorFromRepo);

            // make sure the changes get saved
            if (!_libraryRepository.Save())
                throw new Exception($"Deleting book {id} for author {authorId} failed on save.");

            return NoContent();
        }
    }
}