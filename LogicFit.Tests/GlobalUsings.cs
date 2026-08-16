// The tenant migration project introduces the LogicFit.Tenant namespace. Keep the
// historical test fixtures' unqualified Tenant references bound to the domain entity.
global using Tenant = LogicFit.Domain.Entities.Tenant;
