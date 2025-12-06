using AutoMapper;
using LogicFit.Domain.Entities;

namespace LogicFit.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Tenant mappings will be added as features are implemented
        // Example:
        // CreateMap<Tenant, TenantDto>();
        // CreateMap<CreateTenantCommand, Tenant>();
    }
}
