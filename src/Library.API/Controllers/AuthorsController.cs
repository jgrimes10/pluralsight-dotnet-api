using System;
using System.Collections.Generic;
using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;

        public AuthorsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        [HttpGet]
        public IActionResult GetAuthors()
        {
            // get all of the authors
            var authorsFromRepo = _libraryRepository.GetAuthors();

            // map the author entities from the db to the author DTO using AutoMapper
            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return Ok(authors);
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            // get the author from the db by id
            var authorFromRepo = _libraryRepository.GetAuthor(id);

            // check to make sure an author was found with the given id
            if (authorFromRepo == null)
                // if not, return 404, not found
                return NotFound();

            // author was found, so map it to the author DTO
            var author = Mapper.Map<AuthorDto>(authorFromRepo);
            // return the author dto
            return Ok(author);
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            // make sure request body was correctly serialized to AuthorForCreationDto
            if (author == null) return BadRequest();

            // map the AuthorForCreationDto to an author entity
            var authorEntity = Mapper.Map<Author>(author);

            // add the entity to the context
            _libraryRepository.AddAuthor(authorEntity);
            // save the context to the database
            if (!_libraryRepository.Save()) throw new Exception("Creating an author failed on save.");

            // map the result
            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            return CreatedAtRoute("GetAuthor", new {id = authorToReturn.Id}, authorToReturn);
        }
    }
}