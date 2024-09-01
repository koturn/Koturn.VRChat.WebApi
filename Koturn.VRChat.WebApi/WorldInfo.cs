using System;
using System.Collections.Generic;
using Koturn.VRChat.WebApi.Enums;


namespace Koturn.VRChat.WebApi
{
    // Error	CS0518	Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported.
    public record class WorldInfo(
        string Id,
        string Name,
        string? Namespace,
        string AuthorId,
        string AuthorName,
        int Capacity,
        string? Description,
        ReleaseStatus ReleaseStatus,
        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        DateTime? LabsPublicationDate,
        DateTime? PublicationDate,
        int? Version,
        int Visits,
        int Favorites,
        int Heat,
        bool? Featured,
        string ImageUrl,
        string ThumbnailImageUrl,
        string? YoutubeUrl,
        string Organization,
        int Popularity,
        int Occupants,
        int? PrivateOccupants,
        int? PublicOccupants,
        string? FavoriteId,
        string? FavoriteGroup)
    {
        // public string Id { get; } = Id;
        // public string Name { get; } = Name;
        // public string? Namespace { get; } = Namespace;
        // public string AuthorId { get; } = AuthorId;
        // public string AuthorName { get; } = AuthorName;
        // public int Capacity { get; } = Capacity;
        // public string? Description { get; } = Description;
        // public ReleaseStatus ReleaseStatus { get; } = ReleaseStatus;
        // public DateTime? CreatedAt { get; } = CreatedAt;
        // public DateTime? UpdatedAt { get; } = UpdatedAt;
        // public DateTime? LabsPublicationDate { get; } = LabsPublicationDate;
        // public DateTime? PublicationDate { get; } = PublicationDate;
        // public int? Version { get; } = Version;
        // public int Visits { get; } = Visits;
        // public int Favorites { get; } = Favorites;
        // public int Heat { get; } = Heat;
        // public bool? Featured { get; } = Featured;
        // public string ImageUrl { get; } = ImageUrl;
        // public string ThumbnailImageUrl { get; } = ThumbnailImageUrl;
        // public string? YoutubeUrl { get; } = YoutubeUrl;
        // public string Organization { get; } = Organization;
        // public int Popularity { get; } = Popularity;
        // public int Occupants { get; } = Occupants;
        // public int? PrivateOccupants { get; } = PrivateOccupants;
        // public int? PublicOccupants { get; } = PublicOccupants;
        // public string? FavoriteId { get; } = FavoriteId;
        // public string? FavoriteGroup { get; } = FavoriteGroup;
        // no member for "instances"
        // no member for "unityPackages"
        public List<string> Tags { get; } = new List<string>();
    }
}

