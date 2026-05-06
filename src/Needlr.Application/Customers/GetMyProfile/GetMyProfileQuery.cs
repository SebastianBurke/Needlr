using MediatR;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Customers.GetMyProfile;

public sealed record GetMyProfileQuery : IRequest<Result<MyCustomerProfileDto>>;
