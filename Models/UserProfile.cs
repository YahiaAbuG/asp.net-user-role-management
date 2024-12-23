using AutoMapper;
using Microsoft.AspNetCore.Identity;
using WebApplication5.Models.ViewModels;

namespace WebApplication5.Models
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<ApplicationUser, UserRolesViewModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
            CreateMap<IdentityRole, ManageUserRolesViewModel>()
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Name));
            CreateMap<CreateUserViewModel, ApplicationUser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<ApplicationUser, EditUserViewModel>();
            CreateMap<EditUserViewModel, ApplicationUser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
