using System;
using System.Collections.Generic;
using Koturn.VRChat.WebApi.Enums;


namespace Koturn.VRChat.WebApi
{
    public record class UserInfo(
        string Id,
        string Name,
        bool? AllowAvatarCopying,
        string? Bio,
        string CurrentAvatarImageUrl,
        string CurrentAvatarThumbnailImageUrl,
        DateTime? DateJoined,
        DeveloperType DeveloperType,
        string FriendKey,
        FriendRequestStatus? FriendRequestStatus,
        string? InstancePart,
        bool IsFriend,
        DateTime? LastActivity,
        DateTime? LastLogin,
        Platform LastPlatform,
        string? Location,
        string? Note,
        string ProfilePicOverride,
        UserState? State,
        UserStatus Status,
        string StatusDescription,
        string? TravelingToInstance,
        string? TravelingToLocation,
        string? TravelingToWorld,
        string? UserIcon,
        string? WorldId)
    {
        // public string Id { get; } = Id;
        // public string Name { get; } = Name;
        // public bool? AllowAvatarCopying { get; } = AllowAvatarCopying;
        // public string? Bio { get; } = Bio;
        // public string CurrentAvatarImageUrl { get; } = CurrentAvatarImageUrl;
        // public string CurrentAvatarThumbnailImageUrl { get; } = CurrentAvatarThumbnailImageUrl;
        // public DateTime? DateJoined { get; } = DateJoined;
        // public DeveloperType DeveloperType { get; } = DeveloperType;
        // public string FriendKey { get; } = FriendKey;
        // public FriendRequestStatus? FriendRequestStatus { get; } = FriendRequestStatus;
        // public string? InstancePart { get; } = InstancePart;
        // public bool IsFriend { get; } = IsFriend;
        // public DateTime? LastActivity { get; } = LastActivity;
        // public DateTime? LastLogin { get; } = LastLogin;
        // public Platform LastPlatform { get; } = LastPlatform;
        // public string? Location { get; } = Location;
        // public string? Note { get; } = Note;
        // public string ProfilePicOverride { get; } = ProfilePicOverride;
        // public UserState? State { get; } = State;
        // public UserStatus Status { get; } = Status;
        // public string StatusDescription { get; } = StatusDescription;
        // public string? TravelingToInstance { get; } = TravelingToInstance;
        // public string? TravelingToLocation { get; } = TravelingToLocation;
        // public string? TravelingToWorld { get; } = TravelingToWorld;
        // public string? UserIcon { get; } = UserIcon;
        // public string? WorldId { get; } = WorldId;
        public List<string> Tags { get; } = new List<string>();
    }
}
