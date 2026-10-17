using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace LeHoangMy_1381.MyProject.Web.Tests.Controllers;

public class ApiRouteInventory_Tests : MyProjectWebTestBase
{
    [Fact]
    public void Registered_routes_include_the_student_contract_and_are_exported_for_audit()
    {
        var descriptors = Resolve<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>().ToList();
        var routes = descriptors.Select(action => new
        {
            controller = action.ControllerName,
            action = action.ActionName,
            template = action.AttributeRouteInfo?.Template,
            methods = action.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                .SelectMany(c => c.HttpMethods).Distinct().ToArray() ?? Array.Empty<string>(),
            parameters = action.Parameters.Select(p => new { name = p.Name, type = p.ParameterType.FullName }).ToArray(),
            response = action.MethodInfo.ReturnType.FullName
        }).ToList();
        Assert.Contains(routes, r => r.controller == "ReadingApi" && r.action == "Save" && r.methods.Contains("POST"));
        Assert.Contains(routes, r => r.controller == "ReadingApi" && r.action == "Paper" && r.methods.Contains("GET"));
        Assert.Contains(routes, r => r.controller == "ReadingAccess" && r.action == "Student");
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "docs", "system-audit")))
            directory = directory.Parent;
        if (directory != null)
        {
            var output = Path.Combine(directory.FullName, "docs", "system-audit", "evidence", "registered-routes.json");
            File.WriteAllText(output, JsonSerializer.Serialize(routes, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
