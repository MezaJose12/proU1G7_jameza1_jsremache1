using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using proU1G7_jameza1_jsremache1.Models;
using System;
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
            /*Nose para que sirve ya despues consulto :(*/
            ApplicationDbContext db = new ApplicationDbContext();

            /*Iniciar*/
            roles(db);
            Csuper(db);
            arol(db);

            /*Esto hay que consultar*/
            db.Dispose();

        }
        /*Creación de usuarios Desarrollador José Meza :(*/
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
                /*envia los datos a crear y la contraseña xd*/
                usermanager.Create(user, "P@ssw0rd");
            }
        }
        /*Creación de roles Desarrollador José Meza :( */
        private void roles(ApplicationDbContext db)
        {
            var rolManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            /*Roles que van a existir en el crud xd :(*/
            if (!rolManager.RoleExists("view"))
            {
                rolManager.Create(new IdentityRole("view"));
            }
            if (!rolManager.RoleExists("edit"))
            {
                rolManager.Create(new IdentityRole("edit"));
            }
            if (!rolManager.RoleExists("delete"))
            {
                rolManager.Create(new IdentityRole("delete"));
            }
            if (!rolManager.RoleExists("create"))
            {
                rolManager.Create(new IdentityRole("create"));
            }
        }
        /*Asignacion de roles Desarrollador José Meza :(*/
        private void arol(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var rolManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            var superadmin = usermanager.FindByName("SuperAdmin");
            if(superadmin != null)
            {
                if (!usermanager.IsInRole(superadmin.Id, "view"))
                {
                    if (!usermanager.IsInRole(superadmin.Id, "view"))
                    {
                        usermanager.AddToRole(superadmin.Id, "view");
                    }
                    if (!usermanager.IsInRole(superadmin.Id, "edit"))
                    {
                        usermanager.AddToRole(superadmin.Id, "edit");
                    }
                    if (!usermanager.IsInRole(superadmin.Id, "delete"))
                    {
                        usermanager.AddToRole(superadmin.Id, "delete");
                    }
                    if (!usermanager.IsInRole(superadmin.Id, "create"))
                    {
                        usermanager.AddToRole(superadmin.Id, "create");
                    }
                }
            }
        }
    }
}
