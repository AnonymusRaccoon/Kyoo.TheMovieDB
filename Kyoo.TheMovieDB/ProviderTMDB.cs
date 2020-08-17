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
		string IPlugin.Name => "TheMovieDB Provider";
		public IEnumerable<ITask> Tasks => null;

		private readonly ProviderID _provider = new ProviderID
		{
			Slug = "the-moviedb",
			Name = "TheMovieDB",
			Logo = "https://www.themoviedb.org/assets/2/v4/logos/v2/blue_short-8e7b30f73a4020692ccca9c88bafe5dcb6f8a62a4c6bc55cd9ba82bb2cd95f6c.svg"
		};
		public ProviderID Provider => _provider;

		private const string APIKey = "c9f328a01011b28f22483717395fc3fa";


		public async Task<Collection> GetCollectionFromName(string name)
		{
			return await Task.FromResult<Collection>(null);
		}

		public async Task<IEnumerable<Show>> SearchShows(string showName, bool isMovie)
		{
			TMDbClient client = new TMDbClient(APIKey);
			if (isMovie)
			{
				SearchContainer<SearchMovie> search = await client.SearchMovieAsync(showName);
				return search.Results.Select(x =>
				{
					Show show = x.ToShow();
					show.ExternalIDs = new[] {new MetadataID(Provider, $"{x.Id}", $"https://www.themoviedb.org/movie/{x.Id}")}; 
					return show;
				});
			}
			else
			{
				SearchContainer<SearchTv> search = await client.SearchTvShowAsync(showName);
				return search.Results.Select(x =>
				{
					Show show = x.ToShow();
					show.ExternalIDs = new[] {new MetadataID(Provider, $"{x.Id}", $"https://www.themoviedb.org/tv/{x.Id}")}; 
					return show;
				});
			}
		}
		
		public async Task<Show> GetShowByID(Show show)
		{
			string id = show?.GetID(Provider.Name);
			if (id == null)
				return await Task.FromResult<Show>(null);
			TMDbClient client = new TMDbClient(APIKey);
			if (show.IsMovie)
			{
				Movie movie = await client.GetMovieAsync(id, MovieMethods.AlternativeTitles | MovieMethods.Videos);
				if (movie == null)
					return null;
				Show ret = movie.ToShow();
				ret.ExternalIDs = new[] {new MetadataID(Provider, $"{movie.Id}", $"https://www.themoviedb.org/movie/{movie.Id}")}; 
				return ret;
			}
			else
			{
				TvShow tv = await client.GetTvShowAsync(int.Parse(id), TvShowMethods.AlternativeTitles | TvShowMethods.Videos);
				if (tv == null)
					return null;
				Show ret = tv.ToShow();
				ret.ExternalIDs = new[] {new MetadataID(Provider, $"{tv.Id}", $"https://www.themoviedb.org/tv/{tv.Id}")}; 
				return ret;
			}
		}

		public async Task<IEnumerable<PeopleRole>> GetPeople(Show show)
		{
			string id = show?.GetID(Provider.Name);
			if (id == null)
				return await Task.FromResult(new List<PeopleRole>());
			TMDbClient client = new TMDbClient(APIKey);
			if (show.IsMovie)
			{
				Credits credits = await client.GetMovieCreditsAsync(int.Parse(id));
				return credits.Cast.Select(x =>
					new PeopleRole(Utility.ToSlug(x.Name),
							x.Name,
							x.Character,
							"Actor",
							x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null,
							new[] {new MetadataID(Provider, $"{x.Id}", $"https://www.themoviedb.org/person/{x.Id}")}))
						.Concat(credits.Crew.Select(x =>
							new PeopleRole(Utility.ToSlug(x.Name),
								x.Name,
								x.Job,
								x.Department,
								x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null,
								new[] {new MetadataID(Provider, $"{x.Id}", $"https://www.themoviedb.org/person/{x.Id}")})));
			}
			else
			{
				TvCredits credits = await client.GetTvShowCreditsAsync(int.Parse(id));
				return credits.Cast.Select(x =>
						new PeopleRole(Utility.ToSlug(x.Name),
							x.Name, 
							x.Character, 
							"Actor", 
							x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null, 
							new[] {new MetadataID(Provider, $"{x.Id}", $"https://www.themoviedb.org/person/{x.Id}")}))
					.Concat(credits.Crew.Select(x =>
						new PeopleRole(Utility.ToSlug(x.Name),
							x.Name, 
							x.Job, 
							x.Department, 
							x.ProfilePath != null ? "https://image.tmdb.org/t/p/original" + x.ProfilePath : null, 
							new[] {new MetadataID(Provider, $"{x.Id}", $"https://www.themoviedb.org/person/{x.Id}")})));
			}
		}

		public async Task<Season> GetSeason(Show show, int seasonNumber)
		{
			string id = show?.GetID(Provider.Name);
			if (id == null)
				return await Task.FromResult<Season>(null);
			TMDbClient client = new TMDbClient(APIKey);
			TvSeason season = await client.GetTvSeasonAsync(int.Parse(id), seasonNumber);
			if (season == null)
				return null;
			return new Season(show.ID,
				seasonNumber,
				season.Name,
				season.Overview,
				season.AirDate?.Year,
				season.PosterPath != null ? "https://image.tmdb.org/t/p/original" + season.PosterPath : null,
				new[] {new MetadataID(Provider, $"{season.Id}", $"https://www.themoviedb.org/tv/{id}/season/{season.SeasonNumber}")});
		}

		public async Task<Episode> GetEpisode(Show show, int seasonNumber, int episodeNumber, int absoluteNumber)
		{
			if (seasonNumber == -1 || episodeNumber == -1)
				return await Task.FromResult<Episode>(null);
			
			string id = show?.GetID(Provider.Name);
			if (id == null)
				return await Task.FromResult<Episode>(null);
			TMDbClient client = new TMDbClient(APIKey);
			TvEpisode episode = await client.GetTvEpisodeAsync(int.Parse(id), seasonNumber, episodeNumber);
			if (episode == null)
				return null;
			return new Episode(seasonNumber, episodeNumber, absoluteNumber,
				episode.Name,
				episode.Overview,
				episode.AirDate,
				0,
				episode.StillPath != null ? "https://image.tmdb.org/t/p/original" + episode.StillPath : null,
				new[] {new MetadataID(Provider, $"{episode.Id}", $"https://www.themoviedb.org/tv/{id}/season/{episode.SeasonNumber}/episode/{episode.EpisodeNumber}")});
		}
	}
	
	public static class Convertors
	{
		public static Show ToShow(this Movie movie)
		{
			return new Show(Utility.ToSlug(movie.Title),
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
				movie.BackdropPath != null ? "https://image.tmdb.org/t/p/original" + movie.BackdropPath : null,
				null)
			{
				Genres = movie.Genres.Select(x => new Genre(x.Name)),
				Studio = string.IsNullOrEmpty(movie.ProductionCompanies.FirstOrDefault()?.Name)
					? null
					: new Studio(movie.ProductionCompanies.First().Name),
				IsMovie = true
			};
		}

		public static Show ToShow(this TvShow tv)
		{
			return new Show(Utility.ToSlug(tv.Name),
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
				tv.BackdropPath != null ? "https://image.tmdb.org/t/p/original" + tv.BackdropPath : null,
				null)
			{
				Genres = tv.Genres.Select(x => new Genre(x.Name)),
				Studio = string.IsNullOrEmpty(tv.ProductionCompanies.FirstOrDefault()?.Name)
					? null
					: new Studio(tv.ProductionCompanies.First().Name),
				IsMovie = false
			};
		}

		public static Show ToShow(this SearchMovie movie)
		{
			return new Show(Utility.ToSlug(movie.Title),
				movie.Title,
				null,
				null,
				movie.Overview,
				null,
				Status.Finished,
				movie.ReleaseDate?.Year,
				movie.ReleaseDate?.Year,
				movie.PosterPath != null ? "https://image.tmdb.org/t/p/original" + movie.PosterPath : null,
				null,
				movie.BackdropPath != null ? "https://image.tmdb.org/t/p/original" + movie.BackdropPath : null,
				null)
			{
				IsMovie = true
			};
		}
		
		public static Show ToShow(this SearchTv tv)
		{
			return new Show(Utility.ToSlug(tv.Name),
				tv.Name,
				null,
				null,
				tv.Overview,
				null,
				Status.Finished,
				tv.FirstAirDate?.Year,
				null,
				tv.PosterPath != null ? "https://image.tmdb.org/t/p/original" + tv.PosterPath : null,
				null,
				tv.BackdropPath != null ? "https://image.tmdb.org/t/p/original" + tv.BackdropPath : null,
				null)
			{
				IsMovie = false
			};
		}
	}
}