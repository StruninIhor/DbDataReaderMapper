using DbDataReaderMapper;

namespace Tests.Model
{
    public class DepartmentCustomColumns
    {
        [DbColumn("DepartmentId", Override = true)]
        public short? Id { get; set; }

        [DbColumn("DepartmentName")]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is DepartmentCustomColumns dept &&
                   Id == dept.Id &&
                   Name == dept.Name;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Id, Name);
        }
    }
}
