using FluentValidation;
using MediatR;
using MVSoftware.Flexibill.Application.Common;
using MVSoftware.Flexibill.Application.Common.Interfaces;

namespace MVSoftware.Flexibill.Application.Approvals.Commands;

/// <summary>Verwijdert een leverancier-uitzondering; facturen van die leverancier vallen daarna weer onder de standaardflow.</summary>
public sealed record DeleteSupplierExceptionCommand(Guid BranchId, Guid SupplierId) : IRequest<Result>;

public sealed class DeleteSupplierExceptionCommandValidator : AbstractValidator<DeleteSupplierExceptionCommand>
{
    public DeleteSupplierExceptionCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
    }
}

public sealed class DeleteSupplierExceptionCommandHandler(
    IApprovalFlowSettingRepository approvalFlowRepository) : IRequestHandler<DeleteSupplierExceptionCommand, Result>
{
    public async Task<Result> Handle(DeleteSupplierExceptionCommand request, CancellationToken cancellationToken)
    {
        var flow = await approvalFlowRepository.GetSupplierExceptionAsync(request.BranchId, request.SupplierId, cancellationToken);
        if (flow is null)
        {
            return Result.Failure("Deze uitzondering bestaat niet (meer).");
        }

        await approvalFlowRepository.DeleteAsync(flow.Id, cancellationToken);
        return Result.Success();
    }
}
