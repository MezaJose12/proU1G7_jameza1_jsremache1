using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(proU1G7_jameza1_jsremache1.Startup))]
namespace proU1G7_jameza1_jsremache1
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
