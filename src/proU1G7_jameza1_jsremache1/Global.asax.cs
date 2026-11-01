using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using proU1G7_jameza1_jsremache1.Models;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace proU1G7_jameza1_jsremache1
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Identity guarda usuarios y roles en su propia base de datos.
            using (var db = new ApplicationDbContext())
            {
                roles(db);
                Csuper(db);
                Cadministrador(db);
                arol(db);
            }

#if DEBUG
            if (string.Equals(ConfigurationManager.AppSettings["EnableDemoData"], "true", StringComparison.OrdinalIgnoreCase))
            {
                string demoUserId;
                using (var identityDb = new ApplicationDbContext())
                {
                    var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));
                    var demoUser = userManager.FindByName("SuperAdmin");
                    demoUserId = demoUser == null ? null : demoUser.Id;
                }

                DemoDataSeeder.Seed(demoUserId);
            }
#endif
        }

        /* Creación de usuarios iniciales. */
        /*Usuario superadmin*/
        private void Csuper(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("SuperAdmin");
            /*si no existe crealo*/
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "SuperAdmin",
                    Email = "jameza1@espe.edu.ec"
                };
                // Identity puede rechazar la cuenta si la contraseña no cumple su política.
                VerificarResultado(usermanager.Create(user, "P@ssw0rd"), "crear el usuario SuperAdmin");
            }
        }

        private void Cadministrador(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("Administrador");
            /*si no existe crealo*/
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "Administrador",
                    Email = "jsremache1@espe.edu.ec"
                };
                VerificarResultado(usermanager.Create(user, "P@ssw0rd"), "crear el usuario Administrador");
            }
        }

        /* Creación de roles CRUD. */
        private void roles(ApplicationDbContext db)
        {
            var rolManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            foreach (var nombreRol in new[]
            {
                "Cliente", "Counter", "Administrador", "SuperUsuario",
                "view", "create", "edit", "delete"
            })
            {
                if (!rolManager.RoleExists(nombreRol))
                {
                    VerificarResultado(rolManager.Create(new IdentityRole(nombreRol)), "crear el rol " + nombreRol);
                }
            }
        }

        /* Asigna por separado cada permiso y siempre al usuario correspondiente. */
        private void arol(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var superadmin = usermanager.FindByName("SuperAdmin");
            var administrador = usermanager.FindByName("Administrador");

            if (superadmin != null)
            {
                AsignarRolSiFalta(usermanager, superadmin, "SuperUsuario");
                AsignarRolSiFalta(usermanager, superadmin, "view");
                AsignarRolSiFalta(usermanager, superadmin, "create");
                AsignarRolSiFalta(usermanager, superadmin, "edit");
                AsignarRolSiFalta(usermanager, superadmin, "delete");
            }

            if (administrador != null)
            {
                AsignarRolSiFalta(usermanager, administrador, "Administrador");
                AsignarRolSiFalta(usermanager, administrador, "view");
                AsignarRolSiFalta(usermanager, administrador, "create");
                AsignarRolSiFalta(usermanager, administrador, "edit");
            }
        }

        private static void AsignarRolSiFalta(
            UserManager<ApplicationUser> usermanager,
            ApplicationUser usuario,
            string nombreRol)
        {
            if (!usermanager.IsInRole(usuario.Id, nombreRol))
            {
                VerificarResultado(
                    usermanager.AddToRole(usuario.Id, nombreRol),
                    "asignar el rol " + nombreRol + " a " + usuario.UserName);
            }
        }

        private static void VerificarResultado(IdentityResult resultado, string operacion)
        {
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo " + operacion + ": " + string.Join("; ", resultado.Errors));
            }
        }
    }
}
