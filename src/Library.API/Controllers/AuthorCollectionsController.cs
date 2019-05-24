using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;

        public AuthorCollectionsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            // make sure a collection was parsed from the body of the request
            if (authorCollection == null) return BadRequest();
            // map the AuthorForCreationDto to Author entities
            var authorEntities = Mapper.Map<IEnumerable<Author>>(authorCollection);

            // loop through each author entity in the list and add to the context
            foreach (var author in authorEntities) _libraryRepository.AddAuthor(author);

            // make sure the db save was successful
            if (!_libraryRepository.Save()) throw new Exception("Creating an author collected failed on save.");

            // map the author entities to AuthorDtos
            var authorCollectionToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            // create a string of ids separated by commas for the location url of the new list of authors
            var idsAsString = string.Join(",", authorCollectionToReturn.Select(a => a.Id));

            return CreatedAtRoute("GetAuthorCollection", new {ids = idsAsString}, authorCollectionToReturn);
        }

        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))]
            IEnumerable<Guid> ids)
        {
            // make sure a list of ids were parsed from the body
            if (ids == null) return BadRequest();
            // get the list of authors from the list of ids
            var authorEntities = _libraryRepository.GetAuthors(ids);

            // make sure each author in the list was found
            if (ids.Count() != authorEntities.Count()) return NotFound();

            // map each Author entity to AuthorDtos
            var authorsToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            return Ok(authorsToReturn);
        }

        [HttpDelete("({ids})")]
        public IActionResult Delete()
        {
            return StatusCode(405);
        }
    }
}