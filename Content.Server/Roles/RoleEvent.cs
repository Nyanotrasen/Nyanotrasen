﻿namespace Content.Server.Roles
{
    public abstract class RoleEvent : EntityEventArgs
    {
        public readonly Role Role;

        public RoleEvent(Role role)
        {
            Role = role;
        }
    }
}
