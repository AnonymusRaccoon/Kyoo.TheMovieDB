using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kyoo.Controllers;
using Kyoo.Models;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;

namespace Kyoo.TheMovieDB
{
    public class NoResultFound : Exception {}
    
    public class TheMovieDBProvider : IMetadataProvider, IPlugin
    {
        string IMetadataProvider.Name => "TheMovieDB";

        string IPlugin.Name => "TheMovieDB Provider";

        private const string APIKey = "c9f328a01011b28f22483717395fc3fa";

        public async Task<Collection> GetCollectionFromName(string name)
        {
            return await Task.FromResult<Collection>(null);
        }

        public async Task<Show> GetShowFromName(string showName, bool isMovie)
        {
            TMDbClient client = new TMDbClient(APIKey);
            if (isMovie)
            {
                SearchContainer<SearchMovie> search = await client.SearchMovieAsync(showName);
                if (search.Results.Count == 0)
                    throw new NoResultFound();
                return await GetShowByID(search.Results[0].Id.ToString());
            }

            return null;
        }
        
        public async Task<Show> GetShowByID(string id)
        {
	        TMDbClient client = new TMDbClient(APIKey);
	        Movie movie = await client.GetMovieAsync(id, MovieMethods.Credits | MovieMethods.Images | MovieMethods.AlternativeTitles);
	        return new Show(Utility.ToSlug(movie.Title),
		        movie.Title,
		        movie.AlternativeTitles.Titles.Select(x => x.Title),
		        null,
		        movie.Overview,
		        movie.Videos?.Results.Where(x => (x.Type == "Trailer" || x.Type == "Teaser") && x.Site == "Youtube")
			        .Select(x => "https://www.youtube.com/watch?v=" + x.Key).FirstOrDefault(),
		        Status.Finished,
		        startYear: movie.ReleaseDate?.Year,
		        movie.ReleaseDate?.Year,
		        movie.Images.Posters?.OrderByDescending(x => x.VoteAverage).ThenByDescending(x => x.VoteCount)
			        .Select(x => "https://image.tmdb.org/t/p/original/" + x.FilePath).FirstOrDefault(),
		        null,
		        null,
		         movie.Images.Backdrops?.OrderByDescending(x => x.VoteAverage).ThenByDescending(x => x.VoteCount)
			         .Select(x => "https://image.tmdb.org/t/p/original/" + x.FilePath).FirstOrDefault(),
		        $"{((IMetadataProvider)this).Name}={id}");
        }

        public async Task<IEnumerable<PeopleLink>> GetPeople(Show show)
        {
            throw new NotImplementedException();
        }

        public async Task<Season> GetSeason(Show show, long seasonNumber)
        {
            throw new NotImplementedException();
        }

        public async Task<Episode> GetEpisode(Show show, long seasonNumber, long episodeNumber, long absoluteNumber)
        {
            throw new NotImplementedException();
        }
    }
}