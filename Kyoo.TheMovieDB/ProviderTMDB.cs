using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kyoo.Controllers;
using Kyoo.Models;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using Credits = TMDbLib.Objects.Movies.Credits;
using TvCredits = TMDbLib.Objects.TvShows.Credits;
using Genre = Kyoo.Models.Genre;

namespace Kyoo.TheMovieDB
{
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
					return null;
				return await GetShowByID(new Show { IsMovie = true, ExternalIDs = $"{((IMetadataProvider)this).Name}={search.Results[0].Id}" });
			}
			else
			{
				SearchContainer<SearchTv> search = await client.SearchTvShowAsync(showName);
				if (search.Results.Count == 0)
					return null;
				return await GetShowByID(new Show { ExternalIDs = $"{((IMetadataProvider)this).Name}={search.Results[0].Id}" });
			}
		}
		
		public async Task<Show> GetShowByID(Show show)
		{
			string id = show?.GetID(((IMetadataProvider) this).Name);
			if (id == null)
				return await Task.FromResult<Show>(null);
			TMDbClient client = new TMDbClient(APIKey);
			if (show.IsMovie)
			{
				Movie movie = await client.GetMovieAsync(id, MovieMethods.AlternativeTitles | MovieMethods.Videos);
				if (movie == null)
					return null;
				Show ret = new Show(Utility.ToSlug(movie.Title),
					movie.Title,
					movie.AlternativeTitles.Titles.Select(x => x.Title),
					null,
					movie.Overview,
					movie.Videos?.Results.Where(x => (x.Type == "Trailer" || x.Type == "Teaser") && x.Site == "YouTube")
						.Select(x => "https://www.youtube.com/watch?v=" + x.Key).FirstOrDefault(),
					Status.Finished,
					movie.ReleaseDate?.Year,
					movie.ReleaseDate?.Year,
					movie.PosterPath != null ? "https://image.tmdb.org/t/p/original" + movie.PosterPath : null,
					null,
					null,
					movie.BackdropPath != null ? "https://image.tmdb.org/t/p/original" + movie.BackdropPath : null,
					$"{((IMetadataProvider) this).Name}={id}");
				ret.Genres = movie.Genres.Select(x => new Genre(x.Name));
				ret.Studio = new Studio(movie.ProductionCompanies.FirstOrDefault()?.Name);
				return ret;
			}
			else
			{
				TvShow tv = await client.GetTvShowAsync(int.Parse(id), TvShowMethods.AlternativeTitles | TvShowMethods.Videos);
				if (tv == null)
					return null;
				Show ret = new Show(Utility.ToSlug(tv.Name),
					tv.Name,
					tv.AlternativeTitles.Results.Select(x => x.Title),
					null,
					tv.Overview,
					tv.Videos?.Results.Where(x => (x.Type == "Trailer" || x.Type == "Teaser") && x.Site == "YouTube")
						.Select(x => "https://www.youtube.com/watch?v=" + x.Key).FirstOrDefault(),
					tv.Status == "Ended" ? Status.Finished : Status.Airing,
					tv.FirstAirDate?.Year,
					tv.LastAirDate?.Year,
					tv.PosterPath != null ? "https://image.tmdb.org/t/p/original" + tv.PosterPath : null,
					null,
					null,
					tv.BackdropPath != null ? "https://image.tmdb.org/t/p/original" + tv.BackdropPath : null,
					$"{((IMetadataProvider) this).Name}={id}");
				ret.Genres = tv.Genres.Select(x => new Genre(x.Name));
				ret.Studio = new Studio(tv.ProductionCompanies.FirstOrDefault()?.Name);
				return ret;
			}
		}

		public async Task<IEnumerable<PeopleLink>> GetPeople(Show show)
		{
			string id = show?.GetID(((IMetadataProvider)this).Name);
			if (id == null)
				return await Task.FromResult(new List<PeopleLink>());
			TMDbClient client = new TMDbClient(APIKey);
			if (show.IsMovie)
			{
				Credits credits = await client.GetMovieCreditsAsync(int.Parse(id));
				return credits.Cast.Select(x =>
						new PeopleLink(Utility.ToSlug(x.Name), x.Name, x.Character, "Actor", x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null, $"{((IMetadataProvider) this).Name}={x.Id}"))
					.Concat(credits.Crew.Select(x =>
						new PeopleLink(Utility.ToSlug(x.Name), x.Name, x.Job, x.Department, x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null, $"{((IMetadataProvider) this).Name}={x.Id}")));
			}
			else
			{
				TvCredits credits = await client.GetTvShowCreditsAsync(int.Parse(id));
				return credits.Cast.Select(x =>
						new PeopleLink(Utility.ToSlug(x.Name), x.Name, x.Character, "Actor", x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null, $"{((IMetadataProvider) this).Name}={x.Id}"))
					.Concat(credits.Crew.Select(x =>
						new PeopleLink(Utility.ToSlug(x.Name), x.Name, x.Job, x.Department, x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null, $"{((IMetadataProvider) this).Name}={x.Id}")));
			}
		}

		public async Task<Season> GetSeason(Show show, long seasonNumber)
		{
			string id = show?.GetID(((IMetadataProvider) this).Name);
			if (id == null)
				return await Task.FromResult<Season>(null);
			TMDbClient client = new TMDbClient(APIKey);
			TvSeason season = await client.GetTvSeasonAsync(int.Parse(id), (int)seasonNumber);
			if (season == null)
				return null;
			return new Season(show.ID,
				seasonNumber,
				season.Name,
				season.Overview,
				season.AirDate?.Year,
				season.PosterPath != null ? "https://image.tmdb.org/t/p/original" + season.PosterPath : null,
				$"{((IMetadataProvider)this).Name}={season.Id}");
		}

		public async Task<Episode> GetEpisode(Show show, long seasonNumber, long episodeNumber, long absoluteNumber)
		{
			string id = show?.GetID(((IMetadataProvider) this).Name);
			if (id == null)
				return await Task.FromResult<Episode>(null);
			TMDbClient client = new TMDbClient(APIKey);
			TvEpisode episode = await client.GetTvEpisodeAsync(int.Parse(id), (int)seasonNumber, (int)episodeNumber);
			if (episode == null)
				return null;
			return new Episode(seasonNumber, episodeNumber, absoluteNumber,
				episode.Name,
				episode.Overview,
				episode.AirDate,
				0,
				episode.StillPath != null ? "https://image.tmdb.org/t/p/original" + episode.StillPath : null,
				$"{((IMetadataProvider)this).Name}={episode.Id}");
		}
	}
}