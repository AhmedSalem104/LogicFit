using LogicFit.Application.Features.Foods.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Foods.Queries.GetFoodById;

public class GetFoodByIdQuery : IRequest<FoodDto?>
{
    public int Id { get; set; }
}
