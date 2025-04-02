using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signify.ElasticGrafanaPoc
{
    public static class Config
    {
        // This is output when running the image for the first time.
        public const string CertificateFingerprint = "b88007ab2dbb4e4fa49e828c09b75764f9c8fc8d4e6dd3a6f92f8d16edf14ffe";
        public const string Username = "elastic";
        public const string Password = "Welcome1"; // The password is also output during the first run. Username is always elastic.
        public const string ElasticUri = "https://localhost:9200";  // No change if running the docker image.
    }
}
