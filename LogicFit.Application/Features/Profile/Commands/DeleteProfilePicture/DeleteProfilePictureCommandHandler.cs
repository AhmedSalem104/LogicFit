using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Profile.Commands.DeleteProfilePicture;

public class DeleteProfilePictureCommandHandler : IRequestHandler<DeleteProfilePictureCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileUploadService _fileUploadService;

    public DeleteProfilePictureCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileUploadService fileUploadService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _fileUploadService = fileUploadService;
    }

    public async Task<bool> Handle(DeleteProfilePictureCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = Guid.Parse(_currentUserService.UserId);

        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", userId);

        if (user.Profile == null || string.IsNullOrEmpty(user.Profile.ProfilePictureUrl))
            return true;

        // Delete the file from storage
        await _fileUploadService.DeleteFileAsync(user.Profile.ProfilePictureUrl);

        // Clear the URL in database
        user.Profile.ProfilePictureUrl = null;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
