using AutoMapper;

namespace Signify.A1C.Svc.Core.Tests
{
    /// <summary>
    /// Use this map fixture to allow your unit tests to share mapping profiles with the code.  Ensure the mapping profiles are defined in the Core project so they can be referenced.
    /// 
    /// </summary>
    public class MapFixture
    {
        public IMapper Mapper { get; }
        
        public MapFixture()
        {
            var mappingConfig = new MapperConfiguration(mc =>
            {
                //Uncommment and replace with your mapping profile class.
                //mc.AddProfile(new MyMappingProfile());
            });

            Mapper = mappingConfig.CreateMapper();
        }
    }
}