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
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository,
            IUrlHelper urlHelper,
            IPropertyMappingService propertyMappingService,
            ITypeHelperService typeHelperService)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters,
            [FromHeader(Name = "Accept")] string mediaType)
        {
            // make sure there are valid mappings for parameters passed in
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            // make sure the requested field exists on the dto
            if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            // get all of the authors
            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);

            // map the author entities from the db to the author DTO using AutoMapper
            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            // if we ask for HATEOAS links
            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                // create metadata
                var paginationMetadata = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                // create all HATEOAS links
                var links = CreateLinksForAuthors(authorsResourceParameters,
                    authorsFromRepo.HasNext,
                    authorsFromRepo.HasPrevious);

                var shapedAuthors = authors.ShapeData(authorsResourceParameters.Fields);

                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    var authorLinks = CreateLinksForAuthor(
                        (Guid) authorAsDictionary["Id"],
                        authorsResourceParameters.Fields);

                    authorAsDictionary.Add("links", authorLinks);

                    return authorAsDictionary;
                });

                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links
                };

                return Ok(linkedCollectionResource);
            }
            else
            {
                var previousPageLink = authorsFromRepo.HasPrevious
                    ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage)
                    : null;

                var nextPageLink = authorsFromRepo.HasNext
                    ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage)
                    : null;

                var paginationMetaData = new
                {
                    previousPageLink,
                    nextPageLink,
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };

                Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetaData));

                return Ok(authors.ShapeData(authorsResourceParameters.Fields));
            }
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
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.Current:
                default:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                        });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields)
        {
            // make sure dto contains property asked for in request
            if (!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            // get the author from the db by id
            var authorFromRepo = _libraryRepository.GetAuthor(id);

            // check to make sure an author was found with the given id
            if (authorFromRepo == null)
                // if not, return 404, not found
                return NotFound();

            // author was found, so map it to the author DTO
            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            // create all the HATEOS links for the author
            var links = CreateLinksForAuthor(id, fields);

            // add the links to the response
            var linkedResourceToReturn = author.ShapeData(fields)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            // return the author dto
            return Ok(linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type",
            new[] { "application/vnd.marvin.author.full+json" })]
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

            // create all HATEOS links for new author
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            // add links to response
            var linkedResourceToReturn = authorToReturn.ShapeData(null)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor",
                new {id = linkedResourceToReturn["Id"]},
                linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-Type",
            new []
            {
                "application/vnd.marvin.authorwithdateofdeath.full+json",
                "application/vnd.marvin.authorwithdateofdeath.full+xml"
            })]
        //[RequestHeaderMatchesMediaType("Accept",
        //    new[] { "..." })]
        public IActionResult CreateAuthorWithDateOfDeath([FromBody] AuthorForCreationWithDateOfDeathDto author)
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

            // create all HATEOS links for new author
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            // add links to response
            var linkedResourceToReturn = authorToReturn.ShapeData(null)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor",
                new {id = linkedResourceToReturn["Id"]},
                linkedResourceToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            // stop post requests for an author that already exists
            if (_libraryRepository.AuthorExists(id)) return Conflict();

            return NotFound();
        }

        [HttpDelete("{id}", Name = "DeleteAuthor")]
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

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(
                    new LinkDto(_urlHelper.Link("GetAuthor",
                        new { id }),
                        "self",
                        "GET"));
            }
            else
            {
                links.Add(
                    new LinkDto(_urlHelper.Link("GetAuthor",
                        new { id, fields }),
                        "self",
                        "GET"));
            }

            links.Add(
                new LinkDto(_urlHelper.Link("DeleteAuthor",
                    new { id }),
                    "delete_author",
                    "DELETE"));

            links.Add(
                new LinkDto(_urlHelper.Link("CreateBookForAuthor",
                    new { authorId = id }),
                    "create_book_for_author",
                    "POST"));

            links.Add(
                new LinkDto(_urlHelper.Link("GetBooksForAuthor",
                    new { authorId = id }),
                    "books",
                    "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(
            AuthorsResourceParameters authorsResourceParameters,
            bool hasNext,
            bool hasPrevious)
        {
            var links = new List<LinkDto>();

            // self
            links.Add(
                new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.Current),
                    "self",
                    "GET"));

            if (hasNext)
            {
                links.Add(
                    new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                            ResourceUriType.NextPage),
                        "nextPage",
                        "GET"));
            }

            if (hasPrevious)
            {
                links.Add(
                    new LinkDto(CreateAuthorsResourceUri(authorsResourceParameters,
                        ResourceUriType.PreviousPage),
                        "previousPage",
                        "GET"));
            }

            return links;
        }
    }
}