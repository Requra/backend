
using AutoMapper;
using Requra.Application.DTOs.Document;
using Requra.Application.DTOs.Project;
using Requra.Application.DTOs.Project.ProjectResults.UserStory;
using Requra.Domain.Entities;
using Requra.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
namespace Requra.Application.Mappings
{
    public class MappingProfile :Profile
    {
        public MappingProfile()
        {
            #region ProjectProfile
            CreateMap<Project, ProjectDTO>()

            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))

            .ForMember(dest => dest.ClientName,
                opt => opt.MapFrom(src =>
                    src.Members
                        .Where(m => m.Role == ProjectRole.Viewer)
                        .Select(m => m.User.FullName)
                        .FirstOrDefault()))

            .ForMember(dest => dest.TotalRequirements,
                opt => opt.MapFrom(src =>
                    src.Documents
                        .SelectMany(d => d.DocumentRequirements)
                        .Select(dr => dr.RequirementId)
                        .Distinct()
                        .Count()))

            .ForMember(dest => dest.TotalUserStories,
                opt => opt.MapFrom(src =>
                    src.Documents
                        .SelectMany(d => d.DocumentRequirements)
                        .SelectMany(dr => dr.Requirement.UserStories)
                        .Count()))

            .ForMember(dest => dest.TotalComments,
                opt => opt.MapFrom(src =>
                    src.Documents
                        .SelectMany(d => d.DocumentRequirements)
                        .SelectMany(dr => dr.Requirement.UserStories)
                        .SelectMany(us => us.Comments)
                        .Count()));
            #endregion


            #region UserStoryProfile
            CreateMap<UserStory, UserStoryDto>()

            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))

            .ForMember(dest => dest.Priority,
                opt => opt.MapFrom(src => src.Priority.ToString()))

            .ForMember(dest => dest.Language,
                opt => opt.MapFrom(src => src.Language != null ? src.Language.ToString() : null))

            .ForMember(dest => dest.CreatorName,
                opt => opt.MapFrom(src => src.Creator.FullName));
            #endregion


            #region DocumentProfile

            CreateMap<Document, DocumentDto>()
            .ForMember(dest => dest.Type,
                opt => opt.MapFrom(src => src.Type.ToString()))

            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))

            .ForMember(dest => dest.Language,
                opt => opt.MapFrom(src => src.Language.ToString()))

            .ForMember(dest => dest.UploadedBy,
                opt => opt.MapFrom(src => src.Uploader != null ? src.Uploader.FullName : null));



            #endregion

        }
    }
}
