using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.PaymentMethods.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.PaymentMethods.Queries.GetPaymentMethods;

public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, List<PaymentMethodDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPaymentMethodsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PaymentMethodDto>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TenantPaymentMethods.AsQueryable();
        if (request.ActiveOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new PaymentMethodDto
            {
                Id = m.Id,
                Name = m.Name,
                Type = m.Type,
                AccountName = m.AccountName,
                AccountNumber = m.AccountNumber,
                IBAN = m.IBAN,
                WalletNumber = m.WalletNumber,
                Instructions = m.Instructions,
                QRImageUrl = m.QRImageUrl,
                IsActive = m.IsActive,
                DisplayOrder = m.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }
}
