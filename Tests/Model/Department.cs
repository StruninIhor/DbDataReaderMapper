using System;

namespace Tests.Model
{
    public class Department
    {
        public short? Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Department dept &&
                   Id == dept.Id &&
                   Name == dept.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}
