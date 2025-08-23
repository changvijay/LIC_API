# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY LIC_WebDeskAPI/*.csproj ./LIC_WebDeskAPI/
RUN dotnet restore

# Copy the rest of the source code
COPY LIC_WebDeskAPI/. ./LIC_WebDeskAPI/
WORKDIR /src/LIC_WebDeskAPI

# Build and publish the app
RUN dotnet publish -c Release -o /app/publish

# Use the official ASP.NET Core runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80 (or change if your app uses a different port)
EXPOSE 80

# Set the entrypoint
ENTRYPOINT ["dotnet", "LIC_WebDeskAPI.dll"]
