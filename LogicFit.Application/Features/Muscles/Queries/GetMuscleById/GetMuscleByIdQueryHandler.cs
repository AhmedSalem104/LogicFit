using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Muscles.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Muscles.Queries.GetMuscleById;

public class GetMuscleByIdQueryHandler : IRequestHandler<GetMuscleByIdQuery, MuscleDto?>
{
    private readonly IApplicationDbContext _context;

    public GetMuscleByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MuscleDto?> Handle(GetMuscleByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Muscles
            .Where(m => m.Id == request.Id)
            .Select(m => new MuscleDto
            {
                Id = m.Id,
                Name = m.Name,
                BodyPart = m.BodyPart
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
