using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Muscles.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Muscles.Commands.CreateMuscle;

public class CreateMuscleCommandHandler : IRequestHandler<CreateMuscleCommand, MuscleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateMuscleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MuscleDto> Handle(CreateMuscleCommand request, CancellationToken cancellationToken)
    {
        await EnsureCatalogAdministratorAsync(cancellationToken);

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

    private async Task EnsureCatalogAdministratorAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var role = await _context.Users.Where(u => u.Id == userId).Select(u => u.Role).FirstOrDefaultAsync(cancellationToken);
        if (role is not (UserRole.Owner or UserRole.Manager))
            throw new ForbiddenException("Only gym owners or managers can modify the muscle catalog");
    }
}
