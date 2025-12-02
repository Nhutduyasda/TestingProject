using Microsoft.AspNetCore.Identity;

namespace MyProject.Data
{
    /// <summary>
    /// Identity user for authentication and authorization
    /// Links to domain User model via DomainUserId
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Foreign key to domain User model (User.UserID)
        /// Allows separation of authentication (Identity) and business logic (Domain)
        /// </summary>
        public int? DomainUserId { get; set; }
    }
}
