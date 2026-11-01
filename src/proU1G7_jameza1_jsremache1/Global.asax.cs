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
                    var superUserName = ConfigurationManager.AppSettings["InitialSuperUserUserName"];
                    var demoUser = string.IsNullOrWhiteSpace(superUserName) ? null : userManager.FindByName(superUserName);
                    demoUserId = demoUser == null ? null : demoUser.Id;
                }

                DemoDataSeeder.Seed(demoUserId);
            }
#endif
        }

        // CAMBIO: las cuentas de inicio se crean solo con credenciales locales configuradas.
        private void Csuper(ApplicationDbContext db)
        {
            CrearUsuarioInicial(db, "InitialSuperUserUserName", "InitialSuperUserEmail", "InitialSuperUserPassword");
        }

        private void Cadministrador(ApplicationDbContext db)
        {
            CrearUsuarioInicial(db, "InitialAdministratorUserName", "InitialAdministratorEmail", "InitialAdministratorPassword");
        }

        private static void CrearUsuarioInicial(
            ApplicationDbContext db,
            string claveUsuario,
            string claveCorreo,
            string claveContrasena)
        {
            var nombre = ConfigurationManager.AppSettings[claveUsuario];
            var correo = ConfigurationManager.AppSettings[claveCorreo];
            var contrasena = ConfigurationManager.AppSettings[claveContrasena];
            if (string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(correo) && string.IsNullOrWhiteSpace(contrasena))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                throw new InvalidOperationException("Configure usuario, correo y contraseña locales para la cuenta inicial " + claveUsuario + ".");
            }

            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            if (userManager.FindByName(nombre) != null)
            {
                return;
            }

            var user = new ApplicationUser { UserName = nombre.Trim(), Email = correo.Trim() };
            VerificarResultado(userManager.Create(user, contrasena), "crear la cuenta inicial " + nombre);
        }

        /* Creación de roles, incluido Counter con permisos operativos limitados. */
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

        private void arol(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var nombreSuperUsuario = ConfigurationManager.AppSettings["InitialSuperUserUserName"];
            var nombreAdministrador = ConfigurationManager.AppSettings["InitialAdministratorUserName"];
            var superUsuario = string.IsNullOrWhiteSpace(nombreSuperUsuario) ? null : usermanager.FindByName(nombreSuperUsuario);
            var administrador = string.IsNullOrWhiteSpace(nombreAdministrador) ? null : usermanager.FindByName(nombreAdministrador);

            if (superUsuario != null)
            {
                AsignarRolSiFalta(usermanager, superUsuario, "SuperUsuario");
                AsignarRolSiFalta(usermanager, superUsuario, "view");
                AsignarRolSiFalta(usermanager, superUsuario, "create");
                AsignarRolSiFalta(usermanager, superUsuario, "edit");
                AsignarRolSiFalta(usermanager, superUsuario, "delete");
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
