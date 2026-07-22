using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Muscles.DTOs;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Muscles.Commands.UpdateMuscle;

public class UpdateMuscleCommandHandler : IRequestHandler<UpdateMuscleCommand, MuscleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMuscleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MuscleDto> Handle(UpdateMuscleCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var role = await _context.Users.Where(u => u.Id == userId).Select(u => u.Role).FirstOrDefaultAsync(cancellationToken);
        if (role is not (UserRole.Owner or UserRole.Manager))
            throw new ForbiddenException("Only gym owners or managers can modify the muscle catalog");

        var muscle = await _context.Muscles
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (muscle == null)
            throw new NotFoundException("Muscle", request.Id);

        muscle.Name = request.Name;
        muscle.NameAr = request.NameAr;
        muscle.BodyPart = request.BodyPart;
        muscle.Description = request.Description;
        muscle.DescriptionAr = request.DescriptionAr;
        muscle.Icon = request.Icon;

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
