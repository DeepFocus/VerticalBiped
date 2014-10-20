using AutoMapper;
using JumpFocus.Models;
using JumpFocus.Models.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpFocus.Configurations
{
    class AutoMapperConfiguration
    {
        public static void RegisterMappings()
        {
            Mapper.CreateMap<TwitterUser, Player>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
                .ForMember(dest => dest.TwitterId, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.TwitterHandle, opt => opt.MapFrom(src => src.screen_name))
                .ForMember(dest => dest.TwitterPhoto, opt => opt.MapFrom(src => src.profile_image_url))
                .ForMember(dest => dest.Created, opt => opt.UseValue(DateTime.Now));
        }
    }
}
