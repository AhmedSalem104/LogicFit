using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.PaymentMethods.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.PaymentMethods.Commands.SavePaymentMethod;

public class SavePaymentMethodCommandHandler : IRequestHandler<SavePaymentMethodCommand, PaymentMethodDto>
{
    private readonly IApplicationDbContext _context;

    public SavePaymentMethodCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentMethodDto> Handle(SavePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        TenantPaymentMethod method;

        if (request.Id.HasValue)
        {
            method = await _context.TenantPaymentMethods
                .FirstOrDefaultAsync(m => m.Id == request.Id.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(TenantPaymentMethod), request.Id.Value);
        }
        else
        {
            method = new TenantPaymentMethod();
            _context.TenantPaymentMethods.Add(method);
        }

        method.Name = request.Name;
        method.Type = request.Type;
        method.AccountName = request.AccountName;
        method.AccountNumber = request.AccountNumber;
        method.IBAN = request.IBAN;
        method.WalletNumber = request.WalletNumber;
        method.Instructions = request.Instructions;
        method.QRImageUrl = request.QRImageUrl;
        method.IsActive = request.IsActive;
        method.DisplayOrder = request.DisplayOrder;

        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentMethodDto
        {
            Id = method.Id,
            Name = method.Name,
            Type = method.Type,
            AccountName = method.AccountName,
            AccountNumber = method.AccountNumber,
            IBAN = method.IBAN,
            WalletNumber = method.WalletNumber,
            Instructions = method.Instructions,
            QRImageUrl = method.QRImageUrl,
            IsActive = method.IsActive,
            DisplayOrder = method.DisplayOrder
        };
    }
}
