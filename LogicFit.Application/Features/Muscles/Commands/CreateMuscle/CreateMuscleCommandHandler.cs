using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Muscles.DTOs;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.Muscles.Commands.CreateMuscle;

public class CreateMuscleCommandHandler : IRequestHandler<CreateMuscleCommand, MuscleDto>
{
    private readonly IApplicationDbContext _context;

    public CreateMuscleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MuscleDto> Handle(CreateMuscleCommand request, CancellationToken cancellationToken)
    {
        var muscle = new Muscle
        {
            Name = request.Name,
            NameAr = request.NameAr,
            BodyPart = request.BodyPart,
            Description = request.Description,
            DescriptionAr = request.DescriptionAr,
            Icon = request.Icon
        };

        _context.Muscles.Add(muscle);
        await _context.SaveChangesAsync(cancellationToken);

        return new MuscleDto
        {
            Id = muscle.Id,
            Name = muscle.Name,
            NameAr = muscle.NameAr,
            BodyPart = muscle.BodyPart,
            Description = muscle.Description,
            DescriptionAr = muscle.DescriptionAr,
            Icon = muscle.Icon
        };
    }
}
