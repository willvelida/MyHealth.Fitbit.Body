using AutoMapper;
using mdl = MyHealth.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using MyHealth.Fitbit.Body.Models;
using System.Globalization;

namespace MyHealth.Fitbit.Body.Profiles
{
    [ExcludeFromCodeCoverage]
    public class WeightProfile : Profile
    {
        public WeightProfile()
        {
            CreateMap<Weight, mdl.Weight>()
                .ForMember(
                    dest => dest.BMI,
                    opt => opt.MapFrom(src => src.bmi))
                .ForMember(
                    dest => dest.Date,
                    opt => opt.MapFrom(src => src.date))
                .ForMember(
                    dest => dest.Fat,
                    opt => opt.MapFrom(src => src.fat))
                .ForMember(
                    dest => dest.MeasurementSource,
                    opt => opt.MapFrom(src => src.source))
                .ForMember(
                    dest => dest.Time,
                    opt => opt.MapFrom(src => DateTime.ParseExact(src.time, "HH:mm:ss", CultureInfo.InvariantCulture)))
                .ForMember(
                    dest => dest.WeightInKG,
                    opt => opt.MapFrom(src => src.weight));
        }
    }
}
