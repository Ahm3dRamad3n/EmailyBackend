using System;
using System.Collections.Generic;

namespace Emaily.DAL.Entities;

public partial class User
{
    public struct Role
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
