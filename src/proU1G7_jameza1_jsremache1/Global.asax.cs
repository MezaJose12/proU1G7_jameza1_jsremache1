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
                CrearRoles(db);
                CrearSuperUsuario(db);
                CrearAdministrador(db);
                CrearGerente(db);
                AsignarPermisos(db);
            }

#if DEBUG
            if (string.Equals(ConfigurationManager.AppSettings["EnableDemoData"], "true", StringComparison.OrdinalIgnoreCase))
            {
                string demoUserId;
                using (var identityDb = new ApplicationDbContext())
                {
                    var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));
                    var demoUser = userManager.FindByName("superadmin");
                    demoUserId = demoUser == null ? null : demoUser.Id;
                }

                DemoDataSeeder.Seed(demoUserId);
            }
#endif
        }

        // CAMBIO: las credenciales de las cuentas iniciales se definen aqui, no en Web.config.
        private void CrearSuperUsuario(ApplicationDbContext db)
        {
            CrearUsuarioInicial(db, "superadmin", "superadmin@example.com", "superadmin123");
        }

        private void CrearAdministrador(ApplicationDbContext db)
        {
            CrearUsuarioInicial(db, "administrator", "administrator@example.com", "superadmin123");
        }

        private void CrearGerente(ApplicationDbContext db)
        {
            CrearUsuarioInicial(db, "gerente", "gerente@example.com", "gerente123");
        }

        private static void CrearUsuarioInicial(
            ApplicationDbContext db,
            string nombre,
            string correo,
            string contrasena)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            if (userManager.FindByName(nombre) != null)
            {
                return;
            }

            var user = new ApplicationUser { UserName = nombre, Email = correo };
            VerificarResultado(userManager.Create(user, contrasena), "crear la cuenta inicial " + nombre);
        }

        // CAMBIO: se mantienen los roles funcionales del sistema y se agregan los permisos del ejemplo.
        private void CrearRoles(ApplicationDbContext db)
        {
            var rolManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            foreach (var nombreRol in new[]
            {
                "Cliente", "Counter", "Administrador", "SuperUsuario", "Gerente",
                "view", "create", "edit", "delete"
            })
            {
                if (!rolManager.RoleExists(nombreRol))
                {
                    VerificarResultado(rolManager.Create(new IdentityRole(nombreRol)), "crear el rol " + nombreRol);
                }
            }
        }

        private void AsignarPermisos(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var superUsuario = usermanager.FindByName("superadmin");
            var administrador = usermanager.FindByName("administrator");
            var gerente = usermanager.FindByName("gerente");

            if (gerente != null)
            {
                AsignarRolSiFalta(usermanager, gerente, "Gerente");
                AsignarRolSiFalta(usermanager, gerente, "view");
            }

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
                AsignarRolSiFalta(usermanager, administrador, "edit");
                AsignarRolSiFalta(usermanager, administrador, "delete");
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
