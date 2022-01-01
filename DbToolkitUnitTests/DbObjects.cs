using Byte.Toolkit.Db;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbToolkitUnitTests
{
    [DbObject]
    internal class UserGroup
    {
        [DbColumn("GROUP_ID")]
        public Int64? Id { get; set; }

        [DbColumn("GROUP_NAME")]
        public string? Name { get; set; }
    }

    [DbObject]
    internal class User
    {
        [DbColumn("USER_ID")]
        public Int64? Id { get; set; }

        [DbColumn("GROUP_ID")]
        public Int64? GroupId { get; set; }

        [DbColumn("USERNAME")]
        public string? Username { get; set; }

        [DbColumn("PASSWORD")]
        public string? Password { get; set; }

        [DbColumn("FULL_NAME")]
        public string? Name { get; set; }
    }
}
