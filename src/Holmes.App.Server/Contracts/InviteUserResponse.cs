using Holmes.Users.Application.Abstractions.Dtos;

namespace Holmes.App.Server.Contracts;

public sealed record InviteUserResponse(UserDto User, string ConfirmationLink);