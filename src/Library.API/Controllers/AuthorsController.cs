using System;
using System.Collections.Generic;
using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private IUrlHelper _urlHelper;

        public AuthorsController(ILibraryRepository libraryRepository, IUrlHelper urlHelper)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {
            // get all of the authors
            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);

            // get a link to previous page if there is one
            var previousPageLink = authorsFromRepo.HasPrevious
                ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage)
                : null;

            // get a link to next page if there is one
            var nextPageLink = authorsFromRepo.HasNext
                ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage)
                : null;

            // create metadata
            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink,
                nextPageLink
            };

            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            // map the author entities from the db to the author DTO using AutoMapper
            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return Ok(authors);
        }

        private string CreateAuthorsResourceUri(
            AuthorsResourceParameters authorsResourceParameters,
            ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                        });

                default:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                        });
            }
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

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            // stop post requests for an author that already exists
            if (_libraryRepository.AuthorExists(id)) return Conflict();

            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            // get the author
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            // make sure an author was found
            if (authorFromRepo == null) return NotFound();

            // delete the author
            _libraryRepository.DeleteAuthor(authorFromRepo);
            // make sure the save was successful
            if (!_libraryRepository.Save()) throw new Exception($"Deleting author {id} failed on save.");

            return NoContent();
        }
    }
}