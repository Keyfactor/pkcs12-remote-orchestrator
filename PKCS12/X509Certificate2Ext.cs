using System.Security.Cryptography.X509Certificates;

namespace Keyfactor.Extensions.Orchestrator.PKCS12
{
    class X509Certificate2Ext : X509Certificate2
    {
        public string FriendlyNameExt { get; set; }

        public X509Certificate2Ext(byte[] bytes): base(bytes) { }
    }
}
