using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Branches;
using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Application.Branches.Commands;

/// <summary>UC-B2: Beheerder maakt een nieuwe vestiging aan.</summary>
public sealed record CreateBranchCommand(
    string Name,
    string? Street,
    string? HouseNumber,
    string? PostalCode,
    string? City) : IRequest<Result<Guid>>;

public sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateBranchCommandHandler(
    ICurrentUserContext currentUser,
    IBranchRepository branchRepository) : IRequestHandler<CreateBranchCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        Address? address = string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.City)
            ? null
            : Address.Of(request.Street, request.HouseNumber ?? string.Empty, request.PostalCode ?? string.Empty, request.City);

        var branch = Branch.Create(currentUser.OrganizationId, request.Name, address);

        // UC-B2: het opslaan schrijft BranchCreatedEvent automatisch naar de outbox
        // (DomainEventDispatchInterceptor, Technisch Ontwerp hoofdstuk 3.3/6.3/12); de Worker
        // haalt het daar op en laat BranchCreatedEventHandler de simpele standaard-fiateringsflow
        // aanmaken (FO 6.4) - dit gebeurt dus asynchroon, niet meer binnen dit request.
        await branchRepository.AddAsync(branch, cancellationToken);

        return Result<Guid>.Success(branch.Id);
    }
}
