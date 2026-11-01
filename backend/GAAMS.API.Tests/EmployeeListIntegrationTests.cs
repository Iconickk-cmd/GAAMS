
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GAAMS.API.Tests;

public class EmployeeListIntegrationTests
{
    [Fact]
    public async Task GetEmployees_WithPagination_ReturnsRequestedPage()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        var prefix = $"GAAMS-PAGE-{Guid.NewGuid():N}";

        await CreateEmployeeAsync(client, departmentId, $"{prefix}-A", "Alice", "Able");
        await CreateEmployeeAsync(client, departmentId, $"{prefix}-B", "Brian", "Baker");
        await CreateEmployeeAsync(client, departmentId, $"{prefix}-C", "Carol", "Clark");

        var response = await client.GetAsync(
         $"/api/employees?page=1&pageSize=2&search={prefix}&sortBy=employeeNumber");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmployeeListResponse>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Employees.Count);
        Assert.Equal($"{prefix}-A", result.Employees[0].EmployeeNumber);
        Assert.Equal($"{prefix}-B", result.Employees[1].EmployeeNumber);
    }

    [Fact]
    public async Task GetEmployees_WithSearch_ReturnsMatchingEmployees()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        await CreateEmployeeAsync(
            client, departmentId, $"GAAMS-SEARCH-{Guid.NewGuid():N}",
            "Searchable", "Person");

        await CreateEmployeeAsync(
            client, departmentId, $"GAAMS-OTHER-{Guid.NewGuid():N}",
            "Different", "Person");

        var response = await client.GetAsync(
            "/api/employees?search=Searchable");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmployeeListResponse>();

        Assert.NotNull(result);
        Assert.Contains(result.Employees,
            e => e.FirstName == "Searchable");
        Assert.DoesNotContain(result.Employees,
            e => e.FirstName == "Different");
    }

    [Fact]
    public async Task GetEmployees_WithDepartmentFilter_ReturnsOnlySelectedDepartment()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var firstDepartmentId = await CreateDepartmentAsync(client);
        var secondDepartmentId = await CreateDepartmentAsync(client);

        await CreateEmployeeAsync(
            client, firstDepartmentId, $"GAAMS-DEPT-A-{Guid.NewGuid():N}",
            "First", "Department");

        await CreateEmployeeAsync(
            client, secondDepartmentId, $"GAAMS-DEPT-B-{Guid.NewGuid():N}",
            "Second", "Department");

        var response = await client.GetAsync(
            $"/api/employees?departmentId={firstDepartmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmployeeListResponse>();

        Assert.NotNull(result);
        Assert.NotEmpty(result.Employees);
        Assert.All(result.Employees,
            e => Assert.Equal(firstDepartmentId, e.DepartmentId));
    }

    [Fact]
    public async Task GetEmployees_WithActiveFilter_ReturnsOnlyActiveEmployees()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        var employeeNumber = $"GAAMS-ACTIVE-{Guid.NewGuid():N}";

        await CreateEmployeeAsync(
            client, departmentId, employeeNumber,
            "Active", "Person");

        var createInactiveResponse = await client.PostAsJsonAsync(
            "/api/employees",
            CreateEmployeeRequest(
                $"GAAMS-INACTIVE-{Guid.NewGuid():N}",
                "Inactive",
                "Person",
                departmentId));

        Assert.Equal(HttpStatusCode.Created, createInactiveResponse.StatusCode);

        var inactiveEmployee =
            await createInactiveResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        Assert.NotNull(inactiveEmployee);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/employees/{inactiveEmployee.Id}",
            new
            {
                employeeNumber = inactiveEmployee.EmployeeNumber,
                firstName = inactiveEmployee.FirstName,
                lastName = inactiveEmployee.LastName,
                nationalId = inactiveEmployee.NationalId,
                email = inactiveEmployee.Email,
                phoneNumber = inactiveEmployee.PhoneNumber,
                dateOfBirth = "1990-05-10",
                dateOfEmployment = "2020-01-15",
                jobTitle = "Test Officer",
                departmentId,
                isActive = false
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var response = await client.GetAsync("/api/employees?isActive=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmployeeListResponse>();

        Assert.NotNull(result);
        Assert.Contains(result.Employees,
            e => e.EmployeeNumber == inactiveEmployee.EmployeeNumber);
        Assert.DoesNotContain(result.Employees,
            e => e.EmployeeNumber == employeeNumber);
        Assert.All(result.Employees, e => Assert.False(e.IsActive));
    }

    [Fact]
    public async Task GetEmployees_WithAscendingLastNameSort_ReturnsEmployeesInOrder()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);
        var prefix = $"GAAMS-SORT-{Guid.NewGuid():N}";

        await CreateEmployeeAsync(
            client, departmentId, $"{prefix}-A", "Zara", "Zulu");

        await CreateEmployeeAsync(
            client, departmentId, $"{prefix}-B", "Adam", "Able");

        var response = await client.GetAsync(
            $"/api/employees?search={prefix}&sortBy=lastName");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EmployeeListResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Employees.Count);
        Assert.Equal("Able", result.Employees[0].LastName);
        Assert.Equal("Zulu", result.Employees[1].LastName);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?departmentId=0")]
    [InlineData("?sortBy=unknown")]
    public async Task GetEmployees_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/employees{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<int> CreateDepartmentAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/departments",
            new
            {
                name = $"List Test Department {Guid.NewGuid():N}",
                code = $"TEST-{Guid.NewGuid():N}",
                description = "Department created for employee list testing",
                isActive = true
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var department = await response.Content.ReadFromJsonAsync<DepartmentResponse>();

        Assert.NotNull(department);
        return department.Id;
    }

    private static async Task CreateEmployeeAsync(
        HttpClient client,
        int departmentId,
        string employeeNumber,
        string firstName,
        string lastName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/employees",
            CreateEmployeeRequest(
                employeeNumber,
                firstName,
                lastName,
                departmentId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static object CreateEmployeeRequest(
        string employeeNumber,
        string firstName,
        string lastName,
        int departmentId)
    {
        return new
        {
            employeeNumber,
            firstName,
            lastName,
            nationalId = $"N-{Guid.NewGuid():N}".Substring(0, 20),
            email = (string?)null,
            phoneNumber = (string?)null,
            dateOfBirth = "1990-05-10",
            dateOfEmployment = "2020-01-15",
            jobTitle = "Integration Test Officer",
            departmentId
        };
    }

    private sealed class DepartmentResponse
    {
        public int Id { get; set; }
    }

    private sealed class EmployeeResponse
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class EmployeeListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<EmployeeResponse> Employees { get; set; } = [];
    }
}
