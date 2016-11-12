using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Activos_PrestamosOET.Startup))]
namespace Activos_PrestamosOET
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
