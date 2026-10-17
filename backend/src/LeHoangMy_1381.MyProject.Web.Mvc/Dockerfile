FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN ln -s ~/.dotnet/tools/libman /usr/local/bin/libman

WORKDIR /src
COPY ["src/LeHoangMy_1381.MyProject.Web.Mvc/LeHoangMy_1381.MyProject.Web.Mvc.csproj", "src/LeHoangMy_1381.MyProject.Web.Mvc/"]
COPY ["src/LeHoangMy_1381.MyProject.Web.Core/LeHoangMy_1381.MyProject.Web.Core.csproj", "src/LeHoangMy_1381.MyProject.Web.Core/"]
COPY ["src/LeHoangMy_1381.MyProject.Application/LeHoangMy_1381.MyProject.Application.csproj", "src/LeHoangMy_1381.MyProject.Application/"]
COPY ["src/LeHoangMy_1381.MyProject.Core/LeHoangMy_1381.MyProject.Core.csproj", "src/LeHoangMy_1381.MyProject.Core/"]
COPY ["src/LeHoangMy_1381.MyProject.EntityFrameworkCore/LeHoangMy_1381.MyProject.EntityFrameworkCore.csproj", "src/LeHoangMy_1381.MyProject.EntityFrameworkCore/"]
WORKDIR "/src/src/LeHoangMy_1381.MyProject.Web.Mvc"
RUN dotnet restore

WORKDIR /src
COPY ["src/LeHoangMy_1381.MyProject.Web.Mvc", "src/LeHoangMy_1381.MyProject.Web.Mvc"]
COPY ["src/LeHoangMy_1381.MyProject.Web.Core", "src/LeHoangMy_1381.MyProject.Web.Core"]
COPY ["src/LeHoangMy_1381.MyProject.Application", "src/LeHoangMy_1381.MyProject.Application"]
COPY ["src/LeHoangMy_1381.MyProject.Core", "src/LeHoangMy_1381.MyProject.Core"]
COPY ["src/LeHoangMy_1381.MyProject.EntityFrameworkCore", "src/LeHoangMy_1381.MyProject.EntityFrameworkCore"]
WORKDIR "/src/src/LeHoangMy_1381.MyProject.Web.Mvc"
RUN libman restore
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "LeHoangMy_1381.MyProject.Web.Mvc.dll"]
