using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Muscles.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Muscles.Queries.GetMuscles;

public class GetMusclesQueryHandler : IRequestHandler<GetMusclesQuery, List<MuscleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMusclesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MuscleDto>> Handle(GetMusclesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Muscles.AsQueryable();

        if (!string.IsNullOrEmpty(request.BodyPart))
        {
            query = query.Where(m => m.BodyPart == request.BodyPart);
        }

        return await query
            .Select(m => new MuscleDto
            {
                Id = m.Id,
                Name = m.Name,
                NameAr = m.NameAr,
                BodyPart = m.BodyPart,
                Description = m.Description,
                DescriptionAr = m.DescriptionAr,
                Icon = m.Icon
            })
            .ToListAsync(cancellationToken);
    }
}
