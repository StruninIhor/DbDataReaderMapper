using System.Data.OleDb;
using DbDataReaderMapper;
using Tests.Model;
using DbDataReaderMapper.Exceptions;

namespace Tests
{
    [TestClass]
    public class DbDataReaderExtensionTest
    {
        OleDbConnection connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=TestDatabase.accdb");

        [TestMethod]
        public async Task TestMapperHappyPath()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeAllFields>();
                Assert.AreEqual(employeeObj, new EmployeeAllFields
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Age = reader.GetInt32(2),
                    Address = reader.GetString(3),
                    DoB = reader.GetDateTime(4)
                });
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperNullNullableValues()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=2;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeAllFields>();
                Assert.AreEqual(employeeObj, new EmployeeAllFields
                {
                    Id = reader.GetInt32(0),
                    FullName = null,
                    Age = null,
                    Address = null,
                    DoB = null
                });
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperWithMissingModelFields()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeMissingFields>();
                Assert.AreEqual(employeeObj, new EmployeeMissingFields
                {
                    Id = reader.GetInt32(0)
                });
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperWithExtraModelFields()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeExtraDifferentFields>();
                Assert.AreEqual(employeeObj, new EmployeeExtraDifferentFields
                {
                    Id = reader.GetInt32(0),
                    Name = null
                });
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperWrongType()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Assert.ThrowsException<InvalidCastException>(() => reader.MapToObject<EmployeeWrongType>());
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperNullValueInNonNullableField()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=2;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeNonNullableFieldsDefault>();
                Assert.AreEqual(employeeObj, new EmployeeNonNullableFieldsDefault
                {
                    Id = reader.GetInt32(0),
                    FullName = null,
                    Age = 0,
                    Address = null,
                    DoB = new DateTime()
                });
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperImplicitCastingWhenDisabled()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Assert.ThrowsException<InvalidCastException>(() => reader.MapToObject<EmployeeWrongTypeImplicitCast>());
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperWithAttributeHappyPath()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWithAttributes>();
                Assert.AreEqual(employeeObj, new EmployeeWithAttributes
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperWithAttributeSameNameAsProperty()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Assert.ThrowsException<DbColumnMappingException>(() => reader.MapToObject<EmployeeWithAttributesSameNameAsProperty>());
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperWithAttributeSameNameAsPropertyWithCustomName() //TODO
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT FullName, Address FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWithAttributesPropertyNameSwap>();
                Assert.AreEqual(employeeObj, new EmployeeWithAttributesPropertyNameSwap
                {
                    FullName = reader.GetString(1),
                    Address = reader.GetString(0)
                });
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperNoRowsReturned()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=99;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeAllFields>();
                Assert.IsNull(employeeObj);
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperMultipleRows()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id FROM Employee;";
            cmd.Connection = connection;

            int rowCounter = 0;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeMissingFields>();
                Assert.AreEqual(employeeObj, new EmployeeMissingFields
                {
                    Id = reader.GetInt32(0)
                });
                ++rowCounter;
            }
            Assert.AreEqual(2, rowCounter);
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperListMultipleRows()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id FROM Employee;";
            cmd.Connection = connection;

            int rowCounter = 0;
            var reader = await cmd.ExecuteReaderAsync();
            var employeeList = await reader.MapToListAsync<EmployeeMissingFields>();
            await reader.CloseAsync();
            OleDbCommand cmd1 = connection.CreateCommand();
            cmd.CommandText = "SELECT ID AS Id FROM Employee;";
            cmd.Connection = connection;

          
            var reader1 = await cmd.ExecuteReaderAsync();

            while (await reader1.ReadAsync())
            {
                var employeeObj = employeeList[rowCounter];
                Assert.AreEqual(employeeObj, new EmployeeMissingFields
                {
                    Id = reader1.GetInt32(0)
                });
                ++rowCounter;
            }
            Assert.AreEqual(2, rowCounter);
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperCustomConverter()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT DoB FROM Employee;";
            cmd.Connection = connection;

            CustomPropertyConverter customPropertyConverter = new CustomPropertyConverter()
                .AddConversion<EmployeeDateAsString, DateTime?, string>(e => e.DoB, dob => dob?.ToLongDateString());

            int rowCounter = 0;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var dobAsString = reader.MapToObject<EmployeeDateAsString>(customPropertyConverter);
                var dobAsDate = new EmployeeDateAsDate
                {
                    DoB = reader[0] is DBNull ? (DateTime?)null : reader.GetDateTime(0)
                };

                Assert.AreEqual(dobAsDate.DoB?.ToLongDateString(), dobAsString.DoB);
                ++rowCounter;
            }
            Assert.AreEqual(2, rowCounter);
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperSubclassSetsSuperclassFields()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeSubClass>();
                Assert.AreEqual(employeeObj, new EmployeeSubClass
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Age = reader.GetInt32(2),
                    Address = reader.GetString(3),
                    DoB = reader.GetDateTime(4)
                });
            }
        }

        [TestMethod]
        public async Task TestMapperNestedObjectInnerJoin()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT Employee.ID, Employee.FullName, Employee.Age, Employee.Address, Employee.DoB, " +
                "UCASE(Employee.FullName) AS FullNameUpper, " +
                "Department.ID AS Dept_Id, Department.[Name] AS Dept_Name " +
                "FROM Employee " +
                "INNER JOIN Department ON Employee.DepartmentID = Department.ID " +
                "WHERE Employee.ID = 1;";
            cmd.Connection = connection;

            var converter = new CustomPropertyConverter()
                .AddConversion<EmployeeWithDepartment, string, string>(
                    e => e.FullNameUpperCase, name => name?.ToUpper());

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWithDepartment>(converter);
                Assert.IsNotNull(employeeObj);
                Assert.IsNotNull(employeeObj.Department);
                Assert.AreEqual(1, employeeObj.Id);
                Assert.AreEqual(employeeObj.FullName?.ToUpper(), employeeObj.FullNameUpperCase);
                Assert.IsNotNull(employeeObj.Department.Id);
                Assert.IsNotNull(employeeObj.Department.Name);
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperNestedObjectLeftJoinNull()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT Employee.ID, Employee.FullName, Employee.Age, Employee.Address, Employee.DoB, " +
                "UCASE(Employee.FullName) AS FullNameUpper, " +
                "Department.ID AS Dept_Id, Department.[Name] AS Dept_Name " +
                "FROM Employee " +
                "LEFT JOIN Department ON Employee.DepartmentID = Department.ID " +
                "WHERE Employee.ID = 2;";
            cmd.Connection = connection;

            var converter = new CustomPropertyConverter()
                .AddConversion<EmployeeWithDepartment, string, string>(
                    e => e.FullNameUpperCase, name => name?.ToUpper());

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWithDepartment>(converter);
                Assert.IsNotNull(employeeObj);
                Assert.AreEqual(2, employeeObj.Id);
                Assert.IsNull(employeeObj.Department);
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperNestedObjectListInnerJoin()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT Employee.ID, Employee.FullName, Employee.Age, Employee.Address, Employee.DoB, " +
                "UCASE(Employee.FullName) AS FullNameUpper, " +
                "Department.ID AS Dept_Id, Department.[Name] AS Dept_Name " +
                "FROM Employee " +
                "INNER JOIN Department ON Employee.DepartmentID = Department.ID;";
            cmd.Connection = connection;

            var converter = new CustomPropertyConverter()
                .AddConversion<EmployeeWithDepartment, string, string>(
                    e => e.FullNameUpperCase, name => name?.ToUpper());

            var reader = await cmd.ExecuteReaderAsync();
            var employees = await reader.MapToListAsync<EmployeeWithDepartment>(converter);

            Assert.IsTrue(employees.Count > 0);
            foreach (var emp in employees)
            {
                Assert.IsNotNull(emp.Department);
                Assert.IsNotNull(emp.Department.Id);
                Assert.IsNotNull(emp.Department.Name);
                Assert.AreEqual(emp.FullName?.ToUpper(), emp.FullNameUpperCase);
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperNestedObjectListLeftJoin()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT Employee.ID, Employee.FullName, Employee.Age, Employee.Address, Employee.DoB, " +
                "UCASE(Employee.FullName) AS FullNameUpper, " +
                "Department.ID AS Dept_Id, Department.[Name] AS Dept_Name " +
                "FROM Employee " +
                "LEFT JOIN Department ON Employee.DepartmentID = Department.ID;";
            cmd.Connection = connection;

            var converter = new CustomPropertyConverter()
                .AddConversion<EmployeeWithDepartment, string, string>(
                    e => e.FullNameUpperCase, name => name?.ToUpper());

            var reader = await cmd.ExecuteReaderAsync();
            var employees = await reader.MapToListAsync<EmployeeWithDepartment>(converter);

            Assert.IsTrue(employees.Count >= 2);

            var withDept = employees.FirstOrDefault(e => e.Id == 1);
            var withoutDept = employees.FirstOrDefault(e => e.Id == 2);

            Assert.IsNotNull(withDept);
            Assert.IsNotNull(withDept.Department);
            Assert.IsNotNull(withDept.Department.Id);
            Assert.IsNotNull(withDept.Department.Name);

            Assert.IsNotNull(withoutDept);
            Assert.IsNull(withoutDept.Department);
        }

        [TestMethod]
        public async Task TestMapperNestedObjectWithoutConverter()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT Employee.ID, Employee.FullName, Employee.Age, Employee.Address, Employee.DoB, " +
                "UCASE(Employee.FullName) AS FullNameUpper, " +
                "Department.ID AS Dept_Id, Department.[Name] AS Dept_Name " +
                "FROM Employee " +
                "INNER JOIN Department ON Employee.DepartmentID = Department.ID " +
                "WHERE Employee.ID = 1;";
            cmd.Connection = connection;

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWithDepartment>();
                Assert.IsNotNull(employeeObj);
                Assert.AreEqual(1, employeeObj.Id);
                Assert.IsNotNull(employeeObj.Department);
                Assert.IsNotNull(employeeObj.Department.Id);
                Assert.IsNotNull(employeeObj.Department.Name);
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperSubclassSetsSuperclassFieldsWithCustomPropertyConverter()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT ID AS Id, FullName, Age, Address, DoB FROM Employee WHERE ID=1;";
            cmd.Connection = connection;

            var converter = new CustomPropertyConverter()
                .AddConversion<EmployeeWrongTypeNeedsConversionSubclass, string, int>(e => e.Address, e => e.Length);

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWrongTypeNeedsConversionSubclass>(converter);
                Assert.AreEqual(employeeObj, new EmployeeWrongTypeNeedsConversionSubclass
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Age = reader.GetInt32(2),
                    Address = reader.GetString(3).Length,
                    DoB = reader.GetDateTime(4)
                });
            }
        }

        [TestMethod]
        public async Task TestMapperNestedObjectCustomColumnNames()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT Employee.ID, Employee.FullName, " +
                "Department.ID AS DepartmentId, Department.[Name] AS Dept_DepartmentName " +
                "FROM Employee " +
                "INNER JOIN Department ON Employee.DepartmentID = Department.ID " +
                "WHERE Employee.ID = 1;";
            cmd.Connection = connection;

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWithDepartmentCustomColumns>();
                Assert.IsNotNull(employeeObj);
                Assert.AreEqual(1, employeeObj.Id);
                Assert.IsNotNull(employeeObj.FullName);
                Assert.IsNotNull(employeeObj.Department);
                Assert.IsNotNull(employeeObj.Department.Id);
                Assert.IsNotNull(employeeObj.Department.Name);
            }
            connection.Close();
        }

        [TestMethod]
        public async Task TestMapperNestedObjectCustomColumnNamesLeftJoinNull()
        {
            OleDbCommand cmd = connection.CreateCommand();
            connection.Open();
            cmd.CommandText = "SELECT Employee.ID, Employee.FullName, " +
                "Department.ID AS DepartmentId, Department.[Name] AS Dept_DepartmentName " +
                "FROM Employee " +
                "LEFT JOIN Department ON Employee.DepartmentID = Department.ID " +
                "WHERE Employee.ID = 2;";
            cmd.Connection = connection;

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var employeeObj = reader.MapToObject<EmployeeWithDepartmentCustomColumns>();
                Assert.IsNotNull(employeeObj);
                Assert.AreEqual(2, employeeObj.Id);
                Assert.IsNull(employeeObj.Department);
            }
            connection.Close();
        }
    }
}
