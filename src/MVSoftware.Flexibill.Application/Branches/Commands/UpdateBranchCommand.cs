using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Application.Branches.Commands;

/// <summary>UC-B2: Beheerder wijzigt naam/adres van een bestaande vestiging.</summary>
public sealed record UpdateBranchCommand(
    Guid BranchId,
    string Name,
    string? Street,
    string? HouseNumber,
    string? PostalCode,
    string? City) : IRequest<Result>;

public sealed class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateBranchCommandHandler(
    ICurrentUserContext currentUser,
    IBranchRepository branchRepository) : IRequestHandler<UpdateBranchCommand, Result>
{
    public async Task<Result> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null || branch.OrganizationId != currentUser.OrganizationId)
        {
            return Result.Failure("De vestiging is niet gevonden.");
        }

        Address? address = string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.City)
            ? null
            : Address.Of(request.Street, request.HouseNumber ?? string.Empty, request.PostalCode ?? string.Empty, request.City);

        branch.UpdateDetails(request.Name, address);
        await branchRepository.UpdateAsync(branch, cancellationToken);

        return Result.Success();
    }
}
