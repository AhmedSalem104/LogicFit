using LogicFit.Application.Features.Foods.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Foods.Queries.GetFoods;

public class GetFoodsQuery : IRequest<List<FoodDto>>
{
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsVerified { get; set; }
}
