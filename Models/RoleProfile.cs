using AutoMapper;
using Microsoft.AspNetCore.Identity;
using WebApplication5.Models.ViewModels;

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
