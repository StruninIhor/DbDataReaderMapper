using System;
using DbDataReaderMapper;

namespace Tests.Model
{
    /// <summary>
    /// Employee model that demonstrates nested object mapping.
    /// Columns prefixed with "Dept_" are mapped to the Department property.
    /// The FullNameUpper property uses a CustomPropertyConverter to transform FullName to uppercase.
    /// </summary>
    public class EmployeeWithDepartment
    {
        [DbColumn("ID")]
        public int Id { get; set; }

        public string FullName { get; set; }
        public int? Age { get; set; }
        public string Address { get; set; }
        public DateTime? DoB { get; set; }

        [DbColumn("FullNameUpper")]
        public string FullNameUpperCase { get; set; }

        [DbNestedObject("Dept_")]
        public Department Department { get; set; }

        public override bool Equals(object obj)
        {
            return obj is EmployeeWithDepartment emp &&
                   Id == emp.Id &&
                   FullName == emp.FullName &&
                   Age == emp.Age &&
                   Address == emp.Address &&
                   DoB == emp.DoB &&
                   FullNameUpperCase == emp.FullNameUpperCase &&
                   Equals(Department, emp.Department);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, FullName, Age, Address, DoB, FullNameUpperCase, Department);
        }
    }
}
