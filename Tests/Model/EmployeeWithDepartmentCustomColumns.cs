using System;
using DbDataReaderMapper;

namespace Tests.Model
{
    public class EmployeeWithDepartmentCustomColumns
    {
        [DbColumn("ID")]
        public int Id { get; set; }

        public string FullName { get; set; }

        [DbNestedObject("Dept_")]
        public DepartmentCustomColumns Department { get; set; }

        public override bool Equals(object obj)
        {
            return obj is EmployeeWithDepartmentCustomColumns emp &&
                   Id == emp.Id &&
                   FullName == emp.FullName &&
                   Equals(Department, emp.Department);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, FullName, Department);
        }
    }
}
