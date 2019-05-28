using System;
using System.Collections.Generic;
using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
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

            // ensure book's title is different than its description
            if (book.Description == book.Title)
                ModelState.AddModelError(nameof(BookForCreationDto),
                    "The provided description should be different from the title.");

            // make sure the input is valid
            // if not return 422 - unprocessable 
            if (!ModelState.IsValid) return new UnprocessableEntityObjectResult(ModelState);

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

        [HttpPut("{id}")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            // make sure book could be deserialized from request body
            if (book == null) return BadRequest();

            // ensure book's title is different than its description
            if (book.Description == book.Title)
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");

            // make sure the input is valid
            // if not return 422 - unprocessable 
            if (!ModelState.IsValid) return new UnprocessableEntityObjectResult(ModelState);

            // make sure the author exists
            if (!_libraryRepository.AuthorExists(authorId)) return NotFound();

            // get the book for the author
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            // make sure the book was found
            if (bookForAuthorFromRepo == null)
            {
                // if it wasn't found... create it
                // map BookDto to entity
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!_libraryRepository.Save())
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");

                // map the entity to BookDto
                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor",
                    new {authorId, id = bookToReturn.Id},
                    bookToReturn);
            }

            // map from dto to entity, update, and map back to dto
            Mapper.Map(book, bookForAuthorFromRepo);
            // update the book in the context
            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            // make sure changes saved to db are successful
            if (!_libraryRepository.Save()) throw new Exception($"Updating book {id} for author {id} failed on save.");

            return NoContent();
        }

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDocument)
        {
            // make sure information could be parsed from request body
            if (patchDocument == null) return BadRequest();

            // make sure the author exists
            if (!_libraryRepository.AuthorExists(authorId)) return NotFound();

            // get the book for the author
            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            // make sure the book was found
            if (bookForAuthorFromRepo == null)
            {
                // book with id not found, upsert instead
                var bookDto = new BookForUpdateDto();
                // apply patch document to new book
                patchDocument.ApplyTo(bookDto, ModelState);

                // add validation
                if (bookDto.Description == bookDto.Title)
                    ModelState.AddModelError(nameof(BookForUpdateDto),
                        "The provided description should be different from the title.");

                TryValidateModel(bookDto);

                if (!ModelState.IsValid) return new UnprocessableEntityObjectResult(ModelState);

                // map from bookDto to entity
                var bookToAdd = Mapper.Map<Book>(bookDto);
                // set the books id to whats found in the url route
                bookToAdd.Id = id;

                // save book to context
                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                // save changes to db
                if (!_libraryRepository.Save())
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
                // map entity to bookDto
                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", new {authorId, id = bookToReturn.Id},
                    bookToReturn);
            }

            // map entity to BookForUpdateDto
            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            // add validation
            patchDocument.ApplyTo(bookToPatch, ModelState);

            if (bookToPatch.Description == bookToPatch.Title)
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");

            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid) return new UnprocessableEntityObjectResult(ModelState);

            // apply the patch document
            patchDocument.ApplyTo(bookToPatch);

            // add validation

            // map from dto to entity, update, and map back to dto
            Mapper.Map(bookToPatch, bookForAuthorFromRepo);
            // update the book in the context
            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            // make sure changes saved to db are successful
            if (!_libraryRepository.Save())
                throw new Exception($"Patching book {id} for author {authorId} failed on save.");

            return NoContent();
        }
    }
}