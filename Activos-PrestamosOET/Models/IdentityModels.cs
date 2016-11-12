using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.ComponentModel.DataAnnotations;

namespace Activos_PrestamosOET.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
        [Display(Name ="Nombre")]
        public string Nombre { get; set; }

        [Display(Name ="Apellidos")]
        public string Apellidos { get; set; }

        [Display(Name ="Cédula")]
        public string Cedula { get; set; }

        public string EstacionID { get; set; }

        [Display(Name = "Nombre completo")]
        public string FullName
        {
            get
            {
                return this.Nombre + " " + this.Apellidos;
            }
        }

    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("IdentityContext", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        //se le agrega el siguiente metodo para que use las tablas nuestras
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Debe ser la primer regla.

            modelBuilder.HasDefaultSchema("ACTIVOS"); // LO MAS IMPORTANTE!

            modelBuilder.Entity<ApplicationUser>().ToTable("ActivosUsers");
            modelBuilder.Entity<IdentityRole>().ToTable("ActivosRoles");
            modelBuilder.Entity<IdentityUserRole>().ToTable("ActivosUserRoles");
            modelBuilder.Entity<IdentityUserClaim>().ToTable("ActivosUserClaims");
            modelBuilder.Entity<IdentityUserLogin>().ToTable("ActivosUserLogins");

            //usar en caso de que se despiche la base y tire de nuevo un Extent2.Discriminator
            //modelBuilder.Entity<ApplicationUser>().Ignore(p => p.FullName);
        }
    }
}