using AutoMapper;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.V2_3_1;
using IrisPoc.Models;
using IrisPoc.Models.Image;
using IrisPoc.Models.Storage;

namespace IrisPoc.Maps;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ExamModel, OrderRequest>()
            .ConvertUsing<OrderMapper>();
        CreateMap<PatientModel, RequestPatient>()
            .ConvertUsing<PatientMapper>();
        CreateMap<ProviderModel, RequestProvider>()
            .ConvertUsing<ProviderMapper>();
        CreateMap<ImageModel, RequestImage>()
            .ConvertUsing<RequestImageMapper>();
        CreateMap<UploadBlobResponse, RequestImage>()
            .ConvertUsing<RequestImageMapper>();
    }
}
