﻿using Rise.Common.Constants;
using Rise.Common.Enums;
using Rise.Common.Extensions;
using Rise.Common.Interfaces;
using SQLite;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Rise.Models
{
    /// <summary>
    /// Represents a song.
    /// </summary>
    [Table("Songs")]
    public partial class Song : DbObject, IEquatable<Song>, IMatchable<Song>
    {
        public string Title { get; set; }

        public string Artist { get; set; }

        [NotNull]
        public uint Track { get; set; }

        [NotNull]
        public int Disc { get; set; }

        public string Album { get; set; }

        public string AlbumArtist { get; set; }

        public string Genres { get; set; }

        public TimeSpan Length { get; set; }

        [NotNull]
        public uint Year { get; set; }

        [Unique]
        public string Location { get; set; }

        [NotNull]
        public uint Rating { get; set; }

        [NotNull]
        public uint Bitrate { get; set; }

        public string Thumbnail { get; set; }

        [Ignore]
        public bool IsLocal { get; set; } = true;

        /// <summary>
        /// Returns the song title.
        /// </summary>
        public override string ToString()
            => Title;
    }

    // Constructors/Factory methods
    public partial class Song
    {
        /// <summary>
        /// Creates a <see cref="Song"/> based on a <see cref="StorageFile"/>.
        /// </summary>
        /// <param name="file">Song file.</param>
        /// <returns>A song based on the file.</returns>
        public static async Task<Song> GetFromFileAsync(StorageFile file)
        {
            // Put the value into memory to make sure that the system
            // really fetches the properties
            var musicProperties = await file.Properties.GetMusicPropertiesAsync();

            int cd = 1;
            var extraProps = await file.Properties.
                RetrievePropertiesAsync(SongProperties.DiscProperties);

            // Check if disc number is valid
            string disc, prop = string.Empty;
            if (extraProps.ContainsKey(SystemMusic.DiscNumber))
                prop = SystemMusic.DiscNumber;
            else if (extraProps.ContainsKey(SystemMusic.PartOfSet))
                prop = SystemMusic.PartOfSet;

            if (!string.IsNullOrEmpty(prop))
            {
                disc = extraProps[prop].ToString();
                if (int.TryParse(disc, out int result))
                {
                    cd = result;
                }
                else if (disc.TryGetUntil('/', out string setPart))
                {
                    // iTunes uses the part of set property to store the
                    // disc number, using the {Disc}/{Number of discs in album}
                    // format - main reason why this second check exists
                    if (int.TryParse(setPart, out int part))
                        cd = part;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Couldn't parse {0} property with value {1}", prop, disc);
                }
            }

            string albumTitle = musicProperties.Album.ReplaceIfNullOrWhiteSpace("UnknownAlbumResource");
            string thumb = URIs.MusicThumb;

            using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200);
            string filename = albumTitle.AsValidFileName();

            if (await thumbnail.SaveToFileAsync($@"{filename}.png"))
                thumb = $@"ms-appdata:///local/{filename}.png";

            return new Song
            {
                Title = musicProperties.Title.ReplaceIfNullOrWhiteSpace(file.DisplayName),
                Artist = musicProperties.Artist.ReplaceIfNullOrWhiteSpace("UnknownArtistResource"),
                Album = albumTitle,
                AlbumArtist = musicProperties.AlbumArtist.ReplaceIfNullOrWhiteSpace("UnknownArtistResource"),
                Genres = musicProperties.Genre.FirstOrDefault() ?? "UnknownGenreResource",
                Track = musicProperties.TrackNumber,
                Disc = cd,
                Thumbnail = thumb,
                Length = musicProperties.Duration,
                Year = musicProperties.Year,
                Location = file.Path,
                Rating = musicProperties.Rating,
                Bitrate = musicProperties.Bitrate,
                IsLocal = file.Provider.Id == "computer"
            };
        }
    }

    // IEquatable implementation
    public partial class Song : IEquatable<Song>
    {
        public bool Equals(Song other)
        {
            return Title == other.Title &&
                   Artist == other.Artist &&
                   Track == other.Track &&
                   Disc == other.Disc &&
                   Album == other.Album &&
                   AlbumArtist == other.AlbumArtist &&
                   Genres == other.Genres &&
                   Length == other.Length &&
                   Year == other.Year;
        }

        public override int GetHashCode()
        {
            return (Title, Artist, Track, Disc, Album,
                AlbumArtist, Genres, Length, Year).GetHashCode();
        }
    }

    // IMatchable implementation
    public partial class Song : IMatchable<Song>
    {
        public MatchLevel Matches(Song other)
        {
            if (Title.Equals(other.Title))
            {
                return MatchLevel.Full;
            }

            if (Title.Contains(other.Title))
            {
                return MatchLevel.Partial;
            }

            return MatchLevel.None;
        }
    }
}
