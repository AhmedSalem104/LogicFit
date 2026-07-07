using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.PaymentMethods.Commands.DeletePaymentMethod;

public class DeletePaymentMethodCommandHandler : IRequestHandler<DeletePaymentMethodCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public DeletePaymentMethodCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await _context.TenantPaymentMethods
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TenantPaymentMethod), request.Id);

        _context.TenantPaymentMethods.Remove(method); // soft delete
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
