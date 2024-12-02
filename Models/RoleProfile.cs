using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace WebApplication5.Models
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            CreateMap<IdentityRole, EditRoleViewModel>();
            CreateMap<EditRoleViewModel, IdentityRole>();
        }
    }
}
