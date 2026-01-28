using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Users
{
    /// <summary>
    /// DTO for user information.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// User ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Full name.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// List of roles assigned to the user.
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Date when the user was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date of the last login.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
    }
}
